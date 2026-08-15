package main

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/aws/aws-lambda-go/events"
	"github.com/aws/aws-sdk-go-v2/service/textract"
	"github.com/aws/aws-sdk-go-v2/service/textract/types"
	"github.com/google/uuid"
)

// --- モック実装 ---

type mockS3Client struct {
	data   map[string][]byte
	getErr error
}

func (m *mockS3Client) GetObject(_ context.Context, bucket, key string) ([]byte, error) {
	if m.getErr != nil {
		return nil, m.getErr
	}
	data, ok := m.data[key]
	if !ok {
		return nil, errors.New("not found")
	}
	return data, nil
}

type mockTextractClient struct {
	output *textract.AnalyzeExpenseOutput
	err    error
}

func (m *mockTextractClient) AnalyzeExpense(_ context.Context, _ []byte) (*textract.AnalyzeExpenseOutput, error) {
	if m.err != nil {
		return nil, m.err
	}
	return m.output, nil
}

type mockDB struct {
	existsMap   map[string]bool
	existsErr   error
	categoryID  uuid.UUID
	categoryErr error
	insertErr   error
	inserted    []Transaction
}

func (m *mockDB) ExistsTransactionByReceiptKey(_ context.Context, receiptS3Key string) (bool, error) {
	if m.existsErr != nil {
		return false, m.existsErr
	}
	return m.existsMap[receiptS3Key], nil
}

func (m *mockDB) FindUncategorizedCategoryID(_ context.Context, _ uuid.UUID) (uuid.UUID, error) {
	if m.categoryErr != nil {
		return uuid.Nil, m.categoryErr
	}
	return m.categoryID, nil
}

func (m *mockDB) InsertTransaction(_ context.Context, tx Transaction) error {
	if m.insertErr != nil {
		return m.insertErr
	}
	m.inserted = append(m.inserted, tx)
	return nil
}

// --- ヘルパー ---

func makeS3Record(bucket, key string) events.S3EventRecord {
	return events.S3EventRecord{
		S3: events.S3Entity{
			Bucket: events.S3Bucket{Name: bucket},
			Object: events.S3Object{Key: key},
		},
	}
}

func makeTextractOutput(total, date, vendor string) *textract.AnalyzeExpenseOutput {
	var fields []types.ExpenseField
	if total != "" {
		fields = append(fields, types.ExpenseField{
			Type:           &types.ExpenseType{Text: strPtr("TOTAL")},
			ValueDetection: &types.ExpenseDetection{Text: strPtr(total)},
		})
	}
	if date != "" {
		fields = append(fields, types.ExpenseField{
			Type:           &types.ExpenseType{Text: strPtr("INVOICE_RECEIPT_DATE")},
			ValueDetection: &types.ExpenseDetection{Text: strPtr(date)},
		})
	}
	if vendor != "" {
		fields = append(fields, types.ExpenseField{
			Type:           &types.ExpenseType{Text: strPtr("VENDOR_NAME")},
			ValueDetection: &types.ExpenseDetection{Text: strPtr(vendor)},
		})
	}
	return &textract.AnalyzeExpenseOutput{
		ExpenseDocuments: []types.ExpenseDocument{
			{SummaryFields: fields},
		},
	}
}

func strPtr(s string) *string { return &s }

// --- テスト ---

func TestProcessS3Event_Success(t *testing.T) {
	userID := uuid.MustParse("550e8400-e29b-41d4-a716-446655440000")
	catID := uuid.New()
	key := "receipts/550e8400-e29b-41d4-a716-446655440000/2026/08/20260813_120000.000.jpg"

	s3c := &mockS3Client{data: map[string][]byte{key: {0xFF, 0xD8}}}
	tc := &mockTextractClient{output: makeTextractOutput("1,280", "2026-08-10", "コンビニA")}
	db := &mockDB{existsMap: map[string]bool{}, categoryID: catID}

	now := time.Date(2026, 8, 13, 12, 0, 0, 0, time.UTC)
	result := ProcessS3Event(context.Background(), s3c, tc, db, []events.S3EventRecord{
		makeS3Record("my-bucket", key),
	}, now)

	if result.Created != 1 {
		t.Errorf("Created = %d, want 1", result.Created)
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
	if tx.Amount != 1280 {
		t.Errorf("Amount = %d, want 1280", tx.Amount)
	}
	if tx.Memo != "コンビニA" {
		t.Errorf("Memo = %q, want %q", tx.Memo, "コンビニA")
	}
	if tx.ReceiptS3Key != key {
		t.Errorf("ReceiptS3Key = %q, want %q", tx.ReceiptS3Key, key)
	}
	expectedDate := time.Date(2026, 8, 10, 0, 0, 0, 0, time.UTC)
	if !tx.Date.Equal(expectedDate) {
		t.Errorf("Date = %v, want %v", tx.Date, expectedDate)
	}
}

func TestProcessS3Event_MultipleRecords(t *testing.T) {
	catID := uuid.New()
	key1 := "receipts/550e8400-e29b-41d4-a716-446655440000/2026/08/20260813_120000.000.jpg"
	key2 := "receipts/550e8400-e29b-41d4-a716-446655440000/2026/08/20260813_120001.000.jpg"

	s3c := &mockS3Client{data: map[string][]byte{key1: {0xFF}, key2: {0xFF}}}
	tc := &mockTextractClient{output: makeTextractOutput("500", "2026-08-13", "店B")}
	db := &mockDB{existsMap: map[string]bool{}, categoryID: catID}

	now := time.Date(2026, 8, 13, 12, 0, 0, 0, time.UTC)
	result := ProcessS3Event(context.Background(), s3c, tc, db, []events.S3EventRecord{
		makeS3Record("bucket", key1),
		makeS3Record("bucket", key2),
	}, now)

	if result.Created != 2 {
		t.Errorf("Created = %d, want 2", result.Created)
	}
	if len(db.inserted) != 2 {
		t.Errorf("inserted count = %d, want 2", len(db.inserted))
	}
}

func TestProcessS3Event_AmountParseFail_Skips(t *testing.T) {
	key := "receipts/550e8400-e29b-41d4-a716-446655440000/2026/08/20260813_120000.000.jpg"

	s3c := &mockS3Client{data: map[string][]byte{key: {0xFF}}}
	// TOTAL なしの Textract 出力
	tc := &mockTextractClient{output: makeTextractOutput("", "2026-08-13", "店C")}
	db := &mockDB{existsMap: map[string]bool{}, categoryID: uuid.New()}

	result := ProcessS3Event(context.Background(), s3c, tc, db, []events.S3EventRecord{
		makeS3Record("bucket", key),
	}, time.Now())

	if result.Skipped != 1 {
		t.Errorf("Skipped = %d, want 1", result.Skipped)
	}
	if result.Created != 0 {
		t.Errorf("Created = %d, want 0", result.Created)
	}
}

func TestProcessS3Event_DateParseFail_FallbackToNow(t *testing.T) {
	catID := uuid.New()
	key := "receipts/550e8400-e29b-41d4-a716-446655440000/2026/08/20260813_120000.000.jpg"

	s3c := &mockS3Client{data: map[string][]byte{key: {0xFF}}}
	// 日付がパース不可能
	tc := &mockTextractClient{output: makeTextractOutput("1000", "invalid-date", "店D")}
	db := &mockDB{existsMap: map[string]bool{}, categoryID: catID}

	now := time.Date(2026, 8, 13, 15, 30, 0, 0, time.UTC)
	result := ProcessS3Event(context.Background(), s3c, tc, db, []events.S3EventRecord{
		makeS3Record("bucket", key),
	}, now)

	if result.Created != 1 {
		t.Errorf("Created = %d, want 1", result.Created)
	}
	if len(db.inserted) != 1 {
		t.Fatalf("inserted count = %d, want 1", len(db.inserted))
	}
	// フォールバック日付は now
	if !db.inserted[0].Date.Equal(now) {
		t.Errorf("Date = %v, want %v (fallback to now)", db.inserted[0].Date, now)
	}
}

func TestProcessS3Event_DuplicateReceiptKey_Skips(t *testing.T) {
	key := "receipts/550e8400-e29b-41d4-a716-446655440000/2026/08/20260813_120000.000.jpg"

	s3c := &mockS3Client{data: map[string][]byte{key: {0xFF}}}
	tc := &mockTextractClient{output: makeTextractOutput("1000", "2026-08-13", "店E")}
	db := &mockDB{existsMap: map[string]bool{key: true}, categoryID: uuid.New()}

	result := ProcessS3Event(context.Background(), s3c, tc, db, []events.S3EventRecord{
		makeS3Record("bucket", key),
	}, time.Now())

	if result.Skipped != 1 {
		t.Errorf("Skipped = %d, want 1", result.Skipped)
	}
	if result.Created != 0 {
		t.Errorf("Created = %d, want 0", result.Created)
	}
}

func TestProcessS3Event_S3GetObjectError(t *testing.T) {
	key := "receipts/550e8400-e29b-41d4-a716-446655440000/2026/08/20260813_120000.000.jpg"

	s3c := &mockS3Client{getErr: errors.New("access denied")}
	tc := &mockTextractClient{output: makeTextractOutput("1000", "2026-08-13", "")}
	db := &mockDB{existsMap: map[string]bool{}, categoryID: uuid.New()}

	result := ProcessS3Event(context.Background(), s3c, tc, db, []events.S3EventRecord{
		makeS3Record("bucket", key),
	}, time.Now())

	if result.Errors != 1 {
		t.Errorf("Errors = %d, want 1", result.Errors)
	}
}

func TestProcessS3Event_TextractError(t *testing.T) {
	key := "receipts/550e8400-e29b-41d4-a716-446655440000/2026/08/20260813_120000.000.jpg"

	s3c := &mockS3Client{data: map[string][]byte{key: {0xFF}}}
	tc := &mockTextractClient{err: errors.New("textract error")}
	db := &mockDB{existsMap: map[string]bool{}, categoryID: uuid.New()}

	result := ProcessS3Event(context.Background(), s3c, tc, db, []events.S3EventRecord{
		makeS3Record("bucket", key),
	}, time.Now())

	if result.Errors != 1 {
		t.Errorf("Errors = %d, want 1", result.Errors)
	}
}

func TestProcessS3Event_DBInsertError(t *testing.T) {
	key := "receipts/550e8400-e29b-41d4-a716-446655440000/2026/08/20260813_120000.000.jpg"

	s3c := &mockS3Client{data: map[string][]byte{key: {0xFF}}}
	tc := &mockTextractClient{output: makeTextractOutput("1000", "2026-08-13", "")}
	db := &mockDB{
		existsMap:  map[string]bool{},
		categoryID: uuid.New(),
		insertErr:  errors.New("insert failed"),
	}

	result := ProcessS3Event(context.Background(), s3c, tc, db, []events.S3EventRecord{
		makeS3Record("bucket", key),
	}, time.Now())

	if result.Errors != 1 {
		t.Errorf("Errors = %d, want 1", result.Errors)
	}
	if result.Created != 0 {
		t.Errorf("Created = %d, want 0", result.Created)
	}
}

func TestProcessS3Event_InvalidS3Key(t *testing.T) {
	key := "invalid/key/path.jpg"

	s3c := &mockS3Client{data: map[string][]byte{}}
	tc := &mockTextractClient{output: makeTextractOutput("1000", "2026-08-13", "")}
	db := &mockDB{existsMap: map[string]bool{}, categoryID: uuid.New()}

	result := ProcessS3Event(context.Background(), s3c, tc, db, []events.S3EventRecord{
		makeS3Record("bucket", key),
	}, time.Now())

	if result.Errors != 1 {
		t.Errorf("Errors = %d, want 1", result.Errors)
	}
}

// --- ParseAmount 単体テスト ---

func TestParseAmount(t *testing.T) {
	tests := []struct {
		input string
		want  int
		err   bool
	}{
		{"1,280", 1280, false},
		{"¥1,280", 1280, false},
		{"500", 500, false},
		{"1234.56", 1235, false},
		{"", 0, true},
		{"abc", 0, true},
	}
	for _, tt := range tests {
		got, err := ParseAmount(tt.input)
		if (err != nil) != tt.err {
			t.Errorf("ParseAmount(%q) error = %v, want error = %v", tt.input, err, tt.err)
			continue
		}
		if !tt.err && got != tt.want {
			t.Errorf("ParseAmount(%q) = %d, want %d", tt.input, got, tt.want)
		}
	}
}

// --- ParseUserIDFromKey 単体テスト ---

func TestParseUserIDFromKey(t *testing.T) {
	validID := "550e8400-e29b-41d4-a716-446655440000"
	tests := []struct {
		key  string
		want string
		err  bool
	}{
		{"receipts/" + validID + "/2026/08/test.jpg", validID, false},
		{"receipts/" + validID, validID, false},
		{"invalid/path", "", true},
		{"receipts/not-a-uuid/2026/08/test.jpg", "", true},
	}
	for _, tt := range tests {
		got, err := ParseUserIDFromKey(tt.key)
		if (err != nil) != tt.err {
			t.Errorf("ParseUserIDFromKey(%q) error = %v, want error = %v", tt.key, err, tt.err)
			continue
		}
		if !tt.err && got.String() != tt.want {
			t.Errorf("ParseUserIDFromKey(%q) = %v, want %v", tt.key, got, tt.want)
		}
	}
}
