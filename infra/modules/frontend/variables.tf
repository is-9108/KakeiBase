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

variable "origin_verify_secret" {
  description = "Shared secret sent as a custom origin header and verified by the ALB listener rule"
  type        = string
  sensitive   = true
}

variable "origin_verify_header_name" {
  description = "Custom origin header name used to verify requests came through CloudFront"
  type        = string
  default     = "X-Origin-Verify"
}

variable "web_acl_arn" {
  description = "WAFv2 WebACL ARN (CLOUDFRONT scope) to associate with the distribution"
  type        = string
}
