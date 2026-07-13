# KakeiBase

個人〜家族向けの家計簿管理Webアプリケーション。

自社開発企業への転職を目的としたポートフォリオとして、機能の多さよりも **設計力・テストの質・CI/CDの完成度・開発プロセスの可視化** を重視して作成しています。

![CI](https://github.com/<your-account>/kakeibase/actions/workflows/ci.yml/badge.svg)
![coverage](https://img.shields.io/codecov/c/github/<your-account>/kakeibase)

## デモ

- 公開URL: (準備中)
- テストアカウント: (準備中)

## スクリーンショット

| ダッシュボード | 収支登録 |
|---|---|
| (準備中) | (準備中) |

## なぜこの構成にしたか

技術選定の背景は都度 [ADR](./docs/adr) に記録しています。まずは以下を参照してください。

- [ADR-0001: アーキテクチャ決定記録を残す方針](./docs/adr/0001-record-architecture-decisions.md)
- [ADR-0002: バックエンドは軽量レイヤードアーキテクチャを採用する](./docs/adr/0002-lightweight-layered-architecture.md)
- [ADR-0008: ネットワークは完全プライベート構成とし、外向き経路はNAT Gateway + S3 Gateway型エンドポイントを採用する](./docs/adr/0008-private-network-nat-gateway.md)

## アーキテクチャ

```
[利用者](自宅IPのみ許可)
   ├─ CloudFront → S3 (React / Vite, 静的ホスティング)
   └─ CloudFront → ALB(パブリックサブネット) → ECS Fargate(プライベートサブネット) → RDS(プライベートサブネット)

外向き通信: NAT Gateway + S3 Gateway型VPCエンドポイント(プライベートサブネットのECS/Lambda用)

CI/CD: GitHub Actions → ECR → ECS / S3 へ自動デプロイ
IaC  : Terraform
```

バックエンドは1プロジェクト内でのフォルダ分離による軽量レイヤードアーキテクチャ(Domain / Application / Infrastructure)を採用しています。フルのクリーンアーキテクチャではなく、プロジェクトの複雑度に見合う構成をあえて選んだ経緯は [ADR-0002](./docs/adr/0002-lightweight-layered-architecture.md) を参照してください。

ネットワークはRDS・ECSをプライベートサブネットに配置し、外部からのアクセスは自宅IPのみに限定する完全プライベート構成です。設計判断は [ADR-0008](./docs/adr/0008-private-network-nat-gateway.md) を参照してください。詳細な構成図は [docs/architecture.md](./docs/architecture.md) を参照してください。

## 技術スタック

| 領域 | 技術 |
|---|---|
| フロントエンド | React, TypeScript, Vite |
| バックエンド | .NET 8 (ASP.NET Core Web API), 軽量レイヤードアーキテクチャ |
| データベース | PostgreSQL (Amazon RDS) |
| インフラ | AWS (S3, CloudFront, ALB, ECS Fargate, RDS, VPC/NAT Gateway), Terraform |
| CI/CD | GitHub Actions, Amazon ECR |
| テスト | xUnit, NSubstitute, Testcontainers, Vitest, React Testing Library, Playwright |

## 開発プロセス(アピールポイント)

このリポジトリでは、実装だけでなく「なぜそう設計したか」の過程をGitHub上に残しています。

- **[docs/adr/](./docs/adr)** — 技術選定・設計判断の意思決定記録(ADR)
- **[docs/devlog.md](./docs/devlog.md)** — 週次の開発ログ・振り返り
- **Pull Request** — 各PRの説明欄に設計判断と検討した代替案を記載し、セルフレビューコメントを残しています(過去のPR一覧はこちら)
- **GitHub Discussions** — 実装前の設計検討や技術調査のやり取りを記録

## セットアップ

```bash
# バックエンド
cd backend
dotnet restore
dotnet run --project src/KakeiBase.WebApi

# フロントエンド
cd frontend
npm install
npm run dev

# ローカル環境一括起動(Docker)
docker compose up -d
```

## テスト実行

```bash
# バックエンド(単体 + 結合)
dotnet test

# フロントエンド
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
│   │       └── Endpoints/          # Controller / Minimal API
│   └── tests/
│       ├── KakeiBase.UnitTests/         # Domain/Applicationの単体テスト
│       ├── KakeiBase.ArchitectureTests/ # NetArchTestで依存方向を強制
│       └── KakeiBase.IntegrationTests/  # Testcontainersによる結合テスト
├── frontend/
├── infra/            # Terraform
├── docs/
│   ├── adr/
│   └── devlog.md
└── .github/
    ├── workflows/
    └── PULL_REQUEST_TEMPLATE.md
```

## ライセンス

MIT
