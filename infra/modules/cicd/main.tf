data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

# ------- GitHub OIDC Provider -------
# GitHub Actions が長期のアクセスキーなしで AWS を操作するための信頼元。
# 同一 URL のプロバイダは AWS アカウントに 1 つしか作れないため、
# 既に存在するアカウントでは create_oidc_provider = false で既存を参照する。

resource "aws_iam_openid_connect_provider" "github" {
  count = var.create_oidc_provider ? 1 : 0

  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
  # AWS は 2023 年以降このサムプリントを検証に使っていない (信頼された CA で検証する) が、
  # API が値を要求するため GitHub の中間 CA のものを指定する
  thumbprint_list = ["6938fd4d98bab03faadb97b34396831e3780aea1"]

  tags = {
    Name    = "${var.project}-${var.env}-github-oidc"
    Project = var.project
    Env     = var.env
  }
}

data "aws_iam_openid_connect_provider" "github" {
  count = var.create_oidc_provider ? 0 : 1
  url   = "https://token.actions.githubusercontent.com"
}

locals {
  oidc_provider_arn = var.create_oidc_provider ? aws_iam_openid_connect_provider.github[0].arn : data.aws_iam_openid_connect_provider.github[0].arn

  account_id = data.aws_caller_identity.current.account_id
  region     = data.aws_region.current.name

  # RunTask 対象のタスク定義はリビジョンが増えるためワイルドカードで指定する
  task_definition_arn_pattern = "arn:aws:ecs:${local.region}:${local.account_id}:task-definition/${var.ecs_task_definition_family}:*"
  # RunTask で起動したタスクの ID は事前に分からないためワイルドカードで指定する
  task_arn_pattern = "arn:aws:ecs:${local.region}:${local.account_id}:task/${var.ecs_cluster_name}/*"
}

# ------- IAM: GitHub Actions Deploy Role -------

resource "aws_iam_role" "github_actions" {
  name        = "${var.project}-${var.env}-github-actions"
  description = "Assumed by GitHub Actions via OIDC to deploy the application"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Federated = local.oidc_provider_arn }
      Action    = "sts:AssumeRoleWithWebIdentity"
      Condition = {
        # aud を固定しないと他者のワークフローのトークンでも assume できてしまう
        StringEquals = {
          "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
        }
        # sub でリポジトリとブランチを限定する (github_oidc_subjects の説明を参照)
        StringLike = {
          "token.actions.githubusercontent.com:sub" = var.github_oidc_subjects
        }
      }
    }]
  })

  tags = {
    Name    = "${var.project}-${var.env}-github-actions"
    Project = var.project
    Env     = var.env
  }
}

# デプロイに必要な操作だけを許可する。
# terraform apply の権限は意図的に与えていない。IaC の変更には人間による plan 確認を挟む。
resource "aws_iam_role_policy" "deploy" {
  name = "${var.project}-${var.env}-github-actions-deploy"
  role = aws_iam_role.github_actions.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        # GetAuthorizationToken はリソースレベルの指定ができない
        Sid      = "EcrAuth"
        Effect   = "Allow"
        Action   = ["ecr:GetAuthorizationToken"]
        Resource = "*"
      },
      {
        Sid    = "EcrPush"
        Effect = "Allow"
        Action = [
          "ecr:BatchCheckLayerAvailability",
          "ecr:InitiateLayerUpload",
          "ecr:UploadLayerPart",
          "ecr:CompleteLayerUpload",
          "ecr:PutImage",
          # 差分プッシュ時に既存レイヤーを参照する
          "ecr:BatchGetImage",
          "ecr:GetDownloadUrlForLayer",
          "ecr:DescribeImages"
        ]
        Resource = var.ecr_repository_arn
      },
      {
        # UpdateService: --force-new-deployment / DescribeServices: wait services-stable
        Sid      = "EcsService"
        Effect   = "Allow"
        Action   = ["ecs:UpdateService", "ecs:DescribeServices"]
        Resource = var.ecs_service_arn
      },
      {
        # EF Core マイグレーションを one-off タスクとして実行する (ADR-0008 決定6)
        Sid      = "EcsRunTask"
        Effect   = "Allow"
        Action   = ["ecs:RunTask"]
        Resource = local.task_definition_arn_pattern
        Condition = {
          ArnEquals = {
            "ecs:cluster" = var.ecs_cluster_arn
          }
        }
      },
      {
        # wait tasks-stopped と終了コードの確認に使う
        Sid      = "EcsDescribeTasks"
        Effect   = "Allow"
        Action   = ["ecs:DescribeTasks"]
        Resource = local.task_arn_pattern
      },
      {
        # RunTask はタスク定義が参照する 2 つのロールを渡すため PassRole が必要
        Sid      = "PassEcsRoles"
        Effect   = "Allow"
        Action   = ["iam:PassRole"]
        Resource = [var.ecs_task_role_arn, var.ecs_task_execution_role_arn]
        Condition = {
          StringEquals = {
            "iam:PassedToService" = "ecs-tasks.amazonaws.com"
          }
        }
      },
      {
        # マイグレーションタスクの出力を CI のログに残す
        Sid      = "ReadEcsLogs"
        Effect   = "Allow"
        Action   = ["logs:GetLogEvents", "logs:DescribeLogStreams"]
        Resource = "${var.ecs_log_group_arn}:*"
      },
      {
        # s3 sync --delete が既存オブジェクトを列挙する
        Sid      = "S3ListFrontend"
        Effect   = "Allow"
        Action   = ["s3:ListBucket"]
        Resource = var.frontend_bucket_arn
      },
      {
        Sid      = "S3WriteFrontend"
        Effect   = "Allow"
        Action   = ["s3:PutObject", "s3:GetObject", "s3:DeleteObject"]
        Resource = "${var.frontend_bucket_arn}/*"
      },
      {
        Sid      = "CloudFrontInvalidate"
        Effect   = "Allow"
        Action   = ["cloudfront:CreateInvalidation", "cloudfront:GetInvalidation"]
        Resource = var.cloudfront_distribution_arn
      },
      {
        # GetFunctionConfiguration は wait function-updated が使う
        Sid    = "LambdaDeploy"
        Effect = "Allow"
        Action = [
          "lambda:UpdateFunctionCode",
          "lambda:GetFunction",
          "lambda:GetFunctionConfiguration"
        ]
        Resource = var.lambda_function_arns
      }
    ]
  })
}
