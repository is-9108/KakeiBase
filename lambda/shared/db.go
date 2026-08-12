package shared

import (
	"context"
	"encoding/json"
	"fmt"

	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/config"
	"github.com/aws/aws-sdk-go-v2/service/secretsmanager"
	"github.com/jackc/pgx/v5/pgxpool"
)

// DBSecret は Secrets Manager に格納されている RDS 接続情報の構造体。
type DBSecret struct {
	Host     string `json:"host"`
	Port     int    `json:"port"`
	DBName   string `json:"dbname"`
	Username string `json:"username"`
	Password string `json:"password"`
}

// SecretsManagerClient は Secrets Manager からシークレットを取得するインターフェース。
type SecretsManagerClient interface {
	GetSecretValue(ctx context.Context, params *secretsmanager.GetSecretValueInput, optFns ...func(*secretsmanager.Options)) (*secretsmanager.GetSecretValueOutput, error)
}

// FetchDBSecret は Secrets Manager から DB 接続情報を取得する。
func FetchDBSecret(ctx context.Context, client SecretsManagerClient, secretARN string) (*DBSecret, error) {
	output, err := client.GetSecretValue(ctx, &secretsmanager.GetSecretValueInput{
		SecretId: aws.String(secretARN),
	})
	if err != nil {
		return nil, fmt.Errorf("failed to get secret %s: %w", secretARN, err)
	}

	var secret DBSecret
	if err := json.Unmarshal([]byte(*output.SecretString), &secret); err != nil {
		return nil, fmt.Errorf("failed to parse secret JSON: %w", err)
	}

	return &secret, nil
}

// ConnectDB は DBSecret から pgxpool.Pool を作成する。
func ConnectDB(ctx context.Context, secret *DBSecret) (*pgxpool.Pool, error) {
	dsn := fmt.Sprintf(
		"host=%s port=%d dbname=%s user=%s password=%s sslmode=require",
		secret.Host, secret.Port, secret.DBName, secret.Username, secret.Password,
	)

	pool, err := pgxpool.New(ctx, dsn)
	if err != nil {
		return nil, fmt.Errorf("failed to create connection pool: %w", err)
	}

	if err := pool.Ping(ctx); err != nil {
		pool.Close()
		return nil, fmt.Errorf("failed to ping database: %w", err)
	}

	return pool, nil
}

// NewSecretsManagerClient は デフォルト設定で SecretsManager クライアントを生成する。
func NewSecretsManagerClient(ctx context.Context) (SecretsManagerClient, error) {
	cfg, err := config.LoadDefaultConfig(ctx)
	if err != nil {
		return nil, fmt.Errorf("failed to load AWS config: %w", err)
	}
	return secretsmanager.NewFromConfig(cfg), nil
}
