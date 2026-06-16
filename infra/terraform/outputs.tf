output "resource_group_name" {
  description = "Created resource group name."
  value       = azurerm_resource_group.main.name
}

output "api_app_name" {
  description = "Azure App Service name for the API."
  value       = var.enable_paid_hosting ? azurerm_linux_web_app.api[0].name : null
}

output "api_url" {
  description = "Public API URL."
  value       = var.enable_paid_hosting ? "https://${azurerm_linux_web_app.api[0].default_hostname}" : null
}

output "frontend_static_website_url" {
  description = "Static website endpoint for the React build artifacts."
  value       = var.enable_paid_hosting ? azurerm_storage_account.frontend[0].primary_web_endpoint : null
}

output "documents_storage_account_name" {
  description = "Storage account used for uploaded documents."
  value       = azurerm_storage_account.documents.name
}

output "sql_server_fqdn" {
  description = "Azure SQL Server fully qualified domain name."
  value       = var.enable_paid_hosting ? azurerm_mssql_server.main[0].fully_qualified_domain_name : null
}

output "sql_database_name" {
  description = "Azure SQL database name."
  value       = var.enable_paid_hosting ? azurerm_mssql_database.main[0].name : null
}
