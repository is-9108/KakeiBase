output "web_acl_arn" {
  description = "WebACL ARN to associate with the CloudFront distribution"
  value       = aws_wafv2_web_acl.this.arn
}

output "ip_set_id" {
  description = "IPSet ID (used when the allowed IP changes)"
  value       = aws_wafv2_ip_set.allowed.id
}
