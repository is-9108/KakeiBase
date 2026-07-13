# API 設計書

## 共通仕様

### ベース URL

| 環境 | URL |
|---|---|
| 開発 | `http://localhost:5000` |
| 本番 | `https://api.kakeibase.example.com` (CloudFront 経由) |

### 認証

- httpOnly Cookie `access_token` (JWT) を自動送信
- 有効期限: 15分
- 期限切れ時は `POST /api/auth/refresh` でトークンを更新する

### エラーレスポンス

RFC 7807 (Problem Details) 形式で統一する。

```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "エラーの詳細説明",
  "errors": {
    "email": ["'Email' is not a valid email address."]
  }
}
```

### データ形式

| 項目 | 形式 | 例 |
|---|---|---|
| 日付 | ISO 8601 (`YYYY-MM-DD`) | `"2026-07-13"` |
| 日時 | `yyyy-MM-dd hh:mm:ss` (JST) | `"2026-07-13 10:00:00"` |
| 金額 | 整数（円） | `1500` |
| ID | UUID v4 | `"550e8400-e29b-41d4-a716-446655440000"` |

---

## エンドポイント一覧

### 実装済み

| メソッド | パス | 認証 | 説明 |
|---|---|---|---|
| GET | `/health` | 不要 | ヘルスチェック |
| POST | `/api/auth/login` | 不要 | ログイン |
| POST | `/api/auth/refresh` | Cookie(refresh_token) | トークンリフレッシュ |
| POST | `/api/auth/logout` | Cookie(refresh_token) | ログアウト |

### 計画中（未実装）

| メソッド | パス | 認証 | 説明 |
|---|---|---|---|
| GET | `/api/categories` | 必要 | カテゴリ一覧取得 |
| GET | `/api/categories/{id}` | 必要 | カテゴリ1件取得 |
| POST | `/api/categories` | 必要 | カテゴリ作成 |
| PUT | `/api/categories/{id}` | 必要 | カテゴリ更新 |
| DELETE | `/api/categories/{id}` | 必要 | カテゴリ削除 |
| GET | `/api/transactions` | 必要 | 収支一覧取得 |
| GET | `/api/transactions/{id}` | 必要 | 収支1件取得 |
| POST | `/api/transactions` | 必要 | 収支登録 |
| PUT | `/api/transactions/{id}` | 必要 | 収支更新 |
| DELETE | `/api/transactions/{id}` | 必要 | 収支削除 |
| GET | `/api/subscriptions` | 必要 | サブスク一覧取得 |
| GET | `/api/subscriptions/{id}` | 必要 | サブスク1件取得 |
| POST | `/api/subscriptions` | 必要 | サブスク登録 |
| PUT | `/api/subscriptions/{id}` | 必要 | サブスク更新 |
| DELETE | `/api/subscriptions/{id}` | 必要 | サブスク削除 |
| GET | `/api/dashboard/summary` | 必要 | ダッシュボード集計 |
| POST | `/api/receipts/presigned-url` | 必要 | レシート画像アップロード用 Presigned URL 取得 |

---

## 実装済み API 詳細

### GET `/health`

ヘルスチェック。ロードバランサーの死活監視に使用する。

**レスポンス (200 OK):**
```
Healthy
```

---

### POST `/api/auth/login`

メールアドレスとパスワードで認証し、トークンを Cookie にセットする。

**リクエストボディ:**
```json
{
  "email": "user@example.com",
  "password": "your-password"
}
```

| フィールド | 型 | 必須 | バリデーション |
|---|---|---|---|
| `email` | string | YES | メール形式 |
| `password` | string | YES | 空文字不可 |

**レスポンス (200 OK):**
```json
{
  "accessTokenExpiresAt": "2026-07-13T10:15:00Z"
}
```

**Set-Cookie:**
```
access_token=<JWT>;  HttpOnly; SameSite=Strict; Secure; Max-Age=900
refresh_token=<token>; HttpOnly; SameSite=Strict; Secure; Max-Age=604800
```

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 400 Bad Request | バリデーションエラー (メール形式不正など) |
| 401 Unauthorized | メールアドレスまたはパスワードが不正 |

---

### POST `/api/auth/refresh`

Cookie の `refresh_token` を使ってアクセストークンを更新する。

**リクエスト:** ボディなし。Cookie `refresh_token` を自動送信。

**レスポンス (200 OK):**
```json
{
  "accessTokenExpiresAt": "2026-07-13T10:30:00Z"
}
```

**Set-Cookie:** ログインと同様（新しいトークンペアでローテーション）

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 401 Unauthorized | refresh_token Cookie が存在しない、無効、期限切れ、または無効化済み |

---

### POST `/api/auth/logout`

Cookie を削除し、リフレッシュトークンを DB で無効化する。

**リクエスト:** ボディなし。Cookie `refresh_token` を自動送信。

**レスポンス (200 OK):**

レスポンスボディなし。`refresh_token` と `access_token` Cookie が削除される。

> **注意:** `refresh_token` Cookie が存在しない場合でも 200 OK を返す（冪等性）。

---

## 計画中 API の概要仕様

> 以下は未実装。実装時に詳細を確定する。

### GET `/api/transactions`

クエリパラメータでフィルタリングした収支一覧を返す。

**想定クエリパラメータ:**

| パラメータ | 型 | 説明 |
|---|---|---|
| `year` | integer | 年 |
| `month` | integer | 月 (1-12) |
| `categoryId` | uuid | カテゴリでフィルタ |

### GET `/api/dashboard/summary`

指定月の収支サマリ（収入合計・支出合計・残高・カテゴリ別集計）を返す。

**想定クエリパラメータ:**

| パラメータ | 型 | 説明 |
|---|---|---|
| `year` | integer | 年 |
| `month` | integer | 月 (1-12) |

---

### POST `/api/receipts/presigned-url`

フロントエンドが S3 にレシート画像を直接アップロードするための Presigned PUT URL を取得する。
詳細は [ADR-0005](./adr/0005-receipt-ocr-lambda.md) を参照。

**認証:** 必須（Cookie `access_token`）

**リクエスト:** ボディなし。

**レスポンス (200 OK):**

```json
{
  "uploadUrl": "https://s3.amazonaws.com/receipts-bucket/...",
  "s3Key": "receipts/{userId}/{uuid}.jpg",
  "expiresAt": "2026-07-13T10:15:00Z"
}
```

**アップロードフロー:**

```
1. クライアント → POST /api/receipts/presigned-url → { uploadUrl, s3Key, expiresAt } 取得
2. クライアント → S3 へ直接 PUT（API サーバーを経由しない）
3. S3 イベント通知 → Lambda (レシートOCR) 自動起動
```

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 401 Unauthorized | 未認証 |
