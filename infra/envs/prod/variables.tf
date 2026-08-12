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
  description = "CIDR blocks allowed to access ALB (e.g. home IP)"
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
