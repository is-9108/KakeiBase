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
ブラウザ (自宅IPのみ許可)
  │
  ├── 静的ファイル (HTML/CSS/JS)
  │     CloudFront → S3
  │
  └── API リクエスト
        CloudFront → ALB (パブリックサブネット)
                       → ECS Fargate (プライベートサブネット・アプリ)
                           → Amazon RDS (プライベートサブネット・DB, PostgreSQL)

S3 (レシート画像バケット)
  │
  └── S3イベント通知
        └── Lambda (レシートOCR, プライベートサブネット) → Amazon Textract → Amazon RDS

Amazon EventBridge (毎月1日)
  └── Lambda (月次レポート, プライベートサブネット) → Amazon RDS → Amazon SES → ユーザーメール

プライベートサブネットからの外向き通信:
  ECS / Lambda → NAT Gateway (パブリックサブネット) → インターネット (ECR pull, Textract, SES)
             └→ S3 Gateway型VPCエンドポイント → S3 (ECRイメージレイヤー, レシート画像)
```

### 構成要素

| コンポーネント | サービス | 役割 |
|---|---|---|
| CDN | Amazon CloudFront | 静的ファイル配信、APIキャッシュ制御 |
| 静的ホスティング | Amazon S3 | React SPAのビルド成果物 |
| ロードバランサー | ALB (Application Load Balancer) | HTTPSターミネーション、トラフィック分散(自宅IPのみ許可) |
| APIサーバー | ECS Fargate | .NET 8 Web API コンテナ実行(プライベートサブネット) |
| データベース | Amazon RDS (PostgreSQL) | 家計データ永続化(プライベートサブネット・DB専用) |
| コンテナレジストリ | Amazon ECR | Dockerイメージ管理 |
| IaC | Terraform | インフラ構成管理 |
| S3 (レシートバケット) | Amazon S3 | レシート画像の保存 |
| OCR | Amazon Textract | レシート画像からテキスト抽出 |
| メール送信 | Amazon SES | 月次レポートメール配信 |
| スケジューラー | Amazon EventBridge | 月次レポート Lambda のトリガー |
| サーバーレス処理 | AWS Lambda | レシートOCR / 月次レポート生成 |
| 外向き通信経路 | NAT Gateway (シングルAZ) | プライベートサブネットからインターネット/AWSサービスへの通信 |
| VPCエンドポイント | S3 Gateway型 | S3宛通信をNAT経由にせず直接ルーティング(無料) |

### ネットワーク構成

サブネットはパブリック/プライベート(アプリ)/プライベート(DB専用)の3種類に分離し、セキュリティグループはCIDRではなくSG間参照で連鎖させる。外部からのアクセスは自宅IPのみに限定する。詳細な検討過程・料金試算は [ADR-0008](./adr/0008-private-network-nat-gateway.md) を参照。

| SG | インバウンド | アウトバウンド |
|---|---|---|
| `alb-sg` | 443 を自宅IPのみから許可 | `ecs-sg` の 8080 のみ |
| `ecs-sg` | 8080 を `alb-sg` からのみ | `rds-sg` の 5432、外向き 443 |
| `lambda-sg` | なし | `rds-sg` の 5432、外向き 443 |
| `rds-sg` | 5432 を `ecs-sg` と `lambda-sg` からのみ | なし |

RDSへの管理アクセスは踏み台EC2を置かず、ECS Exec(SSM経由)で稼働中コンテナからpsqlを実行する。マイグレーションはデプロイパイプライン内のECS one-offタスクとして実行する。

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
実装言語はすべて Go で統一する（単一バイナリによるコールドスタートの速さと、AWS SDK for Go v2 の成熟度を優先。詳細は [ADR-0009](./adr/0009-lambda-runtime-go.md) を参照）。
ビルド: `GOOS=linux GOARCH=arm64 go build -o bootstrap main.go`（ARM64 は Lambda の安価なオプション）。

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

**S3 オブジェクトキー形式:** `{yyyy}/{MM}/{yyyyMMdd}_{HHmmss}.{fff}.jpg`(例: `2026/07/20260716_143052.418.jpg`)。年/月フォルダ階層とミリ秒付きタイムスタンプファイル名の採用理由は [ADR-0010](./adr/0010-receipt-image-s3-key-structure.md) を参照。

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
