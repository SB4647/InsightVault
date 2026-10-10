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

output "document_processing_queue_url" {
  description = "URL of the processing queue when explicitly enabled."
  value       = try(aws_sqs_queue.document_processing[0].url, null)
}

output "document_processing_dlq_url" {
  description = "URL of the processing dead-letter queue when explicitly enabled."
  value       = try(aws_sqs_queue.document_processing_dlq[0].url, null)
}

output "api_task_role_arn" {
  description = "ARN of the least-privilege API workload role when explicitly enabled with the processing queues."
  value       = try(aws_iam_role.api_task[0].arn, null)
}

output "worker_task_role_arn" {
  description = "ARN of the least-privilege worker workload role when explicitly enabled with the processing queues."
  value       = try(aws_iam_role.worker_task[0].arn, null)
}
