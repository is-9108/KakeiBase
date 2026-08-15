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

module "security" {
  source       = "../../modules/security"
  project      = var.project
  env          = var.env
  vpc_id       = module.network.vpc_id
  allowed_cidr = var.allowed_cidr
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
}

module "storage" {
  source      = "../../modules/storage"
  project     = var.project
  env         = var.env
  bucket_name = var.receipt_bucket_name
}

module "frontend" {
  source       = "../../modules/frontend"
  project      = var.project
  env          = var.env
  bucket_name  = var.frontend_bucket_name
  alb_dns_name = module.ecs.alb_dns_name
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
