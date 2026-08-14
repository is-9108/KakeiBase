package main

import (
	"bytes"
	"context"
	"fmt"
	"html/template"
	"log/slog"
	"math"
	"os"
	"sort"
	"time"

	"github.com/aws/aws-lambda-go/lambda"
	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/config"
	"github.com/aws/aws-sdk-go-v2/service/sesv2"
	"github.com/aws/aws-sdk-go-v2/service/sesv2/types"
	"github.com/google/uuid"
	"github.com/is-9108/KakeiBase/lambda/shared"
	"github.com/jackc/pgx/v5/pgxpool"
)

// User はメール送信先のユーザーを表す。
type User struct {
	ID    uuid.UUID
	Email string
}

// TransactionWithCategory は取引とカテゴリ情報を結合した構造体。
type TransactionWithCategory struct {
	Amount          int
	TransactionType string // "Income" or "Expense"
	CategoryName    string
}

// MonthlySummary は月次集計結果を表す。
type MonthlySummary struct {
	Year         int
	Month        int
	TotalIncome  int
	TotalExpense int
	Balance      int
	Breakdown    []CategoryBreakdown
}

// CategoryBreakdown はカテゴリ別の支出内訳を表す。
type CategoryBreakdown struct {
	Name       string
	Amount     int
	Percentage float64
}

// DB はデータベース操作のインターフェース。テスト時にモックへ差し替える。
type DB interface {
	FetchUser(ctx context.Context) (User, error)
	FetchMonthlyTransactions(ctx context.Context, userID uuid.UUID, year int, month time.Month) ([]TransactionWithCategory, error)
}

// EmailClient はメール送信のインターフェース。テスト時にモックへ差し替える。
type EmailClient interface {
	SendEmail(ctx context.Context, to, subject, htmlBody string) error
}

// Result は Lambda の実行結果を表す。
type Result struct {
	Status string `json:"status"` // "sent" or "skipped"
	Email  string `json:"email"`
	Year   int    `json:"year"`
	Month  int    `json:"month"`
}

// prevMonth は指定時刻の前月の年と月を返す。
func prevMonth(now time.Time) (int, time.Month) {
	first := time.Date(now.Year(), now.Month(), 1, 0, 0, 0, 0, time.UTC)
	prev := first.AddDate(0, -1, 0)
	return prev.Year(), prev.Month()
}

// aggregateTransactions は取引リストから月次集計を生成する。
func aggregateTransactions(txns []TransactionWithCategory, year int, month time.Month) MonthlySummary {
	var totalIncome, totalExpense int
	categoryAmounts := make(map[string]int)

	for _, tx := range txns {
		if tx.TransactionType == "Income" {
			totalIncome += tx.Amount
		} else {
			totalExpense += tx.Amount
			categoryAmounts[tx.CategoryName] += tx.Amount
		}
	}

	var breakdown []CategoryBreakdown
	for name, amount := range categoryAmounts {
		pct := 0.0
		if totalExpense > 0 {
			pct = math.Round(float64(amount)/float64(totalExpense)*1000) / 10
		}
		breakdown = append(breakdown, CategoryBreakdown{
			Name:       name,
			Amount:     amount,
			Percentage: pct,
		})
	}
	sort.Slice(breakdown, func(i, j int) bool {
		return breakdown[i].Amount > breakdown[j].Amount
	})

	return MonthlySummary{
		Year:         year,
		Month:        int(month),
		TotalIncome:  totalIncome,
		TotalExpense: totalExpense,
		Balance:      totalIncome - totalExpense,
		Breakdown:    breakdown,
	}
}

// formatAmount は金額をカンマ区切りの文字列に変換する（例: 12345 → "12,345"）。
func formatAmount(n int) string {
	negative := n < 0
	if negative {
		n = -n
	}
	s := fmt.Sprintf("%d", n)
	// 3桁ごとにカンマを挿入
	result := make([]byte, 0, len(s)+(len(s)-1)/3)
	for i, c := range s {
		if i > 0 && (len(s)-i)%3 == 0 {
			result = append(result, ',')
		}
		result = append(result, byte(c))
	}
	if negative {
		return "-" + string(result)
	}
	return string(result)
}

var reportTemplate = template.Must(template.New("report").Funcs(template.FuncMap{
	"formatAmount": formatAmount,
}).Parse(`<!DOCTYPE html>
<html lang="ja">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="font-family: 'Helvetica Neue', Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5;">
<div style="max-width: 600px; margin: 0 auto; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
  <div style="background: #2563eb; color: #fff; padding: 24px; text-align: center;">
    <h1 style="margin: 0; font-size: 20px;">KakeiBase 月次レポート</h1>
    <p style="margin: 8px 0 0; font-size: 16px;">{{.Year}}年{{.Month}}月</p>
  </div>
  <div style="padding: 24px;">
    <h2 style="font-size: 16px; color: #374151; border-bottom: 2px solid #e5e7eb; padding-bottom: 8px;">収支サマリ</h2>
    <table style="width: 100%; border-collapse: collapse; margin-bottom: 24px;">
      <tr>
        <td style="padding: 12px 8px; color: #059669; font-weight: bold;">収入合計</td>
        <td style="padding: 12px 8px; text-align: right; font-size: 18px; color: #059669;">¥{{formatAmount .TotalIncome}}</td>
      </tr>
      <tr style="background: #f9fafb;">
        <td style="padding: 12px 8px; color: #dc2626; font-weight: bold;">支出合計</td>
        <td style="padding: 12px 8px; text-align: right; font-size: 18px; color: #dc2626;">¥{{formatAmount .TotalExpense}}</td>
      </tr>
      <tr>
        <td style="padding: 12px 8px; font-weight: bold;">残高</td>
        <td style="padding: 12px 8px; text-align: right; font-size: 20px; font-weight: bold;">¥{{formatAmount .Balance}}</td>
      </tr>
    </table>
    {{if .Breakdown}}
    <h2 style="font-size: 16px; color: #374151; border-bottom: 2px solid #e5e7eb; padding-bottom: 8px;">カテゴリ別支出</h2>
    <table style="width: 100%; border-collapse: collapse;">
      <tr style="background: #f3f4f6;">
        <th style="padding: 10px 8px; text-align: left; font-size: 13px; color: #6b7280;">カテゴリ</th>
        <th style="padding: 10px 8px; text-align: right; font-size: 13px; color: #6b7280;">金額</th>
        <th style="padding: 10px 8px; text-align: right; font-size: 13px; color: #6b7280;">割合</th>
      </tr>
      {{range .Breakdown}}
      <tr style="border-bottom: 1px solid #e5e7eb;">
        <td style="padding: 10px 8px;">{{.Name}}</td>
        <td style="padding: 10px 8px; text-align: right;">¥{{formatAmount .Amount}}</td>
        <td style="padding: 10px 8px; text-align: right;">{{printf "%.1f" .Percentage}}%</td>
      </tr>
      {{end}}
    </table>
    {{end}}
  </div>
  <div style="background: #f9fafb; padding: 16px; text-align: center; font-size: 12px; color: #9ca3af;">
    KakeiBase — 家計簿管理アプリ
  </div>
</div>
</body>
</html>`))

// renderHTML は集計結果からHTMLメール本文を生成する。
func renderHTML(summary MonthlySummary) (string, error) {
	var buf bytes.Buffer
	if err := reportTemplate.Execute(&buf, summary); err != nil {
		return "", fmt.Errorf("render HTML template: %w", err)
	}
	return buf.String(), nil
}

// GenerateMonthlyReport はコアロジック。ユーザーの前月集計を行い、メール送信する。
func GenerateMonthlyReport(ctx context.Context, db DB, emailClient EmailClient, now time.Time) (Result, error) {
	logger := slog.Default()
	year, month := prevMonth(now)

	user, err := db.FetchUser(ctx)
	if err != nil {
		return Result{}, fmt.Errorf("fetch user: %w", err)
	}

	logger.Info("target user", "user_id", user.ID, "email", user.Email)

	txns, err := db.FetchMonthlyTransactions(ctx, user.ID, year, month)
	if err != nil {
		return Result{}, fmt.Errorf("fetch transactions: %w", err)
	}

	if len(txns) == 0 {
		logger.Info("no transactions, skipping", "user_id", user.ID)
		return Result{Status: "skipped", Email: user.Email, Year: year, Month: int(month)}, nil
	}

	summary := aggregateTransactions(txns, year, month)

	html, err := renderHTML(summary)
	if err != nil {
		return Result{}, fmt.Errorf("render HTML: %w", err)
	}

	subject := fmt.Sprintf("[KakeiBase] %d年%d月 月次レポート", year, int(month))

	if err := emailClient.SendEmail(ctx, user.Email, subject, html); err != nil {
		return Result{}, fmt.Errorf("send email to %s: %w", user.Email, err)
	}

	logger.Info("sent report", "user_id", user.ID, "email", user.Email)
	return Result{Status: "sent", Email: user.Email, Year: year, Month: int(month)}, nil
}

// PgxDB は pgx を使った DB インターフェースの実装。
type PgxDB struct {
	pool *pgxpool.Pool
}

func (d *PgxDB) FetchUser(ctx context.Context) (User, error) {
	var u User
	err := d.pool.QueryRow(ctx, `SELECT id, email FROM users LIMIT 1`).Scan(&u.ID, &u.Email)
	if err != nil {
		return User{}, fmt.Errorf("query user: %w", err)
	}
	return u, nil
}

func (d *PgxDB) FetchMonthlyTransactions(ctx context.Context, userID uuid.UUID, year int, month time.Month) ([]TransactionWithCategory, error) {
	startOfMonth := time.Date(year, month, 1, 0, 0, 0, 0, time.UTC)
	startOfNext := startOfMonth.AddDate(0, 1, 0)

	rows, err := d.pool.Query(ctx,
		`SELECT t.amount, c.transaction_type, c.name
		 FROM transactions t
		 JOIN categories c ON t.category_id = c.id
		 WHERE t.user_id = $1
		   AND t.transaction_date >= $2
		   AND t.transaction_date < $3`,
		userID, startOfMonth, startOfNext,
	)
	if err != nil {
		return nil, fmt.Errorf("query transactions: %w", err)
	}
	defer rows.Close()

	var txns []TransactionWithCategory
	for rows.Next() {
		var tx TransactionWithCategory
		if err := rows.Scan(&tx.Amount, &tx.TransactionType, &tx.CategoryName); err != nil {
			return nil, fmt.Errorf("scan transaction: %w", err)
		}
		txns = append(txns, tx)
	}
	return txns, rows.Err()
}

// SESEmailClient は SES v2 を使った EmailClient の実装。
type SESEmailClient struct {
	client      *sesv2.Client
	fromAddress string
}

func (s *SESEmailClient) SendEmail(ctx context.Context, to, subject, htmlBody string) error {
	_, err := s.client.SendEmail(ctx, &sesv2.SendEmailInput{
		FromEmailAddress: aws.String(s.fromAddress),
		Destination: &types.Destination{
			ToAddresses: []string{to},
		},
		Content: &types.EmailContent{
			Simple: &types.Message{
				Subject: &types.Content{
					Data:    aws.String(subject),
					Charset: aws.String("UTF-8"),
				},
				Body: &types.Body{
					Html: &types.Content{
						Data:    aws.String(htmlBody),
						Charset: aws.String("UTF-8"),
					},
				},
			},
		},
	})
	if err != nil {
		return fmt.Errorf("SES SendEmail: %w", err)
	}
	return nil
}

func main() {
	lambda.Start(handler)
}

func handler(ctx context.Context) (Result, error) {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	slog.SetDefault(logger)

	secretARN := os.Getenv("DB_SECRET_ARN")
	if secretARN == "" {
		return Result{}, fmt.Errorf("DB_SECRET_ARN environment variable is not set")
	}

	sesFrom := os.Getenv("SES_FROM")
	if sesFrom == "" {
		return Result{}, fmt.Errorf("SES_FROM environment variable is not set")
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

	cfg, err := config.LoadDefaultConfig(ctx)
	if err != nil {
		return Result{}, fmt.Errorf("load AWS config: %w", err)
	}

	db := &PgxDB{pool: pool}
	emailClient := &SESEmailClient{
		client:      sesv2.NewFromConfig(cfg),
		fromAddress: sesFrom,
	}

	result, err := GenerateMonthlyReport(ctx, db, emailClient, time.Now().UTC())
	return result, err
}
