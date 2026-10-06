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
