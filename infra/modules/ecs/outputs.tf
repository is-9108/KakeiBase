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

output "ecs_task_role_arn" {
  description = "ECS task role ARN (for adding S3 permissions in Phase 3)"
  value       = aws_iam_role.ecs_task.arn
}
