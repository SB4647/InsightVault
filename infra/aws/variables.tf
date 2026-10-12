variable "aws_region" {
  description = "AWS Region for an explicitly approved deployment. InsightVault is designed for Sydney by default."
  type        = string
  default     = "ap-southeast-2"
}

variable "project_name" {
  description = "Portfolio project name applied to AWS resources."
  type        = string
  default     = "InsightVault"
}

variable "environment" {
  description = "Deployment environment name. Use development, staging, or production."
  type        = string
  default     = "development"

  validation {
    condition     = contains(["development", "staging", "production"], var.environment)
    error_message = "environment must be development, staging, or production."
  }
}

variable "managed_by" {
  description = "Tag identifying the infrastructure tool."
  type        = string
  default     = "Terraform"
}

variable "enable_production_infrastructure" {
  description = "Master safety switch. False creates no AWS resources; set true only in a reviewed, manually approved deployment configuration."
  type        = bool
  default     = false
}

variable "documents_bucket_name" {
  description = "Globally unique name for the private documents bucket. Required only when enable_production_infrastructure is true."
  type        = string
  default     = ""
}

variable "vpc_cidr" {
  description = "CIDR block for the production VPC."
  type        = string
  default     = "10.42.0.0/16"
}

variable "enable_nat_gateway" {
  description = "Creates a chargeable NAT Gateway for private workload egress. Keep false for validation-only work; production Azure OpenAI access normally needs this or approved VPC endpoints/proxy egress."
  type        = bool
  default     = false
}

variable "certificate_arn" {
  description = "Existing ACM certificate ARN for the HTTPS listener. Terraform does not request certificates or configure DNS."
  type        = string
  default     = ""
}

variable "api_container_image" {
  description = "Immutable API container image URI, normally from the API ECR repository."
  type        = string
  default     = ""
}

variable "worker_container_image" {
  description = "Immutable worker container image URI, normally from the worker ECR repository."
  type        = string
  default     = ""
}

variable "api_task_secrets" {
  description = "Map of API environment variable names to exact existing Secrets Manager ARNs. No secret values are stored in Terraform."
  type        = map(string)
  default     = {}

  validation {
    condition = alltrue([
      for arn in values(var.api_task_secrets) :
      !strcontains(arn, "*") && can(regex("^arn:aws[a-z-]*:secretsmanager:[^:]+:[0-9]{12}:secret:.+", arn))
    ])
    error_message = "api_task_secrets values must be exact Secrets Manager secret ARNs without wildcard characters."
  }
}

variable "worker_task_secrets" {
  description = "Map of worker environment variable names to exact existing Secrets Manager ARNs. No secret values are stored in Terraform."
  type        = map(string)
  default     = {}

  validation {
    condition = alltrue([
      for arn in values(var.worker_task_secrets) :
      !strcontains(arn, "*") && can(regex("^arn:aws[a-z-]*:secretsmanager:[^:]+:[0-9]{12}:secret:.+", arn))
    ])
    error_message = "worker_task_secrets values must be exact Secrets Manager secret ARNs without wildcard characters."
  }
}

variable "api_cpu" {
  description = "Fargate CPU units for the API task."
  type        = number
  default     = 512
}

variable "api_memory" {
  description = "Fargate memory MiB for the API task."
  type        = number
  default     = 1024
}

variable "worker_cpu" {
  description = "Fargate CPU units for the worker task."
  type        = number
  default     = 512
}

variable "worker_memory" {
  description = "Fargate memory MiB for the worker task."
  type        = number
  default     = 1024
}

variable "api_desired_count" {
  description = "Desired API task count after a real deployment."
  type        = number
  default     = 2
}

variable "worker_desired_count" {
  description = "Desired worker task count after a real deployment."
  type        = number
  default     = 1
}

variable "database_instance_class" {
  description = "RDS instance class selected for a real deployment."
  type        = string
  default     = "db.t4g.small"
}

variable "database_name" {
  description = "Initial PostgreSQL database name. Credentials must be supplied from Secrets Manager through the ECS task definitions."
  type        = string
  default     = "insightvault"
}

variable "database_username" {
  description = "Initial PostgreSQL administrator username. Passwords are intentionally not accepted by this module."
  type        = string
  default     = "insightvaultadmin"
}
