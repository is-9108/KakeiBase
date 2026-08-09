# CI/CD で実コードをデプロイするまでのプレースホルダー
# lifecycle.ignore_changes でデプロイ後のコード差分を無視する
data "archive_file" "placeholder" {
  type        = "zip"
  output_path = "${path.module}/placeholder.zip"

  source {
    content  = "#!/bin/sh\nexit 0"
    filename = "bootstrap"
  }
}

# ============================================================
# CloudWatch Log Groups
# ============================================================

resource "aws_cloudwatch_log_group" "subscription_scheduler" {
  name              = "/aws/lambda/${var.project}-${var.env}-subscription-scheduler"
  retention_in_days = 30

  tags = {
    Project = var.project
    Env     = var.env
  }
}

resource "aws_cloudwatch_log_group" "receipt_ocr" {
  name              = "/aws/lambda/${var.project}-${var.env}-receipt-ocr"
  retention_in_days = 30

  tags = {
    Project = var.project
    Env     = var.env
  }
}

resource "aws_cloudwatch_log_group" "monthly_report" {
  name              = "/aws/lambda/${var.project}-${var.env}-monthly-report"
  retention_in_days = 30

  tags = {
    Project = var.project
    Env     = var.env
  }
}

# ============================================================
# Lambda Functions (Go / ARM64 / provided.al2023)  ADR-0009
# ============================================================

# --- 1. Subscription Scheduler (ADR-0003) ---
# 毎月1日に全アクティブサブスクから transactions を自動生成

resource "aws_lambda_function" "subscription_scheduler" {
  function_name    = "${var.project}-${var.env}-subscription-scheduler"
  role             = aws_iam_role.subscription_scheduler.arn
  handler          = "bootstrap"
  runtime          = "provided.al2023"
  architectures    = ["arm64"]
  filename         = data.archive_file.placeholder.output_path
  source_code_hash = data.archive_file.placeholder.output_base64sha256
  timeout          = 60
  memory_size      = 128

  vpc_config {
    subnet_ids         = var.private_app_subnet_ids
    security_group_ids = [var.lambda_sg_id]
  }

  environment {
    variables = {
      DB_SECRET_ARN = var.db_secret_arn
    }
  }

  depends_on = [aws_cloudwatch_log_group.subscription_scheduler]

  lifecycle {
    ignore_changes = [filename, source_code_hash]
  }

  tags = {
    Name    = "${var.project}-${var.env}-subscription-scheduler"
    Project = var.project
    Env     = var.env
  }
}

# --- 2. Receipt OCR (ADR-0005) ---
# S3 レシート画像アップロード → Textract OCR → transactions INSERT

resource "aws_lambda_function" "receipt_ocr" {
  function_name    = "${var.project}-${var.env}-receipt-ocr"
  role             = aws_iam_role.receipt_ocr.arn
  handler          = "bootstrap"
  runtime          = "provided.al2023"
  architectures    = ["arm64"]
  filename         = data.archive_file.placeholder.output_path
  source_code_hash = data.archive_file.placeholder.output_base64sha256
  timeout          = 120
  memory_size      = 256

  vpc_config {
    subnet_ids         = var.private_app_subnet_ids
    security_group_ids = [var.lambda_sg_id]
  }

  environment {
    variables = {
      DB_SECRET_ARN  = var.db_secret_arn
      RECEIPT_BUCKET = var.receipt_bucket_id
    }
  }

  depends_on = [aws_cloudwatch_log_group.receipt_ocr]

  lifecycle {
    ignore_changes = [filename, source_code_hash]
  }

  tags = {
    Name    = "${var.project}-${var.env}-receipt-ocr"
    Project = var.project
    Env     = var.env
  }
}

# --- 3. Monthly Report (ADR-0006) ---
# 前月の収支サマリを集計し HTML メールで SES 送信

resource "aws_lambda_function" "monthly_report" {
  function_name    = "${var.project}-${var.env}-monthly-report"
  role             = aws_iam_role.monthly_report.arn
  handler          = "bootstrap"
  runtime          = "provided.al2023"
  architectures    = ["arm64"]
  filename         = data.archive_file.placeholder.output_path
  source_code_hash = data.archive_file.placeholder.output_base64sha256
  timeout          = 120
  memory_size      = 128

  vpc_config {
    subnet_ids         = var.private_app_subnet_ids
    security_group_ids = [var.lambda_sg_id]
  }

  environment {
    variables = {
      DB_SECRET_ARN = var.db_secret_arn
      SES_FROM      = var.ses_sender_email
    }
  }

  depends_on = [aws_cloudwatch_log_group.monthly_report]

  lifecycle {
    ignore_changes = [filename, source_code_hash]
  }

  tags = {
    Name    = "${var.project}-${var.env}-monthly-report"
    Project = var.project
    Env     = var.env
  }
}

# ============================================================
# EventBridge Rules
# ============================================================

# Subscription Scheduler: 毎月1日 00:00 UTC (= 09:00 JST)
resource "aws_cloudwatch_event_rule" "subscription_scheduler" {
  name                = "${var.project}-${var.env}-subscription-scheduler"
  schedule_expression = "cron(0 0 1 * ? *)"

  tags = {
    Project = var.project
    Env     = var.env
  }
}

resource "aws_cloudwatch_event_target" "subscription_scheduler" {
  rule = aws_cloudwatch_event_rule.subscription_scheduler.name
  arn  = aws_lambda_function.subscription_scheduler.arn
}

resource "aws_lambda_permission" "subscription_scheduler_eventbridge" {
  statement_id  = "AllowEventBridgeInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.subscription_scheduler.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.subscription_scheduler.arn
}

# Monthly Report: 毎月1日 01:00 UTC (= 10:00 JST)
resource "aws_cloudwatch_event_rule" "monthly_report" {
  name                = "${var.project}-${var.env}-monthly-report"
  schedule_expression = "cron(0 1 1 * ? *)"

  tags = {
    Project = var.project
    Env     = var.env
  }
}

resource "aws_cloudwatch_event_target" "monthly_report" {
  rule = aws_cloudwatch_event_rule.monthly_report.name
  arn  = aws_lambda_function.monthly_report.arn
}

resource "aws_lambda_permission" "monthly_report_eventbridge" {
  statement_id  = "AllowEventBridgeInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.monthly_report.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.monthly_report.arn
}

# ============================================================
# S3 Event Notification → Receipt OCR Lambda
# ============================================================

resource "aws_lambda_permission" "receipt_ocr_s3" {
  statement_id  = "AllowS3Invoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.receipt_ocr.function_name
  principal     = "s3.amazonaws.com"
  source_arn    = var.receipt_bucket_arn
}

resource "aws_s3_bucket_notification" "receipt_ocr" {
  bucket = var.receipt_bucket_id

  lambda_function {
    lambda_function_arn = aws_lambda_function.receipt_ocr.arn
    events              = ["s3:ObjectCreated:Put"]
  }

  depends_on = [aws_lambda_permission.receipt_ocr_s3]
}

# ============================================================
# SES (送信元メールアドレスの検証)
# ============================================================

resource "aws_ses_email_identity" "sender" {
  count = var.ses_sender_email != "" ? 1 : 0
  email = var.ses_sender_email
}
