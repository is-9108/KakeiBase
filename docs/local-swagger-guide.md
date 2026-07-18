# ローカル動作確認手順（Swagger UI）

実装済みエンドポイントを Swagger UI でローカル確認するための手順書。

## 前提

- エンドポイントには `RequireAuthorization()` が付いており、認証が必要
- 認証方式はクッキーベース（`access_token` / `HttpOnly` + `SameSite=Strict`）
- Swagger UI はブラウザ上で同一オリジンの `fetch()` を発行するため、ログイン後にセットされた `HttpOnly` クッキーはブラウザが自動送信する
- `AddSecurityDefinition` などの Swagger 設定変更は不要

## 手順

### 1. 事前準備

```bash
# PostgreSQL 起動
docker compose up -d

# API 起動
cd backend
dotnet run --project src/KakeiBase.WebApi
```

Swagger UI: `https://localhost:7227/swagger`

### 2. ログイン（シードユーザー使用）

Swagger UI で `POST /api/auth/login` を "Try it out" し、以下を送信:

```json
{
  "email": "admin@kakeibase.local",
  "password": "Admin@1234!"
}
```

`200 OK` が返れば `access_token` クッキーがブラウザにセットされる。
レスポンスボディは `accessTokenExpiresAt` のみ（トークン文字列は HttpOnly のため非表示）。

### 3. テストデータ投入（任意）

カテゴリを作成:

```json
POST /api/categories
{ "name": "食費", "type": "Expense" }
```

取引を追加:

```json
POST /api/transactions
{
  "categoryId": "<上記で返った categoryId>",
  "amount": 5000,
  "type": "Expense",
  "date": "2026-07-15"
}
```

### 4. ダッシュボード集計を確認

```
GET /api/dashboard/summary?year=2026&month=7
```

ブラウザがクッキーを自動送信するため `200 OK` で集計結果が返る。

### 5. バリデーション確認

| リクエスト | 期待レスポンス |
|---|---|
| `GET /api/dashboard/summary` (year/month 未指定) | `400` |
| `GET /api/dashboard/summary?year=1999&month=7` (year 範囲外) | `400` |
| ログアウト後に `GET /api/dashboard/summary?year=2026&month=7` | `401` |

### 6. アクセストークン期限切れ後（有効期限: 15分）

```
POST /api/auth/refresh
```

を実行するとクッキーが更新される。

## curl で確認する場合

`SameSite=Strict` のためブラウザ経由でないとクッキーが自動送信されない。
curl の場合はクッキーを手動管理する必要がある:

```bash
# ログインしてクッキーを保存
curl -k -c cookies.txt -X POST https://localhost:7227/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@kakeibase.local","password":"Admin@1234!"}'

# クッキーを使ってダッシュボード取得
curl -k -b cookies.txt "https://localhost:7227/api/dashboard/summary?year=2026&month=7"
```
