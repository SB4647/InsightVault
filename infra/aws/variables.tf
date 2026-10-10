variable "aws_region" {
  description = "AWS region used by explicitly authorised deployments."
  type        = string
  default     = "ap-southeast-2"
}

variable "project_name" {
  description = "Portfolio project name applied to AWS resources."
  type        = string
  default     = "InsightVault"
}

variable "environment" {
  description = "Deployment environment name applied to AWS resources."
  type        = string
  default     = "development"
}

variable "managed_by" {
  description = "Tag identifying the infrastructure tool."
  type        = string
  default     = "Terraform"
}

variable "documents_bucket_name" {
  description = "Globally unique S3 bucket name for private InsightVault documents when deployment is explicitly approved."
  type        = string
}

variable "enable_document_processing_queues" {
  description = "Creates the document processing SQS queue and DLQ only when a deployment is explicitly approved."
  type        = bool
  default     = false
}

variable "enable_workload_iam_roles" {
  description = "Creates future ECS task roles only when document processing queues are explicitly enabled for an approved deployment."
  type        = bool
  default     = false
}

variable "api_secret_arns" {
  description = "Existing Secrets Manager secret ARNs the deployed API may read. This module never creates secrets."
  type        = set(string)
  default     = []

  validation {
    condition = alltrue([
      for arn in var.api_secret_arns :
      !strcontains(arn, "*") && can(regex("^arn:aws[a-z-]*:secretsmanager:[^:]+:[0-9]{12}:secret:.+", arn))
    ])
    error_message = "api_secret_arns must contain exact Secrets Manager secret ARNs without wildcard characters."
  }
}

variable "worker_secret_arns" {
  description = "Existing Secrets Manager secret ARNs the deployed worker may read. This module never creates secrets."
  type        = set(string)
  default     = []

  validation {
    condition = alltrue([
      for arn in var.worker_secret_arns :
      !strcontains(arn, "*") && can(regex("^arn:aws[a-z-]*:secretsmanager:[^:]+:[0-9]{12}:secret:.+", arn))
    ])
    error_message = "worker_secret_arns must contain exact Secrets Manager secret ARNs without wildcard characters."
  }
}
