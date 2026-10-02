variable "aws_region" {
  description = "AWS region used by future, explicitly authorised deployments."
  type        = string
  default     = "ap-southeast-2"
}

variable "project_name" {
  description = "Portfolio project name applied to future AWS resources."
  type        = string
  default     = "InsightVault"
}

variable "environment" {
  description = "Deployment environment name for future AWS resources."
  type        = string
  default     = "development"
}

variable "managed_by" {
  description = "Tag identifying the infrastructure tool."
  type        = string
  default     = "Terraform"
}
