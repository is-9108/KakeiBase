output "alb_sg_id" {
  description = "ALB security group ID"
  value       = aws_security_group.alb.id
}

output "ecs_sg_id" {
  description = "ECS security group ID"
  value       = aws_security_group.ecs.id
}

output "lambda_sg_id" {
  description = "Lambda security group ID"
  value       = aws_security_group.lambda.id
}

output "rds_sg_id" {
  description = "RDS security group ID"
  value       = aws_security_group.rds.id
}
