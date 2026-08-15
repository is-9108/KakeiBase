output "alb_dns_name" {
  description = "ALB DNS name (access URL)"
  value       = aws_lb.this.dns_name
}

output "ecs_cluster_name" {
  description = "ECS cluster name"
  value       = aws_ecs_cluster.this.name
}

output "ecs_service_name" {
  description = "ECS service name"
  value       = aws_ecs_service.api.name
}

output "ecs_cluster_arn" {
  description = "ECS cluster ARN"
  value       = aws_ecs_cluster.this.arn
}

output "ecs_service_arn" {
  description = "ECS service ARN"
  # aws_ecs_service の id は ARN (AWS provider v5 の仕様)
  value = aws_ecs_service.api.id
}

output "ecs_task_definition_family" {
  description = "ECS task definition family (used by the CD one-off migration task)"
  value       = aws_ecs_task_definition.api.family
}

output "ecs_task_role_arn" {
  description = "ECS task role ARN (for adding S3 permissions in Phase 3)"
  value       = aws_iam_role.ecs_task.arn
}

output "ecs_task_execution_role_arn" {
  description = "ECS task execution role ARN"
  value       = aws_iam_role.ecs_task_execution.arn
}

output "log_group_name" {
  description = "CloudWatch log group name for the API container"
  value       = aws_cloudwatch_log_group.api.name
}

output "log_group_arn" {
  description = "CloudWatch log group ARN for the API container"
  value       = aws_cloudwatch_log_group.api.arn
}
