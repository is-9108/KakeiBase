variable "project" {
  description = "Project name"
  type        = string
}

variable "env" {
  description = "Environment name"
  type        = string
}

variable "bucket_name" {
  description = "S3 bucket name for frontend static files (must be globally unique)"
  type        = string
}

variable "alb_dns_name" {
  description = "ALB DNS name used as CloudFront API origin"
  type        = string
}
