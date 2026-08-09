output "bucket_name" {
  description = "Receipt S3 bucket name"
  value       = aws_s3_bucket.receipts.bucket
}

output "bucket_arn" {
  description = "Receipt S3 bucket ARN"
  value       = aws_s3_bucket.receipts.arn
}
