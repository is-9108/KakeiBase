# ------- Receipt S3 Bucket -------

resource "aws_s3_bucket" "receipts" {
  bucket = var.bucket_name

  tags = {
    Name    = var.bucket_name
    Project = var.project
    Env     = var.env
  }
}

resource "aws_s3_bucket_public_access_block" "receipts" {
  bucket = aws_s3_bucket.receipts.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "receipts" {
  bucket = aws_s3_bucket.receipts.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# 365日後に自動削除 (ADR-0009)
resource "aws_s3_bucket_lifecycle_configuration" "receipts" {
  bucket = aws_s3_bucket.receipts.id

  rule {
    id     = "expire-receipts"
    status = "Enabled"

    filter {} # 全オブジェクトに適用

    expiration {
      days = 365
    }
  }
}

# ブラウザから Presigned URL 経由で直接 PUT できるよう CORS を設定
# AllowedOrigins は CloudFront ドメイン確定後に制限することを推奨
resource "aws_s3_bucket_cors_configuration" "receipts" {
  bucket = aws_s3_bucket.receipts.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "PUT"]
    allowed_origins = ["*"]
    expose_headers  = ["ETag"]
    max_age_seconds = 3600
  }
}
