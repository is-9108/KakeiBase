output "db_secret_arn" {
  description = "Secrets Manager secret ARN for DB credentials"
  value       = aws_secretsmanager_secret.db.arn
}

output "db_endpoint" {
  description = "RDS instance endpoint"
  value       = aws_db_instance.this.address
}
