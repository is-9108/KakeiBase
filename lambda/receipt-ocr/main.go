package main

import (
	"context"
	"fmt"
	"io"
	"log/slog"
	"math"
	"os"
	"regexp"
	"strconv"
	"strings"
	"time"

	"github.com/aws/aws-lambda-go/events"
	"github.com/aws/aws-lambda-go/lambda"
	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/config"
	"github.com/aws/aws-sdk-go-v2/service/s3"
	"github.com/aws/aws-sdk-go-v2/service/textract"
	"github.com/aws/aws-sdk-go-v2/service/textract/types"
	"github.com/google/uuid"
	"github.com/is-9108/KakeiBase/lambda/shared"
	"github.com/jackc/pgx/v5/pgxpool"
)

// S3Client は S3 からオブジェクトを取得するインターフェース。
type S3Client interface {
	GetObject(ctx context.Context, bucket, key string) ([]byte, error)
}

// TextractClient は Textract AnalyzeExpense を呼び出すインターフェース。
type TextractClient interface {
	AnalyzeExpense(ctx context.Context, imageBytes []byte) (*textract.AnalyzeExpenseOutput, error)
}

// DB はデータベース操作のインターフェース。テスト時にモックへ差し替える。
type DB interface {
	ExistsTransactionByReceiptKey(ctx context.Context, receiptS3Key string) (bool, error)
	FindUncategorizedCategoryID(ctx context.Context, userID uuid.UUID) (uuid.UUID, error)
	InsertTransaction(ctx context.Context, tx Transaction) error
}

// Transaction は INSERT する収支レコードを表す。
type Transaction struct {
	ID           uuid.UUID
	UserID       uuid.UUID
	CategoryID   uuid.UUID
	Amount       int
	Date         time.Time
	Memo         string
	ReceiptS3Key string
	CreatedAt    time.Time
	UpdatedAt    time.Time
}

// Result は Lambda の実行結果を表す。
type Result struct {
	Created int `json:"created"`
	Skipped int `json:"skipped"`
	Errors  int `json:"errors"`
}

// ParseUserIDFromKey は S3 キー (receipts/{userId}/...) から userId を抽出する。
func ParseUserIDFromKey(key string) (uuid.UUID, error) {
	parts := strings.Split(key, "/")
	if len(parts) < 2 || parts[0] != "receipts" {
		return uuid.Nil, fmt.Errorf("invalid S3 key format: %s", key)
	}
	id, err := uuid.Parse(parts[1])
	if err != nil {
		return uuid.Nil, fmt.Errorf("invalid userId in S3 key: %s: %w", parts[1], err)
	}
	return id, nil
}

// ExpenseData は Textract AnalyzeExpense から抽出したデータを表す。
type ExpenseData struct {
	Total      *int
	Date       *time.Time
	VendorName string
}

// ExtractExpenseData は Textract AnalyzeExpense の出力から金額・日付・店舗名を抽出する。
func ExtractExpenseData(output *textract.AnalyzeExpenseOutput, fallbackDate time.Time) ExpenseData {
	var data ExpenseData
	data.Date = &fallbackDate

	for _, doc := range output.ExpenseDocuments {
		for _, field := range doc.SummaryFields {
			if field.Type == nil || field.Type.Text == nil || field.ValueDetection == nil || field.ValueDetection.Text == nil {
				continue
			}
			fieldType := *field.Type.Text
			value := *field.ValueDetection.Text

			switch fieldType {
			case "TOTAL":
				if amount, err := ParseAmount(value); err == nil {
					data.Total = &amount
				}
			case "INVOICE_RECEIPT_DATE":
				if t, err := ParseDate(value); err == nil {
					data.Date = &t
				}
			case "VENDOR_NAME":
				data.VendorName = value
			}
		}
	}
	return data
}

// amountRegexp は金額文字列からカンマ・通貨記号を除去するための正規表現。
var amountRegexp = regexp.MustCompile(`[^\d.]`)

// ParseAmount は金額文字列を整数（円）にパースする。
func ParseAmount(s string) (int, error) {
	cleaned := amountRegexp.ReplaceAllString(s, "")
	if cleaned == "" {
		return 0, fmt.Errorf("empty amount string")
	}
	f, err := strconv.ParseFloat(cleaned, 64)
	if err != nil {
		return 0, fmt.Errorf("parse amount %q: %w", s, err)
	}
	return int(math.Round(f)), nil
}

// dateFormats は Textract が返す可能性のある日付フォーマット。
var dateFormats = []string{
	"2006-01-02",
	"2006/01/02",
	"20060102",
	"2006年01月02日",
	"2006年1月2日",
	"01/02/2006",
	"1/2/2006",
}

// ParseDate は日付文字列を time.Time にパースする。
func ParseDate(s string) (time.Time, error) {
	s = strings.TrimSpace(s)
	for _, format := range dateFormats {
		if t, err := time.Parse(format, s); err == nil {
			return t, nil
		}
	}
	return time.Time{}, fmt.Errorf("unable to parse date: %q", s)
}

// ProcessS3Event は S3 イベントのレコードを処理するコアロジック。
func ProcessS3Event(ctx context.Context, s3Client S3Client, textractClient TextractClient, db DB, records []events.S3EventRecord, now time.Time) Result {
	logger := slog.Default()
	var result Result

	for _, record := range records {
		bucket := record.S3.Bucket.Name
		key := record.S3.Object.Key

		logger.Info("processing receipt", "bucket", bucket, "key", key)

		// S3キーからuserIdをパース
		userID, err := ParseUserIDFromKey(key)
		if err != nil {
			logger.Error("failed to parse userId from S3 key", "key", key, "error", err)
			result.Errors++
			continue
		}

		// 二重登録チェック
		exists, err := db.ExistsTransactionByReceiptKey(ctx, key)
		if err != nil {
			logger.Error("failed to check existing transaction", "key", key, "error", err)
			result.Errors++
			continue
		}
		if exists {
			logger.Info("transaction already exists, skipping", "key", key)
			result.Skipped++
			continue
		}

		// S3から画像取得
		imageBytes, err := s3Client.GetObject(ctx, bucket, key)
		if err != nil {
			logger.Error("failed to get S3 object", "bucket", bucket, "key", key, "error", err)
			result.Errors++
			continue
		}

		// Textract AnalyzeExpense
		textractOutput, err := textractClient.AnalyzeExpense(ctx, imageBytes)
		if err != nil {
			logger.Error("failed to analyze expense", "key", key, "error", err)
			result.Errors++
			continue
		}

		// OCR結果から金額・日付・店舗名を抽出
		data := ExtractExpenseData(textractOutput, now)

		// 金額パース失敗時はスキップ（ADR-0005）
		if data.Total == nil {
			logger.Warn("no total amount found, skipping", "key", key)
			result.Skipped++
			continue
		}

		// 「未分類」カテゴリIDを取得
		categoryID, err := db.FindUncategorizedCategoryID(ctx, userID)
		if err != nil {
			logger.Error("failed to find uncategorized category", "user_id", userID, "error", err)
			result.Errors++
			continue
		}

		tx := Transaction{
			ID:           uuid.New(),
			UserID:       userID,
			CategoryID:   categoryID,
			Amount:       *data.Total,
			Date:         *data.Date,
			Memo:         data.VendorName,
			ReceiptS3Key: key,
			CreatedAt:    now,
			UpdatedAt:    now,
		}

		if err := db.InsertTransaction(ctx, tx); err != nil {
			logger.Error("failed to insert transaction", "key", key, "error", err)
			result.Errors++
			continue
		}

		logger.Info("created transaction", "key", key, "transaction_id", tx.ID, "amount", tx.Amount)
		result.Created++
	}

	logger.Info("processing complete", "created", result.Created, "skipped", result.Skipped, "errors", result.Errors)
	return result
}

// --- AWS 実装 ---

// AWSS3Client は aws-sdk-go-v2 を使った S3Client の実装。
type AWSS3Client struct {
	client *s3.Client
}

func (c *AWSS3Client) GetObject(ctx context.Context, bucket, key string) ([]byte, error) {
	output, err := c.client.GetObject(ctx, &s3.GetObjectInput{
		Bucket: aws.String(bucket),
		Key:    aws.String(key),
	})
	if err != nil {
		return nil, fmt.Errorf("s3 get object: %w", err)
	}
	defer output.Body.Close()

	data, err := io.ReadAll(output.Body)
	if err != nil {
		return nil, fmt.Errorf("read s3 object body: %w", err)
	}
	return data, nil
}

// AWSTextractClient は aws-sdk-go-v2 を使った TextractClient の実装。
type AWSTextractClient struct {
	client *textract.Client
}

func (c *AWSTextractClient) AnalyzeExpense(ctx context.Context, imageBytes []byte) (*textract.AnalyzeExpenseOutput, error) {
	output, err := c.client.AnalyzeExpense(ctx, &textract.AnalyzeExpenseInput{
		Document: &types.Document{
			Bytes: imageBytes,
		},
	})
	if err != nil {
		return nil, fmt.Errorf("textract analyze expense: %w", err)
	}
	return output, nil
}

// PgxDB は pgx を使った DB インターフェースの実装。
type PgxDB struct {
	pool *pgxpool.Pool
}

func (d *PgxDB) ExistsTransactionByReceiptKey(ctx context.Context, receiptS3Key string) (bool, error) {
	var exists bool
	err := d.pool.QueryRow(ctx,
		`SELECT EXISTS(SELECT 1 FROM transactions WHERE receipt_s3_key = $1)`,
		receiptS3Key,
	).Scan(&exists)
	if err != nil {
		return false, fmt.Errorf("check existing transaction by receipt key: %w", err)
	}
	return exists, nil
}

func (d *PgxDB) FindUncategorizedCategoryID(ctx context.Context, userID uuid.UUID) (uuid.UUID, error) {
	var categoryID uuid.UUID
	err := d.pool.QueryRow(ctx,
		`SELECT id FROM categories WHERE name = '未分類' AND is_system = true LIMIT 1`,
	).Scan(&categoryID)
	if err != nil {
		return uuid.Nil, fmt.Errorf("find uncategorized category: %w", err)
	}
	return categoryID, nil
}

func (d *PgxDB) InsertTransaction(ctx context.Context, tx Transaction) error {
	_, err := d.pool.Exec(ctx,
		`INSERT INTO transactions (id, user_id, category_id, amount, transaction_date, memo, receipt_s3_key, created_at, updated_at)
		 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)`,
		tx.ID, tx.UserID, tx.CategoryID, tx.Amount, tx.Date, tx.Memo, tx.ReceiptS3Key, tx.CreatedAt, tx.UpdatedAt,
	)
	if err != nil {
		return fmt.Errorf("insert transaction: %w", err)
	}
	return nil
}

func main() {
	lambda.Start(handler)
}

func handler(ctx context.Context, s3Event events.S3Event) (Result, error) {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	slog.SetDefault(logger)

	secretARN := os.Getenv("DB_SECRET_ARN")
	if secretARN == "" {
		return Result{}, fmt.Errorf("DB_SECRET_ARN environment variable is not set")
	}

	cfg, err := config.LoadDefaultConfig(ctx)
	if err != nil {
		return Result{}, fmt.Errorf("load AWS config: %w", err)
	}

	smClient, err := shared.NewSecretsManagerClient(ctx)
	if err != nil {
		return Result{}, fmt.Errorf("create secrets manager client: %w", err)
	}

	secret, err := shared.FetchDBSecret(ctx, smClient, secretARN)
	if err != nil {
		return Result{}, fmt.Errorf("fetch db secret: %w", err)
	}

	pool, err := shared.ConnectDB(ctx, secret)
	if err != nil {
		return Result{}, fmt.Errorf("connect to database: %w", err)
	}
	defer pool.Close()

	s3Client := &AWSS3Client{client: s3.NewFromConfig(cfg)}
	textractClient := &AWSTextractClient{client: textract.NewFromConfig(cfg)}
	db := &PgxDB{pool: pool}

	result := ProcessS3Event(ctx, s3Client, textractClient, db, s3Event.Records, time.Now().UTC())
	return result, nil
}
