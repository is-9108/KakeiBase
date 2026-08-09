# ------- Frontend S3 Bucket -------

resource "aws_s3_bucket" "frontend" {
  bucket = var.bucket_name

  tags = {
    Name    = var.bucket_name
    Project = var.project
    Env     = var.env
  }
}

resource "aws_s3_bucket_public_access_block" "frontend" {
  bucket = aws_s3_bucket.frontend.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "frontend" {
  bucket = aws_s3_bucket.frontend.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# ------- CloudFront OAC -------

resource "aws_cloudfront_origin_access_control" "frontend" {
  name                              = "${var.project}-${var.env}-oac"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

# ------- S3 Bucket Policy (CloudFront OAC からのみ GetObject を許可) -------

resource "aws_s3_bucket_policy" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "cloudfront.amazonaws.com" }
      Action    = "s3:GetObject"
      Resource  = "${aws_s3_bucket.frontend.arn}/*"
      Condition = {
        StringEquals = {
          "AWS:SourceArn" = aws_cloudfront_distribution.this.arn
        }
      }
    }]
  })
}

# ------- CloudFront Managed Policies -------

data "aws_cloudfront_cache_policy" "caching_optimized" {
  name = "Managed-CachingOptimized"
}

data "aws_cloudfront_cache_policy" "caching_disabled" {
  name = "Managed-CachingDisabled"
}

data "aws_cloudfront_origin_request_policy" "all_viewer_except_host" {
  name = "Managed-AllViewerExceptHostHeader"
}

# ------- CloudFront Distribution -------

resource "aws_cloudfront_distribution" "this" {
  enabled             = true
  default_root_object = "index.html"
  comment             = "${var.project}-${var.env}"

  # Origin 1: S3 (フロントエンド静的ファイル)
  origin {
    domain_name              = aws_s3_bucket.frontend.bucket_regional_domain_name
    origin_id                = "S3-frontend"
    origin_access_control_id = aws_cloudfront_origin_access_control.frontend.id
  }

  # Origin 2: ALB (バックエンド API)
  origin {
    domain_name = var.alb_dns_name
    origin_id   = "ALB-api"

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "http-only" # ALB は HTTP(80) のみ
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  # デフォルト動作: S3 からフロントエンドを配信 (CachingOptimized + Gzip/Brotli)
  default_cache_behavior {
    allowed_methods        = ["GET", "HEAD"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "S3-frontend"
    viewer_protocol_policy = "redirect-to-https"
    compress               = true
    cache_policy_id        = data.aws_cloudfront_cache_policy.caching_optimized.id
  }

  # /api/* は ALB へ転送
  # CachingDisabled: API レスポンスをキャッシュしない
  # AllViewerExceptHostHeader: 全ヘッダー・Cookie・クエリ文字列をオリジンに転送
  #   - Cookie 転送が必須: JWT は HttpOnly Cookie で管理 (ADR-0004)
  #   - Host ヘッダーは除外: ALB が CloudFront のドメイン名を受け取らないようにする
  ordered_cache_behavior {
    path_pattern             = "/api/*"
    allowed_methods          = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods           = ["GET", "HEAD"]
    target_origin_id         = "ALB-api"
    viewer_protocol_policy   = "redirect-to-https"
    cache_policy_id          = data.aws_cloudfront_cache_policy.caching_disabled.id
    origin_request_policy_id = data.aws_cloudfront_origin_request_policy.all_viewer_except_host.id
  }

  # SPA ルーティング: S3 の 403/404 を index.html にリダイレクト (ADR-0011)
  custom_error_response {
    error_code         = 403
    response_code      = 200
    response_page_path = "/index.html"
  }

  custom_error_response {
    error_code         = 404
    response_code      = 200
    response_page_path = "/index.html"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  # ドメイン未確定のため CloudFront デフォルト証明書を使用
  # 独自ドメイン取得後は aliases + acm_certificate_arn に切り替える
  viewer_certificate {
    cloudfront_default_certificate = true
  }

  tags = {
    Name    = "${var.project}-${var.env}-cf"
    Project = var.project
    Env     = var.env
  }
}
