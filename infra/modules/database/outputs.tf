output "db_secret_arn" {
  description = "Secrets Manager secret ARN for DB credentials"
  # secret ではなく secret_version の arn を返す (値は同じ secret の ARN)。
  # secret を参照すると「箱はあるが値が無い」状態でも下流の作成が始まり、
  # ECS タスクが ResourceInitializationError (staging label AWSCURRENT が無い) で
  # 起動に失敗する。version を経由させて値の投入完了を待たせる。
  value = aws_secretsmanager_secret_version.db.arn
}

output "db_endpoint" {
  description = "RDS instance endpoint"
  value       = aws_db_instance.this.address
}
