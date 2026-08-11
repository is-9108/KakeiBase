package shared

import (
	"context"
	"encoding/json"
	"testing"

	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/service/secretsmanager"
)

// mockSMClient は SecretsManagerClient のモック。
type mockSMClient struct {
	secretString string
	err          error
}

func (m *mockSMClient) GetSecretValue(_ context.Context, _ *secretsmanager.GetSecretValueInput, _ ...func(*secretsmanager.Options)) (*secretsmanager.GetSecretValueOutput, error) {
	if m.err != nil {
		return nil, m.err
	}
	return &secretsmanager.GetSecretValueOutput{
		SecretString: aws.String(m.secretString),
	}, nil
}

func TestFetchDBSecret_Success(t *testing.T) {
	secret := DBSecret{
		Host:     "localhost",
		Port:     5432,
		DBName:   "kakeibase",
		Username: "user",
		Password: "pass",
	}
	jsonBytes, _ := json.Marshal(secret)

	client := &mockSMClient{secretString: string(jsonBytes)}
	got, err := FetchDBSecret(context.Background(), client, "arn:aws:secretsmanager:ap-northeast-1:123456789012:secret:test")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}

	if got.Host != secret.Host {
		t.Errorf("Host = %q, want %q", got.Host, secret.Host)
	}
	if got.Port != secret.Port {
		t.Errorf("Port = %d, want %d", got.Port, secret.Port)
	}
	if got.DBName != secret.DBName {
		t.Errorf("DBName = %q, want %q", got.DBName, secret.DBName)
	}
	if got.Username != secret.Username {
		t.Errorf("Username = %q, want %q", got.Username, secret.Username)
	}
	if got.Password != secret.Password {
		t.Errorf("Password = %q, want %q", got.Password, secret.Password)
	}
}

func TestFetchDBSecret_GetSecretError(t *testing.T) {
	client := &mockSMClient{err: context.DeadlineExceeded}
	_, err := FetchDBSecret(context.Background(), client, "arn:aws:secretsmanager:ap-northeast-1:123456789012:secret:test")
	if err == nil {
		t.Fatal("expected error, got nil")
	}
}

func TestFetchDBSecret_InvalidJSON(t *testing.T) {
	client := &mockSMClient{secretString: "not-json"}
	_, err := FetchDBSecret(context.Background(), client, "arn:aws:secretsmanager:ap-northeast-1:123456789012:secret:test")
	if err == nil {
		t.Fatal("expected error, got nil")
	}
}
