resource "aws_ecr_repository" "api" {
  count                = local.enabled ? 1 : 0
  name                 = "${lower(var.project_name)}-${var.environment}-api"
  image_tag_mutability = "IMMUTABLE"

  image_scanning_configuration { scan_on_push = true }
}

resource "aws_ecr_repository" "worker" {
  count                = local.enabled ? 1 : 0
  name                 = "${lower(var.project_name)}-${var.environment}-worker"
  image_tag_mutability = "IMMUTABLE"

  image_scanning_configuration { scan_on_push = true }
}

resource "aws_ecr_lifecycle_policy" "api" {
  count      = local.enabled ? 1 : 0
  repository = aws_ecr_repository.api[0].name
  policy     = jsonencode({ rules = [{ rulePriority = 1, description = "Keep ten images", selection = { tagStatus = "any", countType = "imageCountMoreThan", countNumber = 10 }, action = { type = "expire" } }] })
}

resource "aws_ecr_lifecycle_policy" "worker" {
  count      = local.enabled ? 1 : 0
  repository = aws_ecr_repository.worker[0].name
  policy     = jsonencode({ rules = [{ rulePriority = 1, description = "Keep ten images", selection = { tagStatus = "any", countType = "imageCountMoreThan", countNumber = 10 }, action = { type = "expire" } }] })
}

resource "aws_cloudwatch_log_group" "api" {
  count             = local.enabled ? 1 : 0
  name              = "/insightvault/${var.environment}/api"
  retention_in_days = 30
}

resource "aws_cloudwatch_log_group" "worker" {
  count             = local.enabled ? 1 : 0
  name              = "/insightvault/${var.environment}/worker"
  retention_in_days = 30
}

resource "aws_ecs_cluster" "this" {
  count = local.enabled ? 1 : 0
  name  = "${var.project_name}-${var.environment}"
}

resource "aws_iam_role" "ecs_execution" {
  count              = local.enabled ? 1 : 0
  name               = "${var.project_name}-${var.environment}-ecs-execution"
  assume_role_policy = data.aws_iam_policy_document.ecs_task_assume_role.json
}

resource "aws_iam_role_policy_attachment" "ecs_execution" {
  count      = local.enabled ? 1 : 0
  role       = aws_iam_role.ecs_execution[0].name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

data "aws_iam_policy_document" "ecs_execution_secrets" {
  count = local.enabled ? 1 : 0

  dynamic "statement" {
    for_each = length(merge(var.api_task_secrets, var.worker_task_secrets)) == 0 ? [] : [true]
    content {
      sid       = "ReadOnlyConfiguredStartupSecrets"
      effect    = "Allow"
      actions   = ["secretsmanager:GetSecretValue"]
      resources = distinct(concat(values(var.api_task_secrets), values(var.worker_task_secrets)))
    }
  }
}

resource "aws_iam_role_policy" "ecs_execution_secrets" {
  count  = local.enabled && length(merge(var.api_task_secrets, var.worker_task_secrets)) > 0 ? 1 : 0
  name   = "${var.project_name}-${var.environment}-ecs-execution-secrets"
  role   = aws_iam_role.ecs_execution[0].id
  policy = data.aws_iam_policy_document.ecs_execution_secrets[0].json
}

resource "aws_ecs_task_definition" "api" {
  count                    = local.enabled ? 1 : 0
  family                   = "${var.project_name}-${var.environment}-api"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = var.api_cpu
  memory                   = var.api_memory
  execution_role_arn       = aws_iam_role.ecs_execution[0].arn
  task_role_arn            = aws_iam_role.api_task[0].arn

  container_definitions = jsonencode([{
    name         = "api"
    image        = var.api_container_image
    essential    = true
    portMappings = [{ containerPort = 8080, hostPort = 8080, protocol = "tcp" }]
    environment = [
      { name = "ASPNETCORE_URLS", value = "http://+:8080" },
      { name = "HttpsRedirection__Enabled", value = "false" },
      { name = "Database__Provider", value = "Postgres" },
      { name = "Storage__Provider", value = "S3" },
      { name = "S3Storage__BucketName", value = aws_s3_bucket.documents[0].bucket },
      { name = "S3Storage__Region", value = var.aws_region },
      { name = "Queue__Provider", value = "Sqs" },
      { name = "Queue__Region", value = var.aws_region },
      { name = "Queue__QueueUrl", value = aws_sqs_queue.document_processing[0].url },
      { name = "ForwardedHeaders__TrustedProxyNetworks__0", value = var.vpc_cidr }
    ]
    secrets          = [for name, arn in var.api_task_secrets : { name = name, valueFrom = arn }]
    logConfiguration = { logDriver = "awslogs", options = { awslogs-group = aws_cloudwatch_log_group.api[0].name, awslogs-region = var.aws_region, awslogs-stream-prefix = "ecs" } }
  }])

  lifecycle {
    precondition {
      condition     = !local.enabled || can(regex("@sha256:[0-9a-fA-F]{64}$", var.api_container_image))
      error_message = "api_container_image must be an immutable image URI ending in @sha256:<64 hexadecimal characters> when production infrastructure is enabled."
    }
  }
}

resource "aws_ecs_task_definition" "worker" {
  count                    = local.enabled ? 1 : 0
  family                   = "${var.project_name}-${var.environment}-worker"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = var.worker_cpu
  memory                   = var.worker_memory
  execution_role_arn       = aws_iam_role.ecs_execution[0].arn
  task_role_arn            = aws_iam_role.worker_task[0].arn

  container_definitions = jsonencode([{
    name      = "worker"
    image     = var.worker_container_image
    essential = true
    environment = [
      { name = "Database__Provider", value = "Postgres" },
      { name = "Storage__Provider", value = "S3" },
      { name = "S3Storage__BucketName", value = aws_s3_bucket.documents[0].bucket },
      { name = "S3Storage__Region", value = var.aws_region },
      { name = "Queue__Provider", value = "Sqs" },
      { name = "Queue__Region", value = var.aws_region },
      { name = "Queue__QueueUrl", value = aws_sqs_queue.document_processing[0].url }
    ]
    secrets          = [for name, arn in var.worker_task_secrets : { name = name, valueFrom = arn }]
    logConfiguration = { logDriver = "awslogs", options = { awslogs-group = aws_cloudwatch_log_group.worker[0].name, awslogs-region = var.aws_region, awslogs-stream-prefix = "ecs" } }
  }])

  lifecycle {
    precondition {
      condition     = !local.enabled || can(regex("@sha256:[0-9a-fA-F]{64}$", var.worker_container_image))
      error_message = "worker_container_image must be an immutable image URI ending in @sha256:<64 hexadecimal characters> when production infrastructure is enabled."
    }
  }
}

resource "aws_ecs_service" "api" {
  count           = local.enabled ? 1 : 0
  name            = "api"
  cluster         = aws_ecs_cluster.this[0].id
  task_definition = aws_ecs_task_definition.api[0].arn
  desired_count   = var.api_desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = values(aws_subnet.app)[*].id
    security_groups  = [aws_security_group.api[0].id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.api[0].arn
    container_name   = "api"
    container_port   = 8080
  }

  depends_on = [aws_lb_listener.https]
}

resource "aws_ecs_service" "worker" {
  count           = local.enabled ? 1 : 0
  name            = "worker"
  cluster         = aws_ecs_cluster.this[0].id
  task_definition = aws_ecs_task_definition.worker[0].arn
  desired_count   = var.worker_desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = values(aws_subnet.app)[*].id
    security_groups  = [aws_security_group.worker[0].id]
    assign_public_ip = false
  }
}
