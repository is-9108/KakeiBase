# CLAUDE.md

このファイルはClaude Code(や他のAIコーディングツール)がこのリポジトリで作業する際のガイドです。実装前に必ず目を通してください。

## プロジェクト概要

KakeiBase — 家計簿管理Webアプリケーション。自社開発企業への転職ポートフォリオとして、機能の網羅性よりも **設計力・テストの質・CI/CDの完成度・開発プロセスの可視化** を重視する。

迷ったら「機能を増やす」より「今ある部分の設計・テスト・ドキュメントの質を上げる」を優先する。

## 技術スタック

| 領域 | 技術 |
|---|---|
| フロントエンド | React, TypeScript, Vite |
| バックエンド | .NET 8 (ASP.NET Core Web API) |
| データベース | PostgreSQL |
| インフラ | AWS (S3, CloudFront, ALB, ECS Fargate, RDS), Terraform |
| CI/CD | GitHub Actions, Amazon ECR |
| テスト | xUnit, NSubstitute, Testcontainers, NetArchTest, Vitest, React Testing Library, Playwright |

## アーキテクチャ方針(重要)

バックエンドは **軽量レイヤードアーキテクチャ** ([ADR-0002](./docs/adr/0002-lightweight-layered-architecture.md))。フルのクリーンアーキテクチャ(プロジェクト分離)は採用していない。

- `backend/src/KakeiBase.WebApi/` 1プロジェクト内に以下のフォルダを切る
  - `Domain/` — Entity, ValueObject, ドメインサービス。**何にも依存しない**
  - `Application/` — ユースケース, DTO, インターフェース定義。**Domainのみに依存**
  - `Infrastructure/` — EF Core実装, Repository, 外部サービス連携。**Applicationのインターフェースを実装**
  - `Endpoints/` — Controller / Minimal API
- 依存方向は上から下(Endpoints → Infrastructure → Application → Domain)のみ。逆方向の参照は禁止
- 依存方向は `backend/tests/KakeiBase.ArchitectureTests` の NetArchTest で機械的に検証している。違反するとCIが落ちる
- ドメインロジックが薄いCRUD(カテゴリのCRUDなど)まで無理に層を分けすぎない。予算進捗計算・定期支出の自動生成など、実際にロジックが乗る部分だけ `Domain/` にモデルとしてしっかり書く

新しく層構成やパターン(CQRS、MediatRなど)を導入したくなった場合は、まず提案し、合意が取れたらADRを起票してから実装する。相談なしに大きな構成変更をしない。

## コーディング規約

**バックエンド(.NET)**
- Nullable参照型を有効にする(`<Nullable>enable</Nullable>`)
- 非同期メソッドは `Async` サフィックス、`CancellationToken` を受け取る
- 入力バリデーションは FluentValidation を使う
- Controllerには薄いロジックのみ。ビジネスロジックはApplication層のユースケースに書く

**フロントエンド(React/TypeScript)**
- 関数コンポーネント + Hooksのみ(クラスコンポーネント禁止)
- APIアクセスは `src/api/` に集約し、コンポーネントから直接fetchしない
- 状態管理はまずReact標準(useState/useContext)で足りるかを検討し、安易にライブラリを増やさない
- Tailwindなどのユーティリティクラスを使う場合はコンポーネント内で完結させる

## テストの方針

- 新しいユースケース・ドメインロジックを追加したら、対応する単体テストを同じPRに含める(テストなしのロジック追加は原則NG)
- Repository/DBアクセスを伴う変更はTestcontainersでの結合テストを検討する
- カバレッジを上げるためだけの意味のないテストは書かない。境界値・異常系を優先する

## コミット・PRの規約

- コミットメッセージは [Conventional Commits](https://www.conventionalcommits.org/) (`feat:` `fix:` `refactor:` `test:` `docs:` `chore:`)
- PRは必ず `.github/PULL_REQUEST_TEMPLATE.md` に沿って書く。特に「設計判断・検討した代替案」は省略しない
- 実装が完了したら、diffに対してセルフレビューコメントを付ける(判断に迷った箇所、あとで見直したい箇所)

## ADR(意思決定記録)を書くタイミング

以下に該当する変更を行う場合は、実装前後に `docs/adr/` へADRを追加する([ADR-0001](./docs/adr/0001-record-architecture-decisions.md)参照)。

- 新しいライブラリ・フレームワークの導入
- アーキテクチャパターンの変更
- インフラ構成の変更
- 認証・認可方式の変更
- 複数の実装方針で明確に迷った判断

小さなリファクタリングや単純なバグ修正にはADRは不要。

Claude Codeが実装中に「これはADRを書くべき判断だ」と感じた場合は、実装を進める前に一度立ち止まってユーザーに確認すること。

## devlogについて

`docs/devlog.md` は週次で人間が書くものなので、Claude Codeが自動で追記する必要はない。ただし、大きな意思決定やハマった問題があった場合は「devlogに書いておくと良い内容です」とユーザーに提案してよい。

## よく使うコマンド

```bash
# バックエンド
cd backend
dotnet restore
dotnet build
dotnet test                                   # 全テスト
dotnet test tests/KakeiBase.ArchitectureTests # 依存方向チェックのみ
dotnet run --project src/KakeiBase.WebApi

# フロントエンド
cd frontend
npm install
npm run dev
npm run test        # Vitest
npm run test:e2e    # Playwright
npm run lint

# ローカル環境一括起動
docker compose up -d
```

## やってほしくないこと

- 相談なしにフォルダ構成・アーキテクチャパターンを大きく変更すること
- テストを書かずにロジックを追加すること
- `docs/adr/` の過去の決定と矛盾する実装を、ADRを更新せずに進めること
- 機能追加を優先してREADME/ADR/PRの説明を省略すること(このプロジェクトの価値はそこにある)
