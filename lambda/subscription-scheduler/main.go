package main

import (
	"context"
	"fmt"
	"log/slog"
	"os"
	"time"

	"github.com/aws/aws-lambda-go/lambda"
	"github.com/google/uuid"
	"github.com/is-9108/KakeiBase/lambda/shared"
	"github.com/jackc/pgx/v5/pgxpool"
)

// Subscription はアクティブなサブスクリプションを表す。
type Subscription struct {
	ID         uuid.UUID
	UserID     uuid.UUID
	CategoryID uuid.UUID
	Name       string
	Amount     int
}

// DB はデータベース操作のインターフェース。テスト時にモックへ差し替える。
type DB interface {
	FetchActiveSubscriptions(ctx context.Context) ([]Subscription, error)
	ExistsTransactionForMonth(ctx context.Context, subscriptionID uuid.UUID, year int, month time.Month) (bool, error)
	InsertTransaction(ctx context.Context, tx Transaction) error
}

// Transaction は INSERT する収支レコードを表す。
type Transaction struct {
	ID             uuid.UUID
	UserID         uuid.UUID
	CategoryID     uuid.UUID
	SubscriptionID uuid.UUID
	Amount         int
	Date           time.Time // transaction_date (date 型)
	Memo           string
	CreatedAt      time.Time
	UpdatedAt      time.Time
}

// Result は Lambda の実行結果を表す。
type Result struct {
	Created int `json:"created"`
	Skipped int `json:"skipped"`
	Errors  int `json:"errors"`
}

// ProcessSubscriptions はコアロジック。アクティブなサブスクを取得し、
// 二重実行を防止しつつ当月の transaction を生成する。
func ProcessSubscriptions(ctx context.Context, db DB, now time.Time) Result {
	logger := slog.Default()
	targetDate := time.Date(now.Year(), now.Month(), 1, 0, 0, 0, 0, time.UTC)

	subs, err := db.FetchActiveSubscriptions(ctx)
	if err != nil {
		logger.Error("failed to fetch subscriptions", "error", err)
		return Result{Errors: 1}
	}

	logger.Info("fetched active subscriptions", "count", len(subs))

	var result Result
	for _, sub := range subs {
		exists, err := db.ExistsTransactionForMonth(ctx, sub.ID, now.Year(), now.Month())
		if err != nil {
			logger.Error("failed to check existing transaction", "subscription_id", sub.ID, "error", err)
			result.Errors++
			continue
		}
		if exists {
			logger.Info("transaction already exists, skipping", "subscription_id", sub.ID)
			result.Skipped++
			continue
		}

		tx := Transaction{
			ID:             uuid.New(),
			UserID:         sub.UserID,
			CategoryID:     sub.CategoryID,
			SubscriptionID: sub.ID,
			Amount:         sub.Amount,
			Date:           targetDate,
			Memo:           sub.Name,
			CreatedAt:      now,
			UpdatedAt:      now,
		}

		if err := db.InsertTransaction(ctx, tx); err != nil {
			logger.Error("failed to insert transaction", "subscription_id", sub.ID, "error", err)
			result.Errors++
			continue
		}

		logger.Info("created transaction", "subscription_id", sub.ID, "transaction_id", tx.ID)
		result.Created++
	}

	logger.Info("processing complete", "created", result.Created, "skipped", result.Skipped, "errors", result.Errors)
	return result
}

// PgxDB は pgx を使った DB インターフェースの実装。
type PgxDB struct {
	pool *pgxpool.Pool
}

func (d *PgxDB) FetchActiveSubscriptions(ctx context.Context) ([]Subscription, error) {
	rows, err := d.pool.Query(ctx,
		`SELECT id, user_id, category_id, name, amount FROM subscriptions WHERE is_active = true`)
	if err != nil {
		return nil, fmt.Errorf("query subscriptions: %w", err)
	}
	defer rows.Close()

	var subs []Subscription
	for rows.Next() {
		var s Subscription
		if err := rows.Scan(&s.ID, &s.UserID, &s.CategoryID, &s.Name, &s.Amount); err != nil {
			return nil, fmt.Errorf("scan subscription: %w", err)
		}
		subs = append(subs, s)
	}
	return subs, rows.Err()
}

func (d *PgxDB) ExistsTransactionForMonth(ctx context.Context, subscriptionID uuid.UUID, year int, month time.Month) (bool, error) {
	startOfMonth := time.Date(year, month, 1, 0, 0, 0, 0, time.UTC)
	startOfNext := startOfMonth.AddDate(0, 1, 0)

	var exists bool
	err := d.pool.QueryRow(ctx,
		`SELECT EXISTS(
			SELECT 1 FROM transactions
			WHERE subscription_id = $1
			  AND transaction_date >= $2
			  AND transaction_date < $3
		)`,
		subscriptionID, startOfMonth, startOfNext,
	).Scan(&exists)
	if err != nil {
		return false, fmt.Errorf("check existing transaction: %w", err)
	}
	return exists, nil
}

func (d *PgxDB) InsertTransaction(ctx context.Context, tx Transaction) error {
	_, err := d.pool.Exec(ctx,
		`INSERT INTO transactions (id, user_id, category_id, subscription_id, amount, transaction_date, memo, created_at, updated_at)
		 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)`,
		tx.ID, tx.UserID, tx.CategoryID, tx.SubscriptionID, tx.Amount, tx.Date, tx.Memo, tx.CreatedAt, tx.UpdatedAt,
	)
	if err != nil {
		return fmt.Errorf("insert transaction: %w", err)
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

	db := &PgxDB{pool: pool}
	result := ProcessSubscriptions(ctx, db, time.Now().UTC())
	return result, nil
}
