output "vpc_id" {
  description = "VPC ID"
  value       = module.network.vpc_id
}

output "public_subnet_ids" {
  description = "Public subnet IDs (ALB / NAT GW)"
  value       = module.network.public_subnet_ids
}

output "private_app_subnet_ids" {
  description = "Private app subnet IDs (ECS / Lambda)"
  value       = module.network.private_app_subnet_ids
}

output "private_db_subnet_ids" {
  description = "Private DB subnet IDs (RDS)"
  value       = module.network.private_db_subnet_ids
}

output "alb_sg_id" {
  description = "ALB security group ID"
  value       = module.security.alb_sg_id
}

output "ecs_sg_id" {
  description = "ECS security group ID"
  value       = module.security.ecs_sg_id
}

output "lambda_sg_id" {
  description = "Lambda security group ID"
  value       = module.security.lambda_sg_id
}

output "rds_sg_id" {
  description = "RDS security group ID"
  value       = module.security.rds_sg_id
}

output "ecr_repository_url" {
  description = "ECR repository URL"
  value       = module.ecr.repository_url
}

output "db_endpoint" {
  description = "RDS instance endpoint"
  value       = module.database.db_endpoint
}

output "alb_dns_name" {
  description = "ALB DNS name (API access URL)"
  value       = module.ecs.alb_dns_name
}

output "ecs_cluster_name" {
  description = "ECS cluster name"
  value       = module.ecs.ecs_cluster_name
}

output "ecs_service_name" {
  description = "ECS service name"
  value       = module.ecs.ecs_service_name
}

output "ecs_task_definition_family" {
  description = "ECS task definition family (used by the CD one-off migration task)"
  value       = module.ecs.ecs_task_definition_family
}

output "ecs_log_group_name" {
  description = "CloudWatch log group name for the API container"
  value       = module.ecs.log_group_name
}

output "receipt_bucket_name" {
  description = "Receipt S3 bucket name"
  value       = module.storage.bucket_name
}

output "cloudfront_domain" {
  description = "CloudFront distribution domain (app access URL)"
  value       = module.frontend.cloudfront_domain
}

output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID (for cache invalidation)"
  value       = module.frontend.cloudfront_distribution_id
}

output "frontend_bucket_name" {
  description = "Frontend S3 bucket name"
  value       = module.frontend.frontend_bucket_name
}

output "lambda_subscription_scheduler" {
  description = "Subscription scheduler Lambda function name"
  value       = module.lambda.subscription_scheduler_function_name
}

output "lambda_receipt_ocr" {
  description = "Receipt OCR Lambda function name"
  value       = module.lambda.receipt_ocr_function_name
}

output "lambda_monthly_report" {
  description = "Monthly report Lambda function name"
  value       = module.lambda.monthly_report_function_name
}

output "waf_web_acl_arn" {
  description = "WAF WebACL ARN associated with the CloudFront distribution"
  value       = module.waf.web_acl_arn
}

output "waf_ip_set_id" {
  description = "WAF IPSet ID (used when the allowed IP changes)"
  value       = module.waf.ip_set_id
}

output "github_actions_role_arn" {
  description = "IAM role ARN for GitHub Actions (register as the AWS_DEPLOY_ROLE_ARN secret)"
  value       = module.cicd.github_actions_role_arn
}
