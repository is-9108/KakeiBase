# API 設計書

## 共通仕様

### ベース URL

| 環境 | URL |
|---|---|
| 開発 | `http://localhost:5173` (Viteプロキシ経由、実体は `https://localhost:7227`) |
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

### GET `/api/categories`

ログインユーザーのカテゴリ一覧を返す。

**クエリパラメータ:** なし

**レスポンス (200 OK):**
```json
[
  {
    "id": "550e8400-...",
    "name": "食費",
    "type": "Expense",
    "createdAt": "2026-07-13T10:00:00+09:00"
  }
]
```

---

### GET `/api/categories/{id}`

カテゴリを1件取得する。

**レスポンス (200 OK):**
```json
{
  "id": "550e8400-...",
  "name": "食費",
  "type": "Expense",
  "createdAt": "2026-07-13T10:00:00+09:00"
}
```

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 404 Not Found | 指定IDのカテゴリが存在しない、または他ユーザーのカテゴリ |

---

### POST `/api/categories`

カテゴリを作成する。

**リクエストボディ:**
```json
{
  "name": "食費",
  "type": "Expense"
}
```

| フィールド | 型 | 必須 | バリデーション |
|---|---|---|---|
| `name` | string | YES | 空文字不可、最大100文字 |
| `type` | string | YES | `Income` または `Expense` |

**レスポンス (201 Created):**
```json
{
  "id": "550e8400-...",
  "name": "食費",
  "type": "Expense",
  "createdAt": "2026-07-13T10:00:00+09:00"
}
```

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 400 Bad Request | バリデーションエラー |
| 409 Conflict | 同一ユーザー内でカテゴリ名と種別の組み合わせが重複 |

---

### PUT `/api/categories/{id}`

カテゴリを更新する。

**リクエストボディ:** `POST /api/categories` と同じ形式

**レスポンス (200 OK):** 更新後のカテゴリオブジェクト

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 400 Bad Request | バリデーションエラー |
| 404 Not Found | 指定IDのカテゴリが存在しない |
| 409 Conflict | 更新後の名前と種別の組み合わせが既存カテゴリと重複 |

---

### DELETE `/api/categories/{id}`

カテゴリを削除する。

**レスポンス (204 No Content):** 削除成功

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 404 Not Found | 指定IDのカテゴリが存在しない |

> **注意:** 取引やサブスクで使用中のカテゴリは DB の RESTRICT 制約により削除不可。

---

### GET `/api/transactions`

クエリパラメータでフィルタリングした収支一覧を返す。パラメータはすべて任意。

**クエリパラメータ:**

| パラメータ | 型 | 必須 | 説明 |
|---|---|---|---|
| `year` | integer | NO | 年でフィルタ |
| `month` | integer | NO | 月 (1-12) でフィルタ |
| `categoryId` | uuid | NO | カテゴリでフィルタ |

**レスポンス (200 OK):**
```json
[
  {
    "id": "550e8400-...",
    "categoryId": "660e8400-...",
    "categoryName": "食費",
    "subscriptionId": null,
    "amount": 800,
    "date": "2026-07-13",
    "memo": "スーパー",
    "receiptS3Key": null,
    "createdAt": "2026-07-13T10:00:00+09:00",
    "updatedAt": "2026-07-13T10:00:00+09:00"
  }
]
```

---

### GET `/api/transactions/{id}`

収支を1件取得する。

**レスポンス (200 OK):** 上記と同じ形式の単一オブジェクト

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 404 Not Found | 指定IDの取引が存在しない |

---

### POST `/api/transactions`

収支を登録する。

**リクエストボディ:**
```json
{
  "categoryId": "660e8400-...",
  "amount": 800,
  "date": "2026-07-13",
  "memo": "スーパー",
  "receiptS3Key": null
}
```

| フィールド | 型 | 必須 | バリデーション |
|---|---|---|---|
| `categoryId` | uuid | YES | 空でないこと |
| `amount` | integer | YES | 1以上 |
| `date` | string (ISO 8601) | YES | 空でないこと |
| `memo` | string | NO | 最大500文字 |
| `receiptS3Key` | string | NO | |

**レスポンス (201 Created):** 作成された取引オブジェクト

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 400 Bad Request | バリデーションエラー |
| 404 Not Found | 指定カテゴリが存在しない |

---

### PUT `/api/transactions/{id}`

収支を更新する。

**リクエストボディ:** `POST /api/transactions` と同じ形式

**レスポンス (200 OK):** 更新後の取引オブジェクト

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 400 Bad Request | バリデーションエラー |
| 404 Not Found | 指定IDの取引またはカテゴリが存在しない |

---

### DELETE `/api/transactions/{id}`

収支を削除する。

**レスポンス (204 No Content):** 削除成功

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 404 Not Found | 指定IDの取引が存在しない |

---

### GET `/api/subscriptions`

サブスク一覧を返す。`isActive` パラメータで有効/無効をフィルタできる。

**クエリパラメータ:**

| パラメータ | 型 | 必須 | 説明 |
|---|---|---|---|
| `isActive` | boolean | NO | `true`: 有効のみ、`false`: 無効のみ、未指定: 全件 |

**レスポンス (200 OK):**
```json
[
  {
    "id": "550e8400-...",
    "categoryId": "660e8400-...",
    "name": "Netflix",
    "amount": 1490,
    "isActive": true,
    "createdAt": "2026-07-01T00:00:00+09:00",
    "updatedAt": "2026-07-01T00:00:00+09:00"
  }
]
```

---

### GET `/api/subscriptions/{id}`

サブスクを1件取得する。

**レスポンス (200 OK):** 上記と同じ形式の単一オブジェクト

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 404 Not Found | 指定IDのサブスクが存在しない |

---

### POST `/api/subscriptions`

サブスクを登録する。

**リクエストボディ:**
```json
{
  "name": "Netflix",
  "amount": 1490
}
```

| フィールド | 型 | 必須 | バリデーション |
|---|---|---|---|
| `name` | string | YES | 空文字不可、最大100文字 |
| `amount` | integer | YES | 1以上 |

**レスポンス (201 Created):** 作成されたサブスクオブジェクト

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 400 Bad Request | バリデーションエラー |

---

### PUT `/api/subscriptions/{id}`

サブスクを更新する。

**リクエストボディ:**
```json
{
  "name": "Netflix",
  "amount": 1490,
  "isActive": true
}
```

| フィールド | 型 | 必須 | バリデーション |
|---|---|---|---|
| `name` | string | YES | 空文字不可、最大100文字 |
| `amount` | integer | YES | 1以上 |
| `isActive` | boolean | YES | |

**レスポンス (200 OK):** 更新後のサブスクオブジェクト

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 400 Bad Request | バリデーションエラー |
| 404 Not Found | 指定IDのサブスクが存在しない |

---

### DELETE `/api/subscriptions/{id}`

サブスクを削除する。

**レスポンス (204 No Content):** 削除成功

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 404 Not Found | 指定IDのサブスクが存在しない |

---

### GET `/api/dashboard/summary`

指定月の収支サマリ（収入合計・支出合計・残高・カテゴリ別集計・直近取引）を返す。

**クエリパラメータ:**

| パラメータ | 型 | 必須 | バリデーション | 説明 |
|---|---|---|---|---|
| `year` | integer | YES | 2000〜2100 | 年 |
| `month` | integer | YES | 1〜12 | 月 |

**レスポンス (200 OK):**
```json
{
  "totalIncome": 200000,
  "totalExpense": 150000,
  "balance": 50000,
  "categoryBreakdown": [
    { "categoryName": "食費", "amount": 60000, "percentage": 40.0 },
    { "categoryName": "家賃", "amount": 80000, "percentage": 53.3 }
  ],
  "recentTransactions": [
    {
      "id": "550e8400-...",
      "categoryName": "食費",
      "type": "Expense",
      "amount": 800,
      "date": "2026-07-13",
      "memo": "スーパー"
    }
  ]
}
```

**エラーレスポンス:**

| ステータス | 条件 |
|---|---|
| 400 Bad Request | year/month 未指定または範囲外 |

---

## POST `/api/receipts/presigned-url`

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
