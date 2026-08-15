variable "project" {
  description = "Project name"
  type        = string
}

variable "env" {
  description = "Environment name"
  type        = string
}

variable "allowed_cidr" {
  description = "CIDR blocks allowed to access the application via CloudFront (e.g. home IP)"
  type        = list(string)
}
