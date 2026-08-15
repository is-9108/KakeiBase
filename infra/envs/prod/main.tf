terraform {
  # backend の use_lockfile (S3 ネイティブロック) は Terraform 1.10 以降で使える
  required_version = ">= 1.10"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
    archive = {
      source  = "hashicorp/archive"
      version = "~> 2.0"
    }
  }
  backend "s3" {
    bucket = "kakeibase-terraform-state"
    key    = "prod/terraform.tfstate"
    region = "ap-northeast-1"
    # state には RDS エンドポイントや Secrets Manager の ARN が含まれるため暗号化する
    encrypt = true
    # S3 ネイティブロック。DynamoDB テーブルを別途管理せずに排他制御できる
    use_lockfile = true
  }
}

provider "aws" {
  region = var.aws_region
}

# WAF の scope = CLOUDFRONT は us-east-1 でのみ作成できる
provider "aws" {
  alias  = "us_east_1"
  region = "us-east-1"
}

# CloudFront -> ALB のオリジン検証に使う共有シークレット。
# CloudFront がカスタムオリジンヘッダーとして付与し、ALB のリスナールールが
# 一致を検証する。不一致 (= CloudFront を経由しない直接アクセス) は ALB が 403 を返す。
# ecs と frontend の 2 モジュールで共有する値のため、ルートで生成して両方に渡す。
resource "random_password" "origin_verify" {
  length  = 48
  special = false # HTTP ヘッダー値として扱うため英数字のみに限定する
}

module "waf" {
  source = "../../modules/waf"
  providers = {
    aws.us_east_1 = aws.us_east_1
  }
  project      = var.project
  env          = var.env
  allowed_cidr = var.allowed_cidr
}

module "network" {
  source                   = "../../modules/network"
  project                  = var.project
  env                      = var.env
  vpc_cidr                 = var.vpc_cidr
  public_subnet_cidrs      = var.public_subnet_cidrs
  private_app_subnet_cidrs = var.private_app_subnet_cidrs
  private_db_subnet_cidrs  = var.private_db_subnet_cidrs
  availability_zones       = var.availability_zones
}

# allowed_cidr は security ではなく waf モジュールへ渡す (ADR-0014)。
# ALB の SG は CloudFront プレフィックスリストのみを許可する形に変わった。
module "security" {
  source  = "../../modules/security"
  project = var.project
  env     = var.env
  vpc_id  = module.network.vpc_id
}

module "ecr" {
  source  = "../../modules/ecr"
  project = var.project
  env     = var.env
}

module "database" {
  source                = "../../modules/database"
  project               = var.project
  env                   = var.env
  private_db_subnet_ids = module.network.private_db_subnet_ids
  rds_sg_id             = module.security.rds_sg_id
}

module "ecs" {
  source                 = "../../modules/ecs"
  project                = var.project
  env                    = var.env
  vpc_id                 = module.network.vpc_id
  public_subnet_ids      = module.network.public_subnet_ids
  private_app_subnet_ids = module.network.private_app_subnet_ids
  alb_sg_id              = module.security.alb_sg_id
  ecs_sg_id              = module.security.ecs_sg_id
  container_image        = "${module.ecr.repository_url}:latest"
  db_secret_arn          = module.database.db_secret_arn
  receipt_bucket_arn     = module.storage.bucket_arn
  receipt_bucket_name    = module.storage.bucket_name
  origin_verify_secret   = random_password.origin_verify.result
}

module "storage" {
  source      = "../../modules/storage"
  project     = var.project
  env         = var.env
  bucket_name = var.receipt_bucket_name
}

module "frontend" {
  source               = "../../modules/frontend"
  project              = var.project
  env                  = var.env
  bucket_name          = var.frontend_bucket_name
  alb_dns_name         = module.ecs.alb_dns_name
  origin_verify_secret = random_password.origin_verify.result
  web_acl_arn          = module.waf.web_acl_arn
}

module "lambda" {
  source                 = "../../modules/lambda"
  project                = var.project
  env                    = var.env
  private_app_subnet_ids = module.network.private_app_subnet_ids
  lambda_sg_id           = module.security.lambda_sg_id
  db_secret_arn          = module.database.db_secret_arn
  receipt_bucket_id      = module.storage.bucket_name
  receipt_bucket_arn     = module.storage.bucket_arn
  ses_sender_email       = var.ses_sender_email
}

# GitHub Actions のデプロイ用ロール。デプロイ対象のリソースの ARN を受け取って
# 権限をそこに絞るため、他のモジュールの後ろに置く。
# terraform apply の権限は与えていない (IaC の適用は手元からの手動運用を維持する)。
module "cicd" {
  source                      = "../../modules/cicd"
  project                     = var.project
  env                         = var.env
  github_oidc_subjects        = var.github_oidc_subjects
  create_oidc_provider        = var.create_github_oidc_provider
  ecr_repository_arn          = module.ecr.repository_arn
  ecs_cluster_arn             = module.ecs.ecs_cluster_arn
  ecs_cluster_name            = module.ecs.ecs_cluster_name
  ecs_service_arn             = module.ecs.ecs_service_arn
  ecs_task_definition_family  = module.ecs.ecs_task_definition_family
  ecs_task_role_arn           = module.ecs.ecs_task_role_arn
  ecs_task_execution_role_arn = module.ecs.ecs_task_execution_role_arn
  ecs_log_group_arn           = module.ecs.log_group_arn
  frontend_bucket_arn         = module.frontend.frontend_bucket_arn
  cloudfront_distribution_arn = module.frontend.cloudfront_distribution_arn
  lambda_function_arns        = module.lambda.function_arns
}
