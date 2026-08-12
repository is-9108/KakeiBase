package main

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/google/uuid"
)

// mockDB は DB インターフェースのモック実装。
type mockDB struct {
	subscriptions []Subscription
	fetchErr      error
	existsMap     map[uuid.UUID]bool
	existsErr     error
	insertErr     error
	inserted      []Transaction
}

func (m *mockDB) FetchActiveSubscriptions(_ context.Context) ([]Subscription, error) {
	if m.fetchErr != nil {
		return nil, m.fetchErr
	}
	return m.subscriptions, nil
}

func (m *mockDB) ExistsTransactionForMonth(_ context.Context, subscriptionID uuid.UUID, _ int, _ time.Month) (bool, error) {
	if m.existsErr != nil {
		return false, m.existsErr
	}
	return m.existsMap[subscriptionID], nil
}

func (m *mockDB) InsertTransaction(_ context.Context, tx Transaction) error {
	if m.insertErr != nil {
		return m.insertErr
	}
	m.inserted = append(m.inserted, tx)
	return nil
}

func TestProcessSubscriptions_CreatesTransactions(t *testing.T) {
	subID := uuid.New()
	userID := uuid.New()
	catID := uuid.New()

	db := &mockDB{
		subscriptions: []Subscription{
			{ID: subID, UserID: userID, CategoryID: catID, Name: "Netflix", Amount: -1490},
		},
		existsMap: map[uuid.UUID]bool{},
	}

	now := time.Date(2026, 8, 11, 10, 0, 0, 0, time.UTC)
	result := ProcessSubscriptions(context.Background(), db, now)

	if result.Created != 1 {
		t.Errorf("Created = %d, want 1", result.Created)
	}
	if result.Skipped != 0 {
		t.Errorf("Skipped = %d, want 0", result.Skipped)
	}
	if result.Errors != 0 {
		t.Errorf("Errors = %d, want 0", result.Errors)
	}

	if len(db.inserted) != 1 {
		t.Fatalf("inserted count = %d, want 1", len(db.inserted))
	}

	tx := db.inserted[0]
	if tx.UserID != userID {
		t.Errorf("UserID = %v, want %v", tx.UserID, userID)
	}
	if tx.CategoryID != catID {
		t.Errorf("CategoryID = %v, want %v", tx.CategoryID, catID)
	}
	if tx.SubscriptionID != subID {
		t.Errorf("SubscriptionID = %v, want %v", tx.SubscriptionID, subID)
	}
	if tx.Amount != -1490 {
		t.Errorf("Amount = %d, want -1490", tx.Amount)
	}
	if tx.Memo != "Netflix" {
		t.Errorf("Memo = %q, want %q", tx.Memo, "Netflix")
	}

	// transaction_date は実行月の1日
	expectedDate := time.Date(2026, 8, 1, 0, 0, 0, 0, time.UTC)
	if !tx.Date.Equal(expectedDate) {
		t.Errorf("Date = %v, want %v", tx.Date, expectedDate)
	}
}

func TestProcessSubscriptions_SkipsDuplicate(t *testing.T) {
	subID := uuid.New()

	db := &mockDB{
		subscriptions: []Subscription{
			{ID: subID, UserID: uuid.New(), CategoryID: uuid.New(), Name: "Spotify", Amount: -980},
		},
		existsMap: map[uuid.UUID]bool{subID: true},
	}

	now := time.Date(2026, 8, 11, 10, 0, 0, 0, time.UTC)
	result := ProcessSubscriptions(context.Background(), db, now)

	if result.Created != 0 {
		t.Errorf("Created = %d, want 0", result.Created)
	}
	if result.Skipped != 1 {
		t.Errorf("Skipped = %d, want 1", result.Skipped)
	}
	if result.Errors != 0 {
		t.Errorf("Errors = %d, want 0", result.Errors)
	}
}

func TestProcessSubscriptions_FetchError(t *testing.T) {
	db := &mockDB{
		fetchErr: errors.New("connection refused"),
	}

	result := ProcessSubscriptions(context.Background(), db, time.Now())

	if result.Errors != 1 {
		t.Errorf("Errors = %d, want 1", result.Errors)
	}
	if result.Created != 0 {
		t.Errorf("Created = %d, want 0", result.Created)
	}
}

func TestProcessSubscriptions_ExistsCheckError(t *testing.T) {
	db := &mockDB{
		subscriptions: []Subscription{
			{ID: uuid.New(), UserID: uuid.New(), CategoryID: uuid.New(), Name: "Test", Amount: -500},
		},
		existsErr: errors.New("query failed"),
	}

	result := ProcessSubscriptions(context.Background(), db, time.Now())

	if result.Errors != 1 {
		t.Errorf("Errors = %d, want 1", result.Errors)
	}
	if result.Created != 0 {
		t.Errorf("Created = %d, want 0", result.Created)
	}
}

func TestProcessSubscriptions_InsertError(t *testing.T) {
	db := &mockDB{
		subscriptions: []Subscription{
			{ID: uuid.New(), UserID: uuid.New(), CategoryID: uuid.New(), Name: "Test", Amount: -500},
		},
		existsMap: map[uuid.UUID]bool{},
		insertErr: errors.New("insert failed"),
	}

	result := ProcessSubscriptions(context.Background(), db, time.Now())

	if result.Errors != 1 {
		t.Errorf("Errors = %d, want 1", result.Errors)
	}
	if result.Created != 0 {
		t.Errorf("Created = %d, want 0", result.Created)
	}
}

func TestProcessSubscriptions_MultipleSubscriptions(t *testing.T) {
	sub1 := uuid.New()
	sub2 := uuid.New()
	sub3 := uuid.New()

	db := &mockDB{
		subscriptions: []Subscription{
			{ID: sub1, UserID: uuid.New(), CategoryID: uuid.New(), Name: "Netflix", Amount: -1490},
			{ID: sub2, UserID: uuid.New(), CategoryID: uuid.New(), Name: "Spotify", Amount: -980},
			{ID: sub3, UserID: uuid.New(), CategoryID: uuid.New(), Name: "iCloud", Amount: -400},
		},
		existsMap: map[uuid.UUID]bool{sub2: true}, // sub2 は既に存在
	}

	now := time.Date(2026, 8, 1, 0, 0, 0, 0, time.UTC)
	result := ProcessSubscriptions(context.Background(), db, now)

	if result.Created != 2 {
		t.Errorf("Created = %d, want 2", result.Created)
	}
	if result.Skipped != 1 {
		t.Errorf("Skipped = %d, want 1", result.Skipped)
	}
	if result.Errors != 0 {
		t.Errorf("Errors = %d, want 0", result.Errors)
	}
}

func TestProcessSubscriptions_EmptySubscriptions(t *testing.T) {
	db := &mockDB{
		subscriptions: []Subscription{},
	}

	result := ProcessSubscriptions(context.Background(), db, time.Now())

	if result.Created != 0 {
		t.Errorf("Created = %d, want 0", result.Created)
	}
	if result.Skipped != 0 {
		t.Errorf("Skipped = %d, want 0", result.Skipped)
	}
	if result.Errors != 0 {
		t.Errorf("Errors = %d, want 0", result.Errors)
	}
}
