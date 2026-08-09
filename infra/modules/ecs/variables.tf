variable "project" {
  description = "Project name"
  type        = string
}

variable "env" {
  description = "Environment name"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "public_subnet_ids" {
  description = "Public subnet IDs for ALB"
  type        = list(string)
}

variable "private_app_subnet_ids" {
  description = "Private app subnet IDs for ECS tasks"
  type        = list(string)
}

variable "alb_sg_id" {
  description = "ALB security group ID"
  type        = string
}

variable "ecs_sg_id" {
  description = "ECS security group ID"
  type        = string
}

variable "container_image" {
  description = "Container image URI (ECR URL:tag)"
  type        = string
}

variable "db_secret_arn" {
  description = "Secrets Manager secret ARN for DB credentials"
  type        = string
}

variable "app_port" {
  description = "Container port the API listens on"
  type        = number
  default     = 8080
}

variable "cpu" {
  description = "Fargate task CPU units"
  type        = number
  default     = 256
}

variable "memory" {
  description = "Fargate task memory (MiB)"
  type        = number
  default     = 512
}

variable "receipt_bucket_arn" {
  description = "Receipt S3 bucket ARN for task role permissions (set in Phase 3)"
  type        = string
  default     = ""
}
