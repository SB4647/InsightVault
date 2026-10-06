output "configuration_summary" {
  description = "Configuration summary; resource changes require an explicit Terraform apply."
  value = {
    environment = var.environment
    project     = var.project_name
    region      = var.aws_region
  }
}

output "documents_bucket_name" {
  description = "Name of the private documents bucket after an explicitly approved deployment."
  value       = aws_s3_bucket.documents.bucket
}

output "documents_bucket_arn" {
  description = "ARN of the private documents bucket after an explicitly approved deployment."
  value       = aws_s3_bucket.documents.arn
}
