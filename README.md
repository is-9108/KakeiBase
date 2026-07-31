# KakeiBase

個人〜家族向けの家計簿管理Webアプリケーション。

自社開発企業への転職を目的としたポートフォリオとして、機能の多さよりも **設計力・テストの質・CI/CDの完成度・開発プロセスの可視化** を重視して作成しています。

![CI](https://github.com/is-9108/KakeiBase/actions/workflows/ci.yml/badge.svg)

## デモ

- 公開URL: (準備中)
- テストアカウント: (準備中)

## スクリーンショット

| ダッシュボード | 収支登録 |
|---|---|
| (準備中) | (準備中) |

## 実装済み機能

| 機能 | バックエンド | フロントエンド | テスト |
|---|---|---|---|
| 認証(ログイン / リフレッシュ / ログアウト) | Done | Done | 単体 |
| ダッシュボード(月次サマリー・カテゴリ別円グラフ) | Done | Done | 単体 |
| 収支管理(CRUD) | Done | Done | 単体 + 結合 |
| カテゴリ管理(CRUD) | Done | Done | 単体 + 結合 |
| サブスク管理(CRUD) | Done | Done | 単体 + 結合 |
| CI パイプライン(PR + main push) | — | — | Done |
| インフラ(Terraform) | — | — | 未着手 |

## なぜこの構成にしたか

技術選定の背景は都度 [ADR](./docs/adr) に記録しています。まずは以下を参照してください。

- [ADR-0001: アーキテクチャ決定記録を残す方針](./docs/adr/0001-record-architecture-decisions.md)
- [ADR-0002: バックエンドは軽量レイヤードアーキテクチャを採用する](./docs/adr/0002-lightweight-layered-architecture.md)
- [ADR-0004: 認証方式としてJWT in HttpOnly Cookieを採用する](./docs/adr/0004-authentication-jwt-httponly-cookie.md)
- [ADR-0008: ネットワークは完全プライベート構成とし、外向き経路はNAT Gateway + S3 Gateway型エンドポイントを採用する](./docs/adr/0008-private-network-nat-gateway.md)
- [ADR-0011: クライアントサイドルーティングにReact Routerを採用する](./docs/adr/0011-client-side-routing-react-router.md)

全ADR一覧は [docs/adr/](./docs/adr) を参照してください。

## アーキテクチャ

```
[利用者](自宅IPのみ許可)
   ├─ CloudFront → S3 (React / Vite, 静的ホスティング)
   └─ CloudFront → ALB(パブリックサブネット) → ECS Fargate(プライベートサブネット) → RDS(プライベートサブネット)

外向き通信: NAT Gateway + S3 Gateway型VPCエンドポイント(プライベートサブネットのECS/Lambda用)

CI/CD: GitHub Actions → ECR → ECS / S3 へ自動デプロイ(予定)
IaC  : Terraform(未着手)
```

バックエンドは1プロジェクト内でのフォルダ分離による軽量レイヤードアーキテクチャ(Domain / Application / Infrastructure / Endpoints)を採用しています。フルのクリーンアーキテクチャではなく、プロジェクトの複雑度に見合う構成をあえて選んだ経緯は [ADR-0002](./docs/adr/0002-lightweight-layered-architecture.md) を参照してください。

依存方向(Endpoints → Infrastructure → Application → Domain)は [NetArchTest](https://github.com/BenMorris/NetArchTest) で機械的に検証しており、違反するとCIが失敗します。

認証は JWT in HttpOnly Cookie 方式を採用しています([ADR-0004](./docs/adr/0004-authentication-jwt-httponly-cookie.md))。

ネットワークはRDS・ECSをプライベートサブネットに配置し、外部からのアクセスは自宅IPのみに限定する完全プライベート構成です。設計判断は [ADR-0008](./docs/adr/0008-private-network-nat-gateway.md) を参照してください。詳細な構成図は [docs/architecture.md](./docs/architecture.md) を参照してください。

## 技術スタック

| 領域 | 技術 |
|---|---|
| フロントエンド | React 19, TypeScript, Vite, React Router v6, Recharts |
| バックエンド | .NET 10 (ASP.NET Core Minimal API), 軽量レイヤードアーキテクチャ |
| データベース | PostgreSQL (Amazon RDS) |
| ORM / バリデーション | Entity Framework Core, FluentValidation |
| 認証 | JWT in HttpOnly Cookie (リフレッシュトークン方式) |
| インフラ | AWS (S3, CloudFront, ALB, ECS Fargate, RDS, VPC/NAT Gateway), Terraform |
| CI/CD | GitHub Actions, Amazon ECR |
| テスト | xUnit, NSubstitute, Testcontainers, NetArchTest, Vitest, React Testing Library, Playwright |

## テスト戦略

| レイヤー | フレームワーク | 対象 |
|---|---|---|
| アーキテクチャテスト | NetArchTest | レイヤー間の依存方向違反を検出 |
| 単体テスト(Backend) | xUnit + NSubstitute | UseCase, ドメインロジック, バリデーター |
| 結合テスト(Backend) | xUnit + Testcontainers | Repository (実PostgreSQLコンテナで検証) |
| 単体テスト(Frontend) | Vitest + React Testing Library | ページコンポーネント, ルーティング |
| E2Eテスト(Frontend) | Playwright | ユーザーフロー (準備中) |

テスト方針: カバレッジを稼ぐためだけのテストは書かない。**境界値・異常系を優先**する。

## 開発プロセス(アピールポイント)

このリポジトリでは、実装だけでなく「なぜそう設計したか」の過程をGitHub上に残しています。

- **[docs/adr/](./docs/adr)** — 技術選定・設計判断の意思決定記録(ADR)。現在12件
- **[docs/devlog.md](./docs/devlog.md)** — 週次の開発ログ・振り返り
- **[Pull Request](https://github.com/is-9108/KakeiBase/pulls?q=is%3Apr+is%3Amerged)** — 各PRの説明欄に設計判断と検討した代替案を記載し、セルフレビューコメントを残しています
- **設計ドキュメント** — [アーキテクチャ](./docs/architecture.md) / [DB設計](./docs/db-design.md) / [API設計](./docs/api-design.md) / [画面設計](./docs/screen-design.md)

## セットアップ

### Docker Compose(推奨)

```bash
docker compose up -d
```

PostgreSQL + バックエンドAPIが起動します。

### 個別起動

初回のみ、`backend/src/KakeiBase.WebApi/appsettings.Development.json` をローカルに作成してください(gitignore対象のため各自で用意する運用です)。

```json
{
  "Jwt": {
    "SecretKey": "任意のランダムな文字列(32文字以上推奨)"
  }
}
```

```bash
# バックエンド
cd backend
dotnet restore
dotnet ef database update --project src/KakeiBase.WebApi
dotnet run --project src/KakeiBase.WebApi

# フロントエンド
cd frontend
npm install
npm run dev
```

## テスト実行

```bash
# バックエンド(単体 + アーキテクチャ + 結合)
cd backend
dotnet test

# アーキテクチャテストのみ
dotnet test tests/KakeiBase.ArchitectureTests

# フロントエンド
cd frontend
npm run test        # Vitest
npm run test:e2e    # Playwright
```

## ディレクトリ構成

```
.
├── backend/
│   ├── src/
│   │   └── KakeiBase.WebApi/       # 単一プロジェクト、フォルダで層を分離
│   │       ├── Domain/             # Entity, ValueObject, ドメインサービス
│   │       ├── Application/        # ユースケース, DTO, インターフェース
│   │       ├── Infrastructure/     # EF Core, Repository実装, 外部連携
│   │       └── Endpoints/          # Minimal API エンドポイント
│   └── tests/
│       ├── KakeiBase.UnitTests/         # Domain/Applicationの単体テスト
│       ├── KakeiBase.ArchitectureTests/ # NetArchTestで依存方向を強制
│       └── KakeiBase.IntegrationTests/  # Testcontainersによる結合テスト
├── frontend/
│   └── src/
│       ├── pages/          # 各画面コンポーネント
│       ├── api/            # API呼び出し層
│       ├── hooks/          # カスタムHooks
│       └── components/     # 共通コンポーネント
├── docs/
│   ├── adr/                # Architecture Decision Records
│   ├── architecture.md     # アーキテクチャ設計
│   ├── db-design.md        # DB設計
│   ├── api-design.md       # API設計
│   └── screen-design.md    # 画面設計
└── .github/
    ├── workflows/ci.yml            # CIパイプライン
    └── PULL_REQUEST_TEMPLATE.md
```

## ライセンス

MIT
