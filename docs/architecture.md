# アーキテクチャ設計書

## 目次

1. [インフラ構成](#1-インフラ構成)
2. [バックエンド構成](#2-バックエンド構成)
3. [フロントエンド構成](#3-フロントエンド構成)
4. [認証フロー](#4-認証フロー)
5. [テスト戦略](#5-テスト戦略)
6. [サーバーレス処理 (Lambda)](#6-サーバーレス処理-lambda)

---

## 1. インフラ構成

```
ブラウザ
  │
  ├── 静的ファイル (HTML/CSS/JS)
  │     CloudFront → S3
  │
  └── API リクエスト
        CloudFront → ALB → ECS Fargate (コンテナ) → Amazon RDS (PostgreSQL)

S3 (レシート画像バケット)
  │
  └── S3イベント通知
        └── Lambda (レシートOCR) → Amazon Textract → Amazon RDS

Amazon EventBridge (毎月1日)
  └── Lambda (月次レポート) → Amazon RDS → Amazon SES → ユーザーメール
```

### 構成要素

| コンポーネント | サービス | 役割 |
|---|---|---|
| CDN | Amazon CloudFront | 静的ファイル配信、APIキャッシュ制御 |
| 静的ホスティング | Amazon S3 | React SPAのビルド成果物 |
| ロードバランサー | ALB (Application Load Balancer) | HTTPSターミネーション、トラフィック分散 |
| APIサーバー | ECS Fargate | .NET 8 Web API コンテナ実行 |
| データベース | Amazon RDS (PostgreSQL) | 家計データ永続化 |
| コンテナレジストリ | Amazon ECR | Dockerイメージ管理 |
| IaC | Terraform | インフラ構成管理 |
| S3 (レシートバケット) | Amazon S3 | レシート画像の保存 |
| OCR | Amazon Textract | レシート画像からテキスト抽出 |
| メール送信 | Amazon SES | 月次レポートメール配信 |
| スケジューラー | Amazon EventBridge | 月次レポート Lambda のトリガー |
| サーバーレス処理 | AWS Lambda | レシートOCR / 月次レポート生成 |

### CI/CD パイプライン

```
GitHub push/PR
  └── GitHub Actions
        ├── テスト実行 (xUnit / Vitest / Playwright)
        ├── Docker ビルド → ECR push
        └── ECS デプロイ / S3 sync
```

---

## 2. バックエンド構成

### レイヤー構成 (軽量レイヤードアーキテクチャ)

1プロジェクト (`KakeiBase.WebApi`) 内をフォルダで論理分割する。
詳細は [ADR-0002](./adr/0002-lightweight-layered-architecture.md) を参照。

```
KakeiBase.WebApi/
├── Domain/          # Entity, ValueObject, ドメインサービス
├── Application/     # ユースケース, DTO, インターフェース定義
├── Infrastructure/  # EF Core実装, Repository, 外部サービス連携
└── Endpoints/       # Minimal API エンドポイント
```

### 依存方向

```
Endpoints
    ↓
Infrastructure
    ↓
Application
    ↓
Domain
```

- `Domain` は何にも依存しない
- `Application` は `Domain` のみに依存する
- `Infrastructure` は `Application` のインターフェースを実装する
- 逆方向の依存は NetArchTest (アーキテクチャテスト) で CI 上で自動検証する

### 主要技術

| 用途 | ライブラリ |
|---|---|
| Web フレームワーク | ASP.NET Core 8 (Minimal API) |
| O/R マッパー | Entity Framework Core 8 |
| バリデーション | FluentValidation |
| 認証 | ASP.NET Core JWT Bearer |
| テスト | xUnit, NSubstitute, Testcontainers, NetArchTest |

---

## 3. フロントエンド構成

SPA (Single Page Application) として S3 + CloudFront で配信する。

```
frontend/
├── src/
│   ├── api/        # API クライアント (fetch ラッパー)
│   ├── components/ # 共通コンポーネント
│   ├── pages/      # 画面コンポーネント
│   └── hooks/      # カスタム Hooks
└── ...
```

### 主要技術

| 用途 | ライブラリ |
|---|---|
| UI フレームワーク | React 18 |
| 言語 | TypeScript |
| ビルドツール | Vite |
| テスト (ユニット) | Vitest, React Testing Library |
| テスト (E2E) | Playwright |

### 設計方針

- 関数コンポーネント + Hooks のみ使用
- API アクセスは `src/api/` に集約し、コンポーネントから直接 fetch しない
- 状態管理は React 標準 (useState / useContext) を優先する

---

## 4. 認証フロー

詳細は [ADR-0004](./adr/0004-authentication-jwt-httponly-cookie.md) を参照。

### ログイン

```
クライアント                       サーバー
    │                                │
    │  POST /api/auth/login          │
    │  { email, password }           │
    │ ─────────────────────────────► │
    │                                │  BCrypt でパスワード検証
    │                                │  JWT アクセストークン生成 (15分)
    │                                │  リフレッシュトークン生成・DB保存 (7日)
    │  200 OK                        │
    │  Set-Cookie: access_token      │
    │  Set-Cookie: refresh_token     │
    │ ◄───────────────────────────── │
```

### トークンリフレッシュ

```
クライアント                       サーバー
    │                                │
    │  POST /api/auth/refresh        │
    │  Cookie: refresh_token=...     │
    │ ─────────────────────────────► │
    │                                │  refresh_token を DB で検証
    │                                │  新しいアクセストークン生成
    │                                │  リフレッシュトークンをローテーション
    │  200 OK                        │
    │  Set-Cookie: access_token (新) │
    │  Set-Cookie: refresh_token (新)│
    │ ◄───────────────────────────── │
```

### ログアウト

```
クライアント                       サーバー
    │                                │
    │  POST /api/auth/logout         │
    │  Cookie: refresh_token=...     │
    │ ─────────────────────────────► │
    │                                │  refresh_token を DB で無効化
    │  200 OK                        │
    │  Cookie: access_token (削除)   │
    │  Cookie: refresh_token (削除)  │
    │ ◄───────────────────────────── │
```

### Cookie 設定

| 属性 | 値 | 目的 |
|---|---|---|
| `HttpOnly` | true | JavaScript からのアクセスを防ぎ XSS 耐性を確保 |
| `SameSite` | Strict | クロスサイトリクエストでの自動送信を防ぎ CSRF 対策 |
| `Secure` | true (本番) | HTTPS 接続時のみ Cookie を送信 |
| `MaxAge` (access_token) | 15分 | 短命にしてトークン漏洩時の影響を限定 |
| `MaxAge` (refresh_token) | 7日 | 長期セッション維持 |

---

## 5. テスト戦略

### テスト層

| 種別 | プロジェクト | 対象 | ツール |
|---|---|---|---|
| ユニットテスト | `KakeiBase.UnitTests` | Domain / Application ユースケース | xUnit, NSubstitute |
| アーキテクチャテスト | `KakeiBase.ArchitectureTests` | レイヤー間の依存方向 | NetArchTest |
| 統合テスト | `KakeiBase.IntegrationTests` | Repository / DB アクセス | xUnit, Testcontainers |
| フロントエンドユニット | `frontend` | コンポーネント, Hooks | Vitest, React Testing Library |
| E2E テスト | `frontend` | 主要ユーザーフロー | Playwright |

### 方針

- 新しいユースケース・ドメインロジックを追加したら、同じ PR に単体テストを含める
- Repository/DB アクセスを伴う変更は Testcontainers での統合テストを検討する
- アーキテクチャテストは CI で必ず実行し、依存方向の違反を自動検出する
- カバレッジを上げるためだけの意味のないテストは書かない。境界値・異常系を優先する

---

## 6. サーバーレス処理 (Lambda)

詳細な採用理由は [ADR-0005](./adr/0005-receipt-ocr-lambda.md) および [ADR-0006](./adr/0006-monthly-report-lambda-ses.md) を参照。
実装言語はどちらも Python 3.12 で統一する（Boto3 SDK の事例が充実しており、運用コストを下げるため）。

### Lambda #1: レシートOCR

**トリガー:** S3 PutObject イベント（レシート画像バケット）

**処理フロー:**

```
1. S3 から画像を取得
2. Amazon Textract で OCR（AnalyzeExpense API）
3. 金額・日付・店舗名を抽出・パース
4. RDS の transactions テーブルに INSERT (transaction_type=Expense)
5. S3 オブジェクトキーを receipt_s3_key カラムに記録
```

**エラー処理:** パース失敗時はログ出力のみ。DLQ による再処理は将来対応とする。

**関連 API:** `POST /api/receipts/presigned-url` — フロントエンドが S3 に直接アップロードするための Presigned PUT URL を取得する。

---

### Lambda #2: 月次レポート

**トリガー:** Amazon EventBridge Scheduler（毎月1日 08:00 JST）

**処理フロー:**

```
1. 前月の transactions を集計（収入合計・支出合計・カテゴリ別内訳）
2. HTML テンプレートでレポート生成
3. Amazon SES で users.email 宛に送信
```

**メール件名:** `[KakeiBase] YYYY年M月 月次レポート`
