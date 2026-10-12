output "configuration_summary" {
  description = "Safety-focused configuration summary. False means Terraform creates no AWS resources."
  value = {
    environment                  = var.environment
    project                      = var.project_name
    region                       = var.aws_region
    production_infrastructure_on = var.enable_production_infrastructure
    nat_gateway_on               = var.enable_nat_gateway
  }
}

output "documents_bucket_name" {
  description = "Private documents bucket name after an explicitly approved deployment."
  value       = try(aws_s3_bucket.documents[0].bucket, null)
}

output "document_processing_queue_url" {
  description = "Processing queue URL after an explicitly approved deployment."
  value       = try(aws_sqs_queue.document_processing[0].url, null)
}

output "api_task_role_arn" {
  description = "Least-privilege API task role ARN after an explicitly approved deployment."
  value       = try(aws_iam_role.api_task[0].arn, null)
}

output "worker_task_role_arn" {
  description = "Least-privilege worker task role ARN after an explicitly approved deployment."
  value       = try(aws_iam_role.worker_task[0].arn, null)
}

output "api_load_balancer_dns_name" {
  description = "Public ALB DNS name after deployment. A DNS record is deliberately out of scope."
  value       = try(aws_lb.api[0].dns_name, null)
}

output "postgres_endpoint" {
  description = "Private PostgreSQL endpoint after deployment. Database migrations are a separate manual release action."
  value       = try(aws_db_instance.postgres[0].address, null)
}
