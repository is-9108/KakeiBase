variable "project" {
  description = "Project name"
  type        = string
}

variable "env" {
  description = "Environment name"
  type        = string
}

variable "github_oidc_subjects" {
  # public リポジトリのため ':*' は使わない。フォークの PR から assume されるのを防ぐ
  description = "Allowed GitHub OIDC sub claims (restrict to specific refs; do not use ':*' on a public repo)"
  type        = list(string)
}

variable "create_oidc_provider" {
  # OIDC プロバイダは AWS アカウントに URL ごと 1 つしか作れないため、
  # 既に存在するアカウントでは false にして既存を参照する
  description = "Whether to create the GitHub OIDC provider (false to reference an existing one)"
  type        = bool
  default     = true
}

variable "ecr_repository_arn" {
  description = "ECR repository ARN the workflow pushes images to"
  type        = string
}

variable "ecs_cluster_arn" {
  description = "ECS cluster ARN (used as the RunTask condition)"
  type        = string
}

variable "ecs_cluster_name" {
  description = "ECS cluster name (used to build the task ARN pattern)"
  type        = string
}

variable "ecs_service_arn" {
  description = "ECS service ARN the workflow updates"
  type        = string
}

variable "ecs_task_definition_family" {
  description = "ECS task definition family used for the migration one-off task"
  type        = string
}

variable "ecs_task_role_arn" {
  description = "ECS task role ARN (needs iam:PassRole for RunTask)"
  type        = string
}

variable "ecs_task_execution_role_arn" {
  description = "ECS task execution role ARN (needs iam:PassRole for RunTask)"
  type        = string
}

variable "ecs_log_group_arn" {
  description = "CloudWatch log group ARN for reading migration task logs"
  type        = string
}

variable "frontend_bucket_arn" {
  description = "Frontend S3 bucket ARN the workflow syncs to"
  type        = string
}

variable "cloudfront_distribution_arn" {
  description = "CloudFront distribution ARN the workflow invalidates"
  type        = string
}

variable "lambda_function_arns" {
  description = "Lambda function ARNs the workflow updates"
  type        = list(string)
}
