variable "project_name" {
  description = "Short lowercase project name used in Azure resource names."
  type        = string
  default     = "insightvault"
}

variable "environment" {
  description = "Deployment environment name."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Primary Azure region for resources."
  type        = string
  default     = "australiaeast"
}

variable "resource_group_name" {
  description = "Name of the Azure resource group."
  type        = string
  default     = "InsightVault-RG"
}

variable "documents_storage_account_name" {
  description = "Name of the storage account used for uploaded documents."
  type        = string
  default     = "insightvaultblobs"
}

variable "azure_ai_foundry_name" {
  description = "Name of the Azure AI Foundry / AI Services resource."
  type        = string
  default     = "insightvault-ai-resource"
}

variable "azure_ai_foundry_location" {
  description = "Azure region for the Azure AI Foundry / AI Services resource."
  type        = string
  default     = "eastus2"
}

variable "azure_ai_foundry_sku_name" {
  description = "SKU for the Azure AI Foundry / AI Services resource."
  type        = string
  default     = "S0"
}

variable "enable_paid_hosting" {
  description = "When true, create paid hosting resources for the API, Azure SQL, frontend static hosting, and monitoring."
  type        = bool
  default     = false
}

variable "sql_admin_login" {
  description = "SQL Server administrator login."
  type        = string
  default     = "insightvaultadmin"
}

variable "sql_admin_password" {
  description = "SQL Server administrator password. Keep this in a local tfvars file or CI secret, never in source control."
  type        = string
  sensitive   = true
  default     = null
}

variable "allowed_ip_address" {
  description = "Optional public IP allowed to connect directly to Azure SQL for local administration. Leave blank to skip."
  type        = string
  default     = ""
}

variable "jwt_signing_key" {
  description = "Production JWT signing key. Use a long random value from a secret store."
  type        = string
  sensitive   = true
  default     = null
}

variable "azure_openai_endpoint" {
  description = "Existing Azure OpenAI endpoint, for example https://resource-name.openai.azure.com."
  type        = string
  default     = ""
}

variable "azure_openai_api_key" {
  description = "Existing Azure OpenAI API key. Keep this in a local tfvars file or CI secret, never in source control."
  type        = string
  sensitive   = true
  default     = null
}

variable "azure_openai_embedding_deployment_name" {
  description = "Existing Azure OpenAI embedding deployment name."
  type        = string
  default     = ""
}

variable "azure_openai_chat_deployment_name" {
  description = "Existing Azure OpenAI chat deployment name."
  type        = string
  default     = ""
}

variable "frontend_allowed_origins" {
  description = "Frontend origins to allow through API CORS. The current API code must also read these from configuration before this takes effect."
  type        = list(string)
  default     = []
}
