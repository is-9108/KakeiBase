variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "ap-northeast-1"
}

variable "project" {
  description = "Project name used for resource naming"
  type        = string
  default     = "kakeibase"
}

variable "env" {
  description = "Environment name"
  type        = string
  default     = "prod"
}

variable "vpc_cidr" {
  description = "VPC CIDR block"
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidrs" {
  description = "Public subnet CIDR blocks (ALB / NAT GW)"
  type        = list(string)
  default     = ["10.0.0.0/24", "10.0.1.0/24"]
}

variable "private_app_subnet_cidrs" {
  description = "Private app subnet CIDR blocks (ECS / Lambda)"
  type        = list(string)
  default     = ["10.0.10.0/24", "10.0.11.0/24"]
}

variable "private_db_subnet_cidrs" {
  description = "Private DB subnet CIDR blocks (RDS)"
  type        = list(string)
  default     = ["10.0.20.0/24", "10.0.21.0/24"]
}

variable "availability_zones" {
  description = "Availability zones (must match subnet count)"
  type        = list(string)
  default     = ["ap-northeast-1a", "ap-northeast-1c"]
}

variable "allowed_cidr" {
  # ADR-0014 で ALB の SG から CloudFront の WAF IPSet へ移した
  description = "CIDR blocks allowed to access the application via the CloudFront WAF (e.g. home IP)"
  type        = list(string)
}

variable "receipt_bucket_name" {
  description = "S3 bucket name for receipt images (must be globally unique)"
  type        = string
  default     = "kakeibase-receipts-prod"
}

variable "frontend_bucket_name" {
  description = "S3 bucket name for frontend static files (must be globally unique)"
  type        = string
  default     = "kakeibase-frontend-prod"
}

variable "ses_sender_email" {
  description = "SES sender email address for monthly reports (empty to skip)"
  type        = string
  default     = ""
}

variable "github_oidc_subjects" {
  # public リポジトリのため ':*' は使わない。フォークの PR から assume されるのを防ぐ
  description = "Allowed GitHub OIDC sub claims. Restrict to the main branch; do not use ':*' on a public repo"
  type        = list(string)
  default     = ["repo:is-9108/KakeiBase:ref:refs/heads/main"]

  validation {
    # 信頼ポリシーは sub を StringLike で評価するため、'repo:owner/repo:*' を
    # 入れると全ブランチ・フォークの PR からも assume できてしまう。
    # 誤って広げられないよう変数側で弾く
    condition     = alltrue([for s in var.github_oidc_subjects : !strcontains(s, ":*")])
    error_message = "github_oidc_subjects must not contain ':*' (it would allow any ref, including fork PRs)."
  }
}

variable "create_github_oidc_provider" {
  # OIDC プロバイダはアカウントに 1 つしか作れない。既存がある場合は false にする
  description = "Whether to create the GitHub OIDC provider (false to reference an existing one)"
  type        = bool
  default     = true
}
