package main

import (
	"context"
	"errors"
	"strings"
	"testing"
	"time"

	"github.com/google/uuid"
)

// mockDB は DB インターフェースのモック実装。
type mockDB struct {
	user     User
	fetchErr error
	txns     []TransactionWithCategory
	txnErr   error
}

func (m *mockDB) FetchUser(_ context.Context) (User, error) {
	if m.fetchErr != nil {
		return User{}, m.fetchErr
	}
	return m.user, nil
}

func (m *mockDB) FetchMonthlyTransactions(_ context.Context, _ uuid.UUID, _ int, _ time.Month) ([]TransactionWithCategory, error) {
	if m.txnErr != nil {
		return nil, m.txnErr
	}
	return m.txns, nil
}

// mockEmailClient は EmailClient インターフェースのモック実装。
type mockEmailClient struct {
	sent    []sentEmail
	sendErr error
}

type sentEmail struct {
	To      string
	Subject string
	Body    string
}

func (m *mockEmailClient) SendEmail(_ context.Context, to, subject, htmlBody string) error {
	if m.sendErr != nil {
		return m.sendErr
	}
	m.sent = append(m.sent, sentEmail{To: to, Subject: subject, Body: htmlBody})
	return nil
}

func TestGenerateMonthlyReport_Success(t *testing.T) {
	db := &mockDB{
		user: User{ID: uuid.New(), Email: "test@example.com"},
		txns: []TransactionWithCategory{
			{Amount: 300000, TransactionType: "Income", CategoryName: "給与"},
			{Amount: 50000, TransactionType: "Expense", CategoryName: "食費"},
			{Amount: 80000, TransactionType: "Expense", CategoryName: "家賃"},
			{Amount: 20000, TransactionType: "Expense", CategoryName: "食費"},
		},
	}

	email := &mockEmailClient{}
	// 2026年8月に実行 → 前月は7月
	now := time.Date(2026, 8, 1, 8, 0, 0, 0, time.UTC)
	result, err := GenerateMonthlyReport(context.Background(), db, email, now)

	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if result.Status != "sent" {
		t.Errorf("Status = %q, want %q", result.Status, "sent")
	}
	if result.Email != "test@example.com" {
		t.Errorf("Email = %q, want %q", result.Email, "test@example.com")
	}
	if result.Year != 2026 || result.Month != 7 {
		t.Errorf("Year/Month = %d/%d, want 2026/7", result.Year, result.Month)
	}

	if len(email.sent) != 1 {
		t.Fatalf("sent count = %d, want 1", len(email.sent))
	}
	if email.sent[0].To != "test@example.com" {
		t.Errorf("To = %q, want %q", email.sent[0].To, "test@example.com")
	}
	if email.sent[0].Subject != "[KakeiBase] 2026年7月 月次レポート" {
		t.Errorf("Subject = %q, want %q", email.sent[0].Subject, "[KakeiBase] 2026年7月 月次レポート")
	}
	body := email.sent[0].Body
	if !strings.Contains(body, "300,000") {
		t.Error("HTML should contain income amount 300,000")
	}
	if !strings.Contains(body, "150,000") {
		t.Error("HTML should contain expense amount 150,000")
	}
}

func TestGenerateMonthlyReport_CategoryBreakdown(t *testing.T) {
	db := &mockDB{
		user: User{ID: uuid.New(), Email: "test@example.com"},
		txns: []TransactionWithCategory{
			{Amount: 60000, TransactionType: "Expense", CategoryName: "食費"},
			{Amount: 40000, TransactionType: "Expense", CategoryName: "交通費"},
		},
	}

	email := &mockEmailClient{}
	now := time.Date(2026, 8, 1, 8, 0, 0, 0, time.UTC)
	result, err := GenerateMonthlyReport(context.Background(), db, email, now)

	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if result.Status != "sent" {
		t.Errorf("Status = %q, want %q", result.Status, "sent")
	}

	body := email.sent[0].Body
	if !strings.Contains(body, "60.0%") {
		t.Error("HTML should contain 60.0% for 食費")
	}
	if !strings.Contains(body, "40.0%") {
		t.Error("HTML should contain 40.0% for 交通費")
	}
}

func TestGenerateMonthlyReport_NoTransactions_Skipped(t *testing.T) {
	db := &mockDB{
		user: User{ID: uuid.New(), Email: "test@example.com"},
		txns: nil,
	}

	email := &mockEmailClient{}
	now := time.Date(2026, 8, 1, 8, 0, 0, 0, time.UTC)
	result, err := GenerateMonthlyReport(context.Background(), db, email, now)

	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if result.Status != "skipped" {
		t.Errorf("Status = %q, want %q", result.Status, "skipped")
	}
	if len(email.sent) != 0 {
		t.Errorf("email should not be sent for zero transactions")
	}
}

func TestGenerateMonthlyReport_ZeroExpense_NoZeroDivision(t *testing.T) {
	db := &mockDB{
		user: User{ID: uuid.New(), Email: "test@example.com"},
		txns: []TransactionWithCategory{
			{Amount: 100000, TransactionType: "Income", CategoryName: "給与"},
		},
	}

	email := &mockEmailClient{}
	now := time.Date(2026, 8, 1, 8, 0, 0, 0, time.UTC)
	result, err := GenerateMonthlyReport(context.Background(), db, email, now)

	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if result.Status != "sent" {
		t.Errorf("Status = %q, want %q", result.Status, "sent")
	}
}

func TestGenerateMonthlyReport_FetchUserError(t *testing.T) {
	db := &mockDB{
		fetchErr: errors.New("connection refused"),
	}

	email := &mockEmailClient{}
	_, err := GenerateMonthlyReport(context.Background(), db, email, time.Now())

	if err == nil {
		t.Fatal("expected error, got nil")
	}
	if !strings.Contains(err.Error(), "fetch user") {
		t.Errorf("error = %q, want to contain 'fetch user'", err.Error())
	}
}

func TestGenerateMonthlyReport_FetchTransactionsError(t *testing.T) {
	db := &mockDB{
		user:   User{ID: uuid.New(), Email: "test@example.com"},
		txnErr: errors.New("query timeout"),
	}

	email := &mockEmailClient{}
	_, err := GenerateMonthlyReport(context.Background(), db, email, time.Now())

	if err == nil {
		t.Fatal("expected error, got nil")
	}
	if !strings.Contains(err.Error(), "fetch transactions") {
		t.Errorf("error = %q, want to contain 'fetch transactions'", err.Error())
	}
}

func TestGenerateMonthlyReport_SendEmailError(t *testing.T) {
	db := &mockDB{
		user: User{ID: uuid.New(), Email: "test@example.com"},
		txns: []TransactionWithCategory{
			{Amount: 10000, TransactionType: "Expense", CategoryName: "食費"},
		},
	}

	email := &mockEmailClient{sendErr: errors.New("SES throttled")}
	now := time.Date(2026, 8, 1, 8, 0, 0, 0, time.UTC)
	_, err := GenerateMonthlyReport(context.Background(), db, email, now)

	if err == nil {
		t.Fatal("expected error, got nil")
	}
	if !strings.Contains(err.Error(), "send email") {
		t.Errorf("error = %q, want to contain 'send email'", err.Error())
	}
}

func TestPrevMonth_January(t *testing.T) {
	now := time.Date(2026, 1, 15, 10, 0, 0, 0, time.UTC)
	year, month := prevMonth(now)

	if year != 2025 {
		t.Errorf("year = %d, want 2025", year)
	}
	if month != time.December {
		t.Errorf("month = %v, want December", month)
	}
}

func TestPrevMonth_August(t *testing.T) {
	now := time.Date(2026, 8, 1, 8, 0, 0, 0, time.UTC)
	year, month := prevMonth(now)

	if year != 2026 {
		t.Errorf("year = %d, want 2026", year)
	}
	if month != time.July {
		t.Errorf("month = %v, want July", month)
	}
}

func TestAggregateTransactions(t *testing.T) {
	txns := []TransactionWithCategory{
		{Amount: 300000, TransactionType: "Income", CategoryName: "給与"},
		{Amount: 50000, TransactionType: "Expense", CategoryName: "食費"},
		{Amount: 80000, TransactionType: "Expense", CategoryName: "家賃"},
		{Amount: 20000, TransactionType: "Expense", CategoryName: "食費"},
	}

	summary := aggregateTransactions(txns, 2026, time.July)

	if summary.TotalIncome != 300000 {
		t.Errorf("TotalIncome = %d, want 300000", summary.TotalIncome)
	}
	if summary.TotalExpense != 150000 {
		t.Errorf("TotalExpense = %d, want 150000", summary.TotalExpense)
	}
	if summary.Balance != 150000 {
		t.Errorf("Balance = %d, want 150000", summary.Balance)
	}
	if len(summary.Breakdown) != 2 {
		t.Fatalf("Breakdown count = %d, want 2", len(summary.Breakdown))
	}
	// 金額降順: 家賃(80000) > 食費(70000)
	if summary.Breakdown[0].Name != "家賃" {
		t.Errorf("Breakdown[0].Name = %q, want 家賃", summary.Breakdown[0].Name)
	}
	if summary.Breakdown[0].Amount != 80000 {
		t.Errorf("Breakdown[0].Amount = %d, want 80000", summary.Breakdown[0].Amount)
	}
	if summary.Breakdown[1].Name != "食費" {
		t.Errorf("Breakdown[1].Name = %q, want 食費", summary.Breakdown[1].Name)
	}
	if summary.Breakdown[1].Amount != 70000 {
		t.Errorf("Breakdown[1].Amount = %d, want 70000", summary.Breakdown[1].Amount)
	}
}

func TestFormatAmount(t *testing.T) {
	tests := []struct {
		input int
		want  string
	}{
		{0, "0"},
		{999, "999"},
		{1000, "1,000"},
		{12345, "12,345"},
		{1234567, "1,234,567"},
		{-5000, "-5,000"},
	}

	for _, tt := range tests {
		got := formatAmount(tt.input)
		if got != tt.want {
			t.Errorf("formatAmount(%d) = %q, want %q", tt.input, got, tt.want)
		}
	}
}

func TestRenderHTML(t *testing.T) {
	summary := MonthlySummary{
		Year:         2026,
		Month:        7,
		TotalIncome:  300000,
		TotalExpense: 150000,
		Balance:      150000,
		Breakdown: []CategoryBreakdown{
			{Name: "家賃", Amount: 80000, Percentage: 53.3},
			{Name: "食費", Amount: 70000, Percentage: 46.7},
		},
	}

	html, err := renderHTML(summary)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}

	checks := []string{
		"2026年7月",
		"300,000",
		"150,000",
		"家賃",
		"食費",
		"53.3%",
		"46.7%",
	}
	for _, check := range checks {
		if !strings.Contains(html, check) {
			t.Errorf("HTML should contain %q", check)
		}
	}
}
