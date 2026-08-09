variable "project" {
  description = "Project name"
  type        = string
}

variable "env" {
  description = "Environment name"
  type        = string
}

variable "vpc_cidr" {
  description = "VPC CIDR block"
  type        = string
}

variable "public_subnet_cidrs" {
  description = "Public subnet CIDR blocks"
  type        = list(string)
}

variable "private_app_subnet_cidrs" {
  description = "Private app subnet CIDR blocks (ECS / Lambda)"
  type        = list(string)
}

variable "private_db_subnet_cidrs" {
  description = "Private DB subnet CIDR blocks (RDS)"
  type        = list(string)
}

variable "availability_zones" {
  description = "Availability zones (must match subnet list lengths)"
  type        = list(string)
}
