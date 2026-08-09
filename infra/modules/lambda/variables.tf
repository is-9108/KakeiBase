variable "project" {
  description = "Project name"
  type        = string
}

variable "env" {
  description = "Environment name"
  type        = string
}

variable "private_app_subnet_ids" {
  description = "Private app subnet IDs for Lambda VPC config"
  type        = list(string)
}

variable "lambda_sg_id" {
  description = "Lambda security group ID"
  type        = string
}

variable "db_secret_arn" {
  description = "Secrets Manager secret ARN for DB credentials"
  type        = string
}

variable "receipt_bucket_id" {
  description = "Receipt S3 bucket ID (for S3 event notification)"
  type        = string
}

variable "receipt_bucket_arn" {
  description = "Receipt S3 bucket ARN (for IAM permissions)"
  type        = string
}

variable "ses_sender_email" {
  description = "SES sender email address (empty to skip SES identity creation)"
  type        = string
  default     = ""
}
