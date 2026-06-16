locals {
  name_prefix     = lower("${var.project_name}-${var.environment}")
  compact_prefix  = lower(replace("${var.project_name}${var.environment}", "-", ""))
  sql_server_name = "${local.name_prefix}-sql"
  api_app_name    = "${local.name_prefix}-api"

  tags = {
    Project     = "InsightVault"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
  tags     = local.tags

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_log_analytics_workspace" "main" {
  count               = var.enable_paid_hosting ? 1 : 0
  name                = "${local.name_prefix}-logs"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.tags
}

resource "azurerm_application_insights" "api" {
  count               = var.enable_paid_hosting ? 1 : 0
  name                = "${local.name_prefix}-appi"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main[0].id
  application_type    = "web"
  tags                = local.tags
}

resource "azurerm_storage_account" "documents" {
  name                     = var.documents_storage_account_name
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  access_tier              = "Hot"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
  tags                     = local.tags

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_storage_container" "documents" {
  name                  = "documents"
  storage_account_id    = azurerm_storage_account.documents.id
  container_access_type = "private"
}

resource "azurerm_cognitive_account" "foundry" {
  name                          = var.azure_ai_foundry_name
  resource_group_name           = azurerm_resource_group.main.name
  location                      = var.azure_ai_foundry_location
  kind                          = "AIServices"
  sku_name                      = var.azure_ai_foundry_sku_name
  custom_subdomain_name         = var.azure_ai_foundry_name
  project_management_enabled    = true
  public_network_access_enabled = true
  local_auth_enabled            = true
  tags                          = local.tags

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_storage_account" "frontend" {
  count                    = var.enable_paid_hosting ? 1 : 0
  name                     = substr("${local.compact_prefix}web", 0, 24)
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  tags = local.tags
}

resource "azurerm_storage_account_static_website" "frontend" {
  count              = var.enable_paid_hosting ? 1 : 0
  storage_account_id = azurerm_storage_account.frontend[0].id
  index_document     = "index.html"
  error_404_document = "index.html"
}

resource "azurerm_mssql_server" "main" {
  count                        = var.enable_paid_hosting ? 1 : 0
  name                         = local.sql_server_name
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"
  tags                         = local.tags
}

resource "azurerm_mssql_database" "main" {
  count       = var.enable_paid_hosting ? 1 : 0
  name        = "InsightVault"
  server_id   = azurerm_mssql_server.main[0].id
  sku_name    = "Basic"
  max_size_gb = 2
  tags        = local.tags
}

resource "azurerm_mssql_firewall_rule" "azure_services" {
  count            = var.enable_paid_hosting ? 1 : 0
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main[0].id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_firewall_rule" "local_admin" {
  count            = var.enable_paid_hosting && var.allowed_ip_address != "" ? 1 : 0
  name             = "LocalAdmin"
  server_id        = azurerm_mssql_server.main[0].id
  start_ip_address = var.allowed_ip_address
  end_ip_address   = var.allowed_ip_address
}

resource "azurerm_service_plan" "api" {
  count               = var.enable_paid_hosting ? 1 : 0
  name                = "${local.name_prefix}-plan"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = "B1"
  tags                = local.tags
}

resource "azurerm_linux_web_app" "api" {
  count               = var.enable_paid_hosting ? 1 : 0
  name                = local.api_app_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.api[0].id
  https_only          = true
  tags                = local.tags

  site_config {
    always_on           = false
    minimum_tls_version = "1.2"
    ftps_state          = "Disabled"
    application_stack {
      dotnet_version = "10.0"
    }
  }

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = "Server=tcp:${azurerm_mssql_server.main[0].fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main[0].name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                = "Production"
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.api[0].connection_string
    "ConnectionStrings__DefaultConnection"  = "Server=tcp:${azurerm_mssql_server.main[0].fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main[0].name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    "AzureBlobStorage__ConnectionString"    = azurerm_storage_account.documents.primary_connection_string
    "AzureBlobStorage__ContainerName"       = azurerm_storage_container.documents.name
    "AzureOpenAI__Endpoint"                 = var.azure_openai_endpoint
    "AzureOpenAI__ApiKey"                   = var.azure_openai_api_key
    "AzureOpenAI__EmbeddingDeploymentName"  = var.azure_openai_embedding_deployment_name
    "AzureOpenAI__ChatDeploymentName"       = var.azure_openai_chat_deployment_name
    "Jwt__Issuer"                           = "InsightVault"
    "Jwt__Audience"                         = "InsightVault.Client"
    "Jwt__SigningKey"                       = var.jwt_signing_key
    "Jwt__ExpiresMinutes"                   = "60"
    "Cors__AllowedOrigins"                  = join(";", var.frontend_allowed_origins)
  }
}

# Later Terraform components worth adding only when the app needs them:
#
# - Key Vault:
#   Store Jwt__SigningKey, AzureOpenAI__ApiKey, SQL credentials, and storage
#   connection strings outside App Service app settings and Terraform variables.
#
# - Managed identities:
#   Replace connection strings and API keys with Azure AD-based access where SDKs
#   and services support it. Start with Blob Storage, then SQL, then Key Vault.
#
# - Private networking:
#   Add VNet integration, private endpoints, private DNS zones, and firewall
#   restrictions when this needs a production-grade network boundary.
#
# - Azure AI Search:
#   Add only if SQL vector storage is no longer enough or you want managed
#   hybrid search, filtering, scoring profiles, and larger document indexes.
#
# - Foundry agents:
#   Add only if the app moves from direct Azure OpenAI calls to an agentic
#   workflow with tools, state, or managed orchestration.
#
# - Multi-environment modules:
#   Split this into reusable modules when there is a real dev/test/prod need.
#
# - Kubernetes:
#   Not needed for the current app. App Service is simpler and more appropriate.
