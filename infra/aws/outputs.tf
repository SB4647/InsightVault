output "configuration_summary" {
  description = "Validation-only summary; this module creates no AWS resources."
  value = {
    environment = var.environment
    project     = var.project_name
    region      = var.aws_region
  }
}
