variable "project" {
  description = "Project name"
  type        = string
}

variable "env" {
  description = "Environment name"
  type        = string
}

variable "bucket_name" {
  description = "S3 bucket name for receipt images (must be globally unique)"
  type        = string
}
