# CloudFront スコープの WAF リソースは us-east-1 にのみ作成できるため、
# 呼び出し側から us_east_1 の provider alias を受け取る。
terraform {
  required_providers {
    aws = {
      source                = "hashicorp/aws"
      version               = "~> 5.0"
      configuration_aliases = [aws.us_east_1]
    }
  }
}

resource "aws_wafv2_ip_set" "allowed" {
  provider           = aws.us_east_1
  name               = "${var.project}-${var.env}-allowed-ips"
  description        = "IP addresses allowed to access the application (ADR-0008)"
  scope              = "CLOUDFRONT"
  ip_address_version = "IPV4"
  addresses          = var.allowed_cidr

  tags = {
    Name    = "${var.project}-${var.env}-allowed-ips"
    Project = var.project
    Env     = var.env
  }
}

# ALB の SG から自宅IP制限を外した代わりに、CloudFront のエッジで遮断する。
# ADR-0008 決定3 の「自宅IPのみ許可」という要件はここで維持している。
resource "aws_wafv2_web_acl" "this" {
  provider    = aws.us_east_1
  name        = "${var.project}-${var.env}-webacl"
  description = "Allow only listed IPs; block everything else at the edge"
  scope       = "CLOUDFRONT"

  default_action {
    block {}
  }

  rule {
    name     = "allow-listed-ips"
    priority = 1

    action {
      allow {}
    }

    statement {
      ip_set_reference_statement {
        arn = aws_wafv2_ip_set.allowed.arn
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project}-${var.env}-allow-listed-ips"
      sampled_requests_enabled   = true
    }
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "${var.project}-${var.env}-webacl"
    sampled_requests_enabled   = true
  }

  tags = {
    Name    = "${var.project}-${var.env}-webacl"
    Project = var.project
    Env     = var.env
  }
}
