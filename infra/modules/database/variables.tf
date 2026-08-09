variable "project" {
  description = "Project name"
  type        = string
}

variable "env" {
  description = "Environment name"
  type        = string
}

variable "private_db_subnet_ids" {
  description = "Private DB subnet IDs for RDS subnet group"
  type        = list(string)
}

variable "rds_sg_id" {
  description = "RDS security group ID"
  type        = string
}

variable "db_name" {
  description = "Database name"
  type        = string
  default     = "kakeibase"
}

variable "db_username" {
  description = "Database master username"
  type        = string
  default     = "kakeibase"
}

variable "instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.micro"
}
