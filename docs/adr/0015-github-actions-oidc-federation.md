# ADR-0015: GitHub Actions から AWS への認証に OIDC フェデレーションを採用する

## ステータス

承認済み

## コンテキスト

`main` への push で ECS / S3 / Lambda へ自動デプロイする CD パイプラインを構築するにあたり、GitHub Actions が AWS を操作するための認証方式を決める必要がある。

前提として次の制約がある。

- **リポジトリが public である。** フォークからのワークフロー実行や、ログへの出力経路が第三者から見える状態にある
- IAM ユーザーのアクセスキーを GitHub Secrets に置く方式は、**キーが漏れた場合に自動失効しない**。ローテーションも手動運用になる
- インフラは Terraform で管理しており、`terraform apply` は現状ローカルから手動で実行している（[ADR-0008](./0008-private-network-nat-gateway.md)）

加えて、CD ロールにどこまで権限を与えるかという論点がある。CD が `terraform apply` まで実行できれば「push だけで全部が反映される」構成になるが、その場合 CD ロールは VPC・IAM・RDS を作り替えられる広範な権限を持つことになる。

## 決定

GitHub Actions から AWS への認証に **OIDC フェデレーション**を採用する。長期のアクセスキーは発行せず、ワークフロー実行ごとに短命の認証情報を `sts:AssumeRoleWithWebIdentity` で取得する。

信頼ポリシーでは次の2つを必須にする。

1. `token.actions.githubusercontent.com:aud` = `sts.amazonaws.com`
2. `token.actions.githubusercontent.com:sub` = **`repo:is-9108/KakeiBase:ref:refs/heads/main`**（変数 `github_oidc_subjects` で指定）

さらに、**CD ロールには `terraform apply` に必要な権限を与えない**。IaC の適用はローカルからの手動運用を維持する。

## 検討した代替案

### 認証方式

| 案 | メリット | デメリット | 不採用の理由 |
|---|---|---|---|
| **採用案: OIDC フェデレーション** | 長期クレデンシャルが存在しない。ワークフロー単位で失効する。`sub` でリポジトリ・ブランチを限定できる | OIDC プロバイダと信頼ポリシーの設定が必要。プロバイダは AWS アカウントに1つしか作れない | — |
| 代替案A: IAM ユーザーのアクセスキーを GitHub Secrets に置く | 設定が最も単純 | キーが長期有効で、漏洩時に自動失効しない。ローテーションが手動。public リポジトリでは事故時の影響が大きい | セキュリティ上の妥当性が OIDC に対して明確に劣る。ポートフォリオとしても指摘対象になる |
| 代替案B: GitHub Environments の承認機能と併用する | 本番デプロイに人間の承認を挟める | private リポジトリまたは有料プランでないと保護ルールが使えない | public リポジトリの無料プランでは保護ルールが機能しないため、承認ゲートとして成立しない |

### `sub` の絞り込み方

| 案 | 不採用の理由 |
|---|---|
| **採用案: `repo:is-9108/KakeiBase:ref:refs/heads/main`** | — |
| `repo:is-9108/KakeiBase:*` | **public リポジトリでは危険。** フォーク経由の PR やタグ・他ブランチのワークフローからも assume できてしまう |
| `repo:is-9108/KakeiBase:environment:prod` | Environment 保護ルールが使えない（代替案B と同じ理由）ため、`ref` による限定より強くならない |

### CD ロールの権限範囲

| 案 | メリット | デメリット | 不採用の理由 |
|---|---|---|---|
| **採用案: デプロイに必要な操作のみ（ECR push / ECS 更新・RunTask / S3 sync / CloudFront invalidation / Lambda 更新）** | 権限が漏れても壊せる範囲がアプリのデプロイに限定される | インフラ変更のたびに手元で `terraform apply` する必要がある | — |
| 代替案C: CD ロールに `terraform apply` 権限を与える | インフラ変更も push で反映される | IAM・VPC・RDS を作り替えられる広範な権限を GitHub 側の認証に紐づけることになる。`plan` の結果を人間が見ないまま本番へ適用される | IaC の変更は影響が大きく、`plan` の目視確認を挟みたい。得られる自動化の利便性に対してリスクが見合わない |

## 結果・影響

**得られたもの**

- AWS の長期クレデンシャルがリポジトリにも GitHub Secrets にも存在しない状態になる
- CD が assume できるのは `main` ブランチのワークフローだけに限定される。フォークからの PR では assume に失敗する
- 権限がデプロイ操作に限定されているため、万一 CD が乗っ取られてもインフラ構成そのものは壊せない

**犠牲にしたもの・トレードオフ**

- **インフラ変更は自動化されない。** `infra/` を変更したら手元で `terraform apply` を実行する必要がある。CI では `fmt -check` と `validate` までを見る
- 信頼ポリシーにリポジトリ名（`is-9108/KakeiBase`）が埋め込まれるため、**リポジトリ名の変更やフォーク先での運用時には `github_oidc_subjects` の更新が必要**になる
- OIDC プロバイダは AWS アカウント内で URL ごとに1つしか作れない。既にプロバイダが存在するアカウントで適用する場合は `create_github_oidc_provider = false` にして既存を参照する

**受容した残存リスク**

CD ロールは `ecs:RunTask` と `containerOverrides` によるコマンド上書きの権限を持つ（マイグレーションを one-off タスクで実行するため）。これは裏を返すと、**このロールを奪取した攻撃者は ECS タスクとして任意のコマンドを実行でき、タスク定義が Secrets Manager から注入する環境変数（DB 接続文字列・JWT 署名鍵）を読み出せる**ことを意味する。マイグレーションを既存タスク定義の command override で実行する方式（ADR-0008 決定6 の具体化）に内在するリスクであり、権限の絞り込みだけでは消せない。

次の3点で緩和する。

- ロールを assume できるのを `main` ブランチのワークフローに限定する（`sub` の限定）。`main` は保護ブランチであり、変更には PR とステータスチェックの通過が要る
- `github_oidc_subjects` に `:*` を含む値を弾く `validation` を置き、限定を誤って広げられないようにする
- `iam:PassRole` の対象を ECS の2ロールに限定し、`iam:PassedToService` で ECS 以外への委譲を禁じる

**IAM 権限の設計上の注意**

- `ecs:RunTask` は `Condition: ArnEquals ecs:cluster` でクラスタを限定している。タスク定義 ARN はリビジョンが増えるため `:*` のワイルドカードで指定する
- `ecs:RunTask` はタスク定義が参照する2つのロール（タスクロール・実行ロール）を渡すため `iam:PassRole` が必要になる。`iam:PassedToService = ecs-tasks.amazonaws.com` の条件を付け、他サービスへの委譲に流用できないようにしている
- `ecr:GetAuthorizationToken` はリソースレベルの指定ができないため `Resource: "*"` になる。これは AWS 側の仕様上避けられない

**将来見直す条件**

- リポジトリを private 化した場合は、GitHub Environments の保護ルールが使えるようになるため、`sub` を `environment:prod` に切り替えて承認ゲートを挟む構成を再検討する
- 環境が増えた場合（stg 等）は、環境ごとにロールを分け、`sub` もブランチ・Environment 単位で分離する
