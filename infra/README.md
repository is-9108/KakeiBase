# KakeiBase Infrastructure (Terraform)

AWS インフラを Terraform で管理する。構成の全体像は [`docs/aws-architecture.html`](../docs/aws-architecture.html) を参照。

## ディレクトリ構成

```
infra/
├── envs/prod/              # 環境固有の設定 (backend, variables, module呼び出し)
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   └── terraform.tfvars.example
└── modules/
    ├── network/            # VPC, Subnet, NAT GW, IGW, S3 VPC Endpoint
    ├── security/           # Security Groups (ALB/ECS/Lambda/RDS)
    ├── database/           # RDS PostgreSQL + Secrets Manager
    ├── ecr/                # ECR リポジトリ
    ├── ecs/                # ALB + ECS Fargate + IAM (ECS Exec対応)
    ├── storage/            # S3 レシートバケット
    ├── frontend/           # S3 + CloudFront (OAC, SPA対応)
    ├── waf/                # WAF WebACL + IPSet (CloudFront スコープ / us-east-1)
    ├── lambda/             # Lambda x3 + EventBridge + S3通知 + SES
    └── cicd/               # GitHub OIDC プロバイダ + デプロイ用 IAM ロール
```

## 前提条件

- [Terraform](https://developer.hashicorp.com/terraform/downloads) >= 1.10 (state の S3 ネイティブロックに必要)
- [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html) v2
- AWS アカウントに AdministratorAccess 相当の権限を持つ IAM ユーザー/ロール
- `aws configure` で認証情報が設定済み

## 初回セットアップ

### 1. State backend 用の S3 バケットを作成

state を置くバケット自体は Terraform で作れない (鶏卵問題) ため、ここだけ手動で用意する。

```bash
aws s3api create-bucket \
  --bucket kakeibase-terraform-state \
  --region ap-northeast-1 \
  --create-bucket-configuration LocationConstraint=ap-northeast-1

# state の破損・誤削除から復旧できるようにする
aws s3api put-bucket-versioning \
  --bucket kakeibase-terraform-state \
  --versioning-configuration Status=Enabled

# state には RDS エンドポイントや Secrets Manager の ARN が含まれる
aws s3api put-bucket-encryption \
  --bucket kakeibase-terraform-state \
  --server-side-encryption-configuration \
  '{"Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"AES256"}}]}'

aws s3api put-public-access-block \
  --bucket kakeibase-terraform-state \
  --public-access-block-configuration \
  BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true
```

> state のロックは `backend "s3"` の `use_lockfile = true` による S3 ネイティブロックを使う。
> DynamoDB テーブルの作成は不要。

### 2. tfvars を作成

```bash
cd infra/envs/prod
cp terraform.tfvars.example terraform.tfvars
```

`terraform.tfvars` を編集し、自宅の IP アドレスを設定する:

```hcl
allowed_cidr = ["203.0.113.1/32"]  # 自分のグローバル IP (複数指定可)

# 月次レポートメールを使う場合 (任意)
# ses_sender_email = "you@example.com"
```

### 3. Terraform 初期化 & デプロイ

```bash
terraform init
terraform plan        # 作成されるリソースを確認
terraform apply       # 実行 (確認プロンプトで yes)
```

> **別プラットフォームで開発する場合**
> `.terraform.lock.hcl` は Git で追跡しており、現在は `windows_amd64` (開発環境) と
> `linux_amd64` (CI) のハッシュのみを含む。macOS など別のプラットフォームで作業するときは、
> 先に下記を実行してハッシュを追加し、その差分もコミットする。
> そうしないと `terraform init` が lock ファイルを書き換えて意図しない差分が出る。
>
> ```bash
> terraform providers lock -platform=darwin_arm64
> ```

`apply` 完了後、以下の output が表示される:

| Output | 用途 |
|---|---|
| `cloudfront_domain` | アプリのアクセス URL (`https://xxx.cloudfront.net`) |
| `alb_dns_name` | ALB の DNS 名 |
| `ecr_repository_url` | Docker イメージの push 先 |
| `ecs_cluster_name` / `ecs_service_name` | ECS 操作時に使用 |
| `ecs_task_definition_family` | マイグレーション用 one-off タスクの起動に使用 |
| `ecs_log_group_name` | マイグレーションタスクのログ取得に使用 |
| `frontend_bucket_name` | フロントエンドのデプロイ先 |
| `github_actions_role_arn` | GitHub Actions が assume するロール (CD 設定に使用) |

## デプロイ手順

### バックエンド (ECS)

```bash
# 1. Docker イメージをビルド
# --platform は必須。ECS タスク定義は runtime_platform 未指定 = X86_64 のため、
# Apple Silicon などの arm64 マシンで指定を省くと ECS が
# CannotPullContainerError (image does not match the expected platform) で起動できない。
cd backend
docker build --platform linux/amd64 -t kakeibase-backend .

# 2. ECR にログイン & プッシュ
ECR_URL=$(terraform -chdir=../infra/envs/prod output -raw ecr_repository_url)
aws ecr get-login-password --region ap-northeast-1 | \
  docker login --username AWS --password-stdin "$ECR_URL"
docker tag kakeibase-backend:latest "$ECR_URL:latest"
docker push "$ECR_URL:latest"

# 3. ECS サービスを更新 (新イメージでタスクを再起動)
CLUSTER=$(terraform -chdir=../infra/envs/prod output -raw ecs_cluster_name)
SERVICE=$(terraform -chdir=../infra/envs/prod output -raw ecs_service_name)
aws ecs update-service --cluster "$CLUSTER" --service "$SERVICE" --force-new-deployment
```

### フロントエンド (S3 + CloudFront)

```bash
# 1. ビルド
cd frontend
npm ci
npm run build

# 2. S3 にアップロード
BUCKET=$(terraform -chdir=../infra/envs/prod output -raw frontend_bucket_name)
aws s3 sync dist/ "s3://$BUCKET" --delete

# 3. CloudFront キャッシュを無効化
CF_ID=$(terraform -chdir=../infra/envs/prod output -raw cloudfront_distribution_id)
aws cloudfront create-invalidation --distribution-id "$CF_ID" --paths "/*"
```

### Lambda (Go)

```bash
# 例: subscription-scheduler
cd lambda/subscription-scheduler
GOOS=linux GOARCH=arm64 go build -o bootstrap main.go
zip function.zip bootstrap

FUNC_NAME=$(terraform -chdir=../../infra/envs/prod output -raw lambda_subscription_scheduler)
aws lambda update-function-code \
  --function-name "$FUNC_NAME" \
  --zip-file fileb://function.zip
```

同様に `receipt-ocr`、`monthly-report` もビルド & デプロイする。

## ECS Exec でコンテナに接続

踏み台 EC2 なしでコンテナ内のシェルを取得できる:

```bash
CLUSTER=$(terraform -chdir=infra/envs/prod output -raw ecs_cluster_name)
TASK_ID=$(aws ecs list-tasks --cluster "$CLUSTER" --query 'taskArns[0]' --output text | awk -F/ '{print $NF}')
aws ecs execute-command \
  --cluster "$CLUSTER" \
  --task "$TASK_ID" \
  --container api \
  --interactive \
  --command "/bin/sh"
```

## CI/CD (GitHub Actions OIDC)

`cicd` モジュールが GitHub OIDC プロバイダと、GitHub Actions が assume するデプロイ用ロールを作成する。
長期のアクセスキーは発行しない ([ADR-0015](../docs/adr/0015-github-actions-oidc-federation.md))。

```bash
cd infra/envs/prod
terraform output -raw github_actions_role_arn   # GitHub Secrets の AWS_DEPLOY_ROLE_ARN に登録する
```

ロールを assume できるのは **`main` ブランチのワークフローのみ**。フォークの PR からは assume できない。
リポジトリ名を変えた場合やフォーク先で使う場合は `github_oidc_subjects` を更新する。

```hcl
# terraform.tfvars
github_oidc_subjects = ["repo:<owner>/<repo>:ref:refs/heads/main"]

# 同一 URL の OIDC プロバイダが既にアカウントにある場合 (アカウントごとに1つしか作れない)
create_github_oidc_provider = false
```

このロールに **`terraform apply` の権限は無い**。インフラの変更は上記の「Terraform 初期化 & デプロイ」の手順で手元から適用する。

## インフラの削除

```bash
cd infra/envs/prod
terraform destroy    # 確認プロンプトで yes
```

> **注意:** RDS は `skip_final_snapshot = true` のため、destroy 時にスナップショットを作成しない。

## 関連ドキュメント

- [ADR-0007: ECS + Lambda ハイブリッド構成](../docs/adr/0007-ecs-lambda-hybrid-no-rds-proxy.md)
- [ADR-0008: プライベートネットワーク + NAT Gateway](../docs/adr/0008-private-network-nat-gateway.md)
- [ADR-0009: Lambda ランタイムを Go に変更](../docs/adr/0009-lambda-runtime-go.md)
- [ADR-0014: ALB へのアクセスを CloudFront 経由に限定](../docs/adr/0014-cloudfront-only-alb-access.md)
- [ADR-0015: GitHub Actions から AWS への認証に OIDC を採用](../docs/adr/0015-github-actions-oidc-federation.md)
- [AWS 構成図 (HTML)](../docs/aws-architecture.html)
