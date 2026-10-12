resource "aws_lb" "api" {
  count                      = local.enabled ? 1 : 0
  name                       = "${lower(var.project_name)}-${var.environment}-api"
  internal                   = false
  load_balancer_type         = "application"
  security_groups            = [aws_security_group.alb[0].id]
  subnets                    = values(aws_subnet.public)[*].id
  enable_deletion_protection = var.environment == "production"
}

resource "aws_lb_target_group" "api" {
  count       = local.enabled ? 1 : 0
  name        = "${lower(var.project_name)}-${var.environment}-api"
  port        = 8080
  protocol    = "HTTP"
  target_type = "ip"
  vpc_id      = aws_vpc.this[0].id

  health_check {
    enabled             = true
    path                = "/health"
    protocol            = "HTTP"
    matcher             = "200"
    healthy_threshold   = 2
    unhealthy_threshold = 3
  }
}

resource "aws_lb_listener" "http" {
  count             = local.enabled ? 1 : 0
  load_balancer_arn = aws_lb.api[0].arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type = "redirect"
    redirect {
      port        = "443"
      protocol    = "HTTPS"
      status_code = "HTTP_301"
    }
  }
}

resource "aws_lb_listener" "https" {
  count             = local.enabled ? 1 : 0
  load_balancer_arn = aws_lb.api[0].arn
  port              = 443
  protocol          = "HTTPS"
  certificate_arn   = var.certificate_arn

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.api[0].arn
  }

  lifecycle {
    precondition {
      condition     = !local.enabled || can(regex("^arn:aws[a-z-]*:acm:[^:]+:[0-9]{12}:certificate/.+", var.certificate_arn))
      error_message = "certificate_arn must be an existing ACM certificate ARN when production infrastructure is enabled."
    }
  }
}

resource "aws_db_subnet_group" "postgres" {
  count      = local.enabled ? 1 : 0
  name       = "${lower(var.project_name)}-${var.environment}-postgres"
  subnet_ids = values(aws_subnet.data)[*].id
}

resource "aws_db_parameter_group" "postgres" {
  count  = local.enabled ? 1 : 0
  name   = "${lower(var.project_name)}-${var.environment}-postgres17"
  family = "postgres17"

  parameter {
    name  = "rds.force_ssl"
    value = "1"
  }
}

resource "aws_db_instance" "postgres" {
  count                       = local.enabled ? 1 : 0
  identifier                  = "${lower(var.project_name)}-${var.environment}-postgres"
  engine                      = "postgres"
  engine_version              = "17"
  instance_class              = var.database_instance_class
  allocated_storage           = 20
  max_allocated_storage       = 100
  storage_encrypted           = true
  publicly_accessible         = false
  db_name                     = var.database_name
  username                    = var.database_username
  manage_master_user_password = true
  db_subnet_group_name        = aws_db_subnet_group.postgres[0].name
  vpc_security_group_ids      = [aws_security_group.database[0].id]
  parameter_group_name        = aws_db_parameter_group.postgres[0].name
  backup_retention_period     = var.environment == "production" ? 14 : 7
  deletion_protection         = var.environment == "production"
  skip_final_snapshot         = var.environment != "production"
  apply_immediately           = false

  lifecycle {
    precondition {
      condition     = !local.enabled || length(trimspace(var.database_name)) > 0
      error_message = "database_name must be configured when production infrastructure is enabled."
    }
  }
}
