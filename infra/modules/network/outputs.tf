output "vpc_id" {
  description = "VPC ID"
  value       = aws_vpc.this.id
}

output "public_subnet_ids" {
  description = "Public subnet IDs"
  value       = aws_subnet.public[*].id
}

output "private_app_subnet_ids" {
  description = "Private app subnet IDs (ECS / Lambda)"
  value       = aws_subnet.private_app[*].id
}

output "private_db_subnet_ids" {
  description = "Private DB subnet IDs (RDS)"
  value       = aws_subnet.private_db[*].id
}
