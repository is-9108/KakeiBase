variable "project" {
  description = "Project name"
  type        = string
}

variable "env" {
  description = "Environment name"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "allowed_cidr" {
  description = "CIDR block allowed to access ALB (e.g. home IP: x.x.x.x/32)"
  type        = string
}
