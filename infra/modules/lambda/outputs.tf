output "subscription_scheduler_function_name" {
  description = "Subscription scheduler Lambda function name"
  value       = aws_lambda_function.subscription_scheduler.function_name
}

output "receipt_ocr_function_name" {
  description = "Receipt OCR Lambda function name"
  value       = aws_lambda_function.receipt_ocr.function_name
}

output "monthly_report_function_name" {
  description = "Monthly report Lambda function name"
  value       = aws_lambda_function.monthly_report.function_name
}
