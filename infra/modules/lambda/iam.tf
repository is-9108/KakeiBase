# 全 Lambda 共通の assume role policy
data "aws_iam_policy_document" "lambda_assume" {
  statement {
    effect = "Allow"
    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
    actions = ["sts:AssumeRole"]
  }
}

# ============================================================
# 1. Subscription Scheduler Role
#    - CloudWatch Logs, VPC, Secrets Manager
# ============================================================

resource "aws_iam_role" "subscription_scheduler" {
  name               = "${var.project}-${var.env}-lambda-subscription-scheduler"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume.json

  tags = {
    Project = var.project
    Env     = var.env
  }
}

resource "aws_iam_role_policy_attachment" "subscription_scheduler_basic" {
  role       = aws_iam_role.subscription_scheduler.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy_attachment" "subscription_scheduler_vpc" {
  role       = aws_iam_role.subscription_scheduler.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

resource "aws_iam_role_policy" "subscription_scheduler_secrets" {
  name = "secrets-access"
  role = aws_iam_role.subscription_scheduler.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["secretsmanager:GetSecretValue"]
      Resource = [var.db_secret_arn]
    }]
  })
}

# ============================================================
# 2. Receipt OCR Role
#    - CloudWatch Logs, VPC, Secrets Manager, S3 (read), Textract
# ============================================================

resource "aws_iam_role" "receipt_ocr" {
  name               = "${var.project}-${var.env}-lambda-receipt-ocr"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume.json

  tags = {
    Project = var.project
    Env     = var.env
  }
}

resource "aws_iam_role_policy_attachment" "receipt_ocr_basic" {
  role       = aws_iam_role.receipt_ocr.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy_attachment" "receipt_ocr_vpc" {
  role       = aws_iam_role.receipt_ocr.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

resource "aws_iam_role_policy" "receipt_ocr_app" {
  name = "app-access"
  role = aws_iam_role.receipt_ocr.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "SecretsManager"
        Effect   = "Allow"
        Action   = ["secretsmanager:GetSecretValue"]
        Resource = [var.db_secret_arn]
      },
      {
        Sid      = "S3ReadReceipts"
        Effect   = "Allow"
        Action   = ["s3:GetObject"]
        Resource = "${var.receipt_bucket_arn}/*"
      },
      {
        Sid      = "TextractOCR"
        Effect   = "Allow"
        Action   = ["textract:AnalyzeExpense"]
        Resource = "*"
      }
    ]
  })
}

# ============================================================
# 3. Monthly Report Role
#    - CloudWatch Logs, VPC, Secrets Manager, SES
# ============================================================

resource "aws_iam_role" "monthly_report" {
  name               = "${var.project}-${var.env}-lambda-monthly-report"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume.json

  tags = {
    Project = var.project
    Env     = var.env
  }
}

resource "aws_iam_role_policy_attachment" "monthly_report_basic" {
  role       = aws_iam_role.monthly_report.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy_attachment" "monthly_report_vpc" {
  role       = aws_iam_role.monthly_report.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

resource "aws_iam_role_policy" "monthly_report_app" {
  name = "app-access"
  role = aws_iam_role.monthly_report.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "SecretsManager"
        Effect   = "Allow"
        Action   = ["secretsmanager:GetSecretValue"]
        Resource = [var.db_secret_arn]
      },
      {
        Sid      = "SESSendEmail"
        Effect   = "Allow"
        Action   = ["ses:SendEmail", "ses:SendRawEmail"]
        Resource = "*"
      }
    ]
  })
}
