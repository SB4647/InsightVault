locals {
  enabled = var.enable_production_infrastructure

  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = var.managed_by
  }
}

# The document bucket is private, encrypted with the AWS-managed S3 key, and only exists after an approved apply.
resource "aws_s3_bucket" "documents" {
  count         = local.enabled ? 1 : 0
  bucket        = var.documents_bucket_name
  force_destroy = false

  lifecycle {
    precondition {
      condition     = !local.enabled || length(trimspace(var.documents_bucket_name)) > 0
      error_message = "documents_bucket_name is required when production infrastructure is enabled."
    }
  }
}

resource "aws_s3_bucket_public_access_block" "documents" {
  count  = local.enabled ? 1 : 0
  bucket = aws_s3_bucket.documents[0].id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_ownership_controls" "documents" {
  count  = local.enabled ? 1 : 0
  bucket = aws_s3_bucket.documents[0].id

  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "documents" {
  count  = local.enabled ? 1 : 0
  bucket = aws_s3_bucket.documents[0].id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "documents" {
  count  = local.enabled ? 1 : 0
  bucket = aws_s3_bucket.documents[0].id

  rule {
    id     = "clean-up-incomplete-uploads"
    status = "Enabled"
    filter {}
    abort_incomplete_multipart_upload {
      days_after_initiation = 7
    }
  }
}

data "aws_iam_policy_document" "documents_bucket_tls_only" {
  count = local.enabled ? 1 : 0

  statement {
    sid    = "DenyInsecureTransport"
    effect = "Deny"
    principals {
      type        = "*"
      identifiers = ["*"]
    }
    actions = ["s3:*"]
    resources = [
      aws_s3_bucket.documents[0].arn,
      "${aws_s3_bucket.documents[0].arn}/*"
    ]
    condition {
      test     = "Bool"
      variable = "aws:SecureTransport"
      values   = ["false"]
    }
  }
}

resource "aws_s3_bucket_policy" "documents" {
  count  = local.enabled ? 1 : 0
  bucket = aws_s3_bucket.documents[0].id
  policy = data.aws_iam_policy_document.documents_bucket_tls_only[0].json
}

resource "aws_sqs_queue" "document_processing_dlq" {
  count                   = local.enabled ? 1 : 0
  name                    = "${var.project_name}-${var.environment}-document-processing-dlq"
  sqs_managed_sse_enabled = true
}

resource "aws_sqs_queue" "document_processing" {
  count                      = local.enabled ? 1 : 0
  name                       = "${var.project_name}-${var.environment}-document-processing"
  receive_wait_time_seconds  = 20
  visibility_timeout_seconds = 900
  sqs_managed_sse_enabled    = true
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.document_processing_dlq[0].arn
    maxReceiveCount     = 3
  })
}

data "aws_iam_policy_document" "ecs_task_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "api_task" {
  count              = local.enabled ? 1 : 0
  name               = "${var.project_name}-${var.environment}-api-task"
  assume_role_policy = data.aws_iam_policy_document.ecs_task_assume_role.json
}

data "aws_iam_policy_document" "api_task" {
  count = local.enabled ? 1 : 0

  statement {
    sid       = "ListPrivateDocumentBucket"
    effect    = "Allow"
    actions   = ["s3:ListBucket"]
    resources = [aws_s3_bucket.documents[0].arn]
  }
  statement {
    sid       = "ManagePrivateDocumentObjects"
    effect    = "Allow"
    actions   = ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"]
    resources = ["${aws_s3_bucket.documents[0].arn}/*"]
  }
  statement {
    sid       = "QueueDocumentProcessing"
    effect    = "Allow"
    actions   = ["sqs:SendMessage"]
    resources = [aws_sqs_queue.document_processing[0].arn]
  }

  dynamic "statement" {
    for_each = length(var.api_task_secrets) == 0 ? [] : [true]
    content {
      sid       = "ReadConfiguredApiSecrets"
      effect    = "Allow"
      actions   = ["secretsmanager:GetSecretValue"]
      resources = values(var.api_task_secrets)
    }
  }
}

resource "aws_iam_role_policy" "api_task" {
  count  = local.enabled ? 1 : 0
  name   = "${var.project_name}-${var.environment}-api-task"
  role   = aws_iam_role.api_task[0].id
  policy = data.aws_iam_policy_document.api_task[0].json
}

resource "aws_iam_role" "worker_task" {
  count              = local.enabled ? 1 : 0
  name               = "${var.project_name}-${var.environment}-worker-task"
  assume_role_policy = data.aws_iam_policy_document.ecs_task_assume_role.json
}

data "aws_iam_policy_document" "worker_task" {
  count = local.enabled ? 1 : 0

  statement {
    sid       = "ReadPrivateDocumentObjects"
    effect    = "Allow"
    actions   = ["s3:GetObject"]
    resources = ["${aws_s3_bucket.documents[0].arn}/*"]
  }
  statement {
    sid       = "ProcessDocumentJobs"
    effect    = "Allow"
    actions   = ["sqs:ReceiveMessage", "sqs:DeleteMessage", "sqs:ChangeMessageVisibility", "sqs:GetQueueAttributes"]
    resources = [aws_sqs_queue.document_processing[0].arn]
  }

  dynamic "statement" {
    for_each = length(var.worker_task_secrets) == 0 ? [] : [true]
    content {
      sid       = "ReadConfiguredWorkerSecrets"
      effect    = "Allow"
      actions   = ["secretsmanager:GetSecretValue"]
      resources = values(var.worker_task_secrets)
    }
  }
}

resource "aws_iam_role_policy" "worker_task" {
  count  = local.enabled ? 1 : 0
  name   = "${var.project_name}-${var.environment}-worker-task"
  role   = aws_iam_role.worker_task[0].id
  policy = data.aws_iam_policy_document.worker_task[0].json
}
