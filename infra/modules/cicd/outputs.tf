output "github_actions_role_arn" {
  description = "IAM role ARN assumed by GitHub Actions (set as the AWS_DEPLOY_ROLE_ARN secret)"
  value       = aws_iam_role.github_actions.arn
}

output "oidc_provider_arn" {
  description = "GitHub OIDC provider ARN in use (created or existing)"
  value       = local.oidc_provider_arn
}
