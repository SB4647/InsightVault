data "aws_availability_zones" "available" {
  count = local.enabled ? 1 : 0
  state = "available"
}

locals {
  availability_zones = local.enabled ? slice(data.aws_availability_zones.available[0].names, 0, 2) : []
  public_subnets     = { for index, zone in local.availability_zones : zone => cidrsubnet(var.vpc_cidr, 4, index) }
  app_subnets        = { for index, zone in local.availability_zones : zone => cidrsubnet(var.vpc_cidr, 4, index + 2) }
  data_subnets       = { for index, zone in local.availability_zones : zone => cidrsubnet(var.vpc_cidr, 4, index + 4) }
}

resource "aws_vpc" "this" {
  count                = local.enabled ? 1 : 0
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = merge(local.common_tags, { Name = "${var.project_name}-${var.environment}" })

  lifecycle {
    precondition {
      condition     = !local.enabled || var.enable_nat_gateway
      error_message = "enable_nat_gateway must be true for this production design because private ECS tasks need outbound access to ECR, CloudWatch, Secrets Manager, S3, SQS, and Azure OpenAI."
    }
  }
}

resource "aws_internet_gateway" "this" {
  count  = local.enabled ? 1 : 0
  vpc_id = aws_vpc.this[0].id
}

resource "aws_subnet" "public" {
  for_each                = local.public_subnets
  vpc_id                  = aws_vpc.this[0].id
  availability_zone       = each.key
  cidr_block              = each.value
  map_public_ip_on_launch = false
  tags                    = merge(local.common_tags, { Name = "${var.project_name}-${var.environment}-public-${each.key}" })
}

resource "aws_subnet" "app" {
  for_each          = local.app_subnets
  vpc_id            = aws_vpc.this[0].id
  availability_zone = each.key
  cidr_block        = each.value
  tags              = merge(local.common_tags, { Name = "${var.project_name}-${var.environment}-app-${each.key}" })
}

resource "aws_subnet" "data" {
  for_each          = local.data_subnets
  vpc_id            = aws_vpc.this[0].id
  availability_zone = each.key
  cidr_block        = each.value
  tags              = merge(local.common_tags, { Name = "${var.project_name}-${var.environment}-data-${each.key}" })
}

resource "aws_route_table" "public" {
  count  = local.enabled ? 1 : 0
  vpc_id = aws_vpc.this[0].id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.this[0].id
  }
}

resource "aws_route_table_association" "public" {
  for_each       = aws_subnet.public
  subnet_id      = each.value.id
  route_table_id = aws_route_table.public[0].id
}

# Private tasks intentionally have no default route unless egress is explicitly approved.
resource "aws_eip" "nat" {
  count  = local.enabled && var.enable_nat_gateway ? 1 : 0
  domain = "vpc"
}

resource "aws_nat_gateway" "this" {
  count         = local.enabled && var.enable_nat_gateway ? 1 : 0
  allocation_id = aws_eip.nat[0].id
  subnet_id     = values(aws_subnet.public)[0].id
  depends_on    = [aws_internet_gateway.this]
}

resource "aws_route_table" "app" {
  count  = local.enabled ? 1 : 0
  vpc_id = aws_vpc.this[0].id

  dynamic "route" {
    for_each = local.enabled && var.enable_nat_gateway ? [true] : []
    content {
      cidr_block     = "0.0.0.0/0"
      nat_gateway_id = aws_nat_gateway.this[0].id
    }
  }
}

resource "aws_route_table_association" "app" {
  for_each       = aws_subnet.app
  subnet_id      = each.value.id
  route_table_id = aws_route_table.app[0].id
}

resource "aws_route_table" "data" {
  count  = local.enabled ? 1 : 0
  vpc_id = aws_vpc.this[0].id
}

resource "aws_route_table_association" "data" {
  for_each       = aws_subnet.data
  subnet_id      = each.value.id
  route_table_id = aws_route_table.data[0].id
}

resource "aws_security_group" "alb" {
  count       = local.enabled ? 1 : 0
  name        = "${var.project_name}-${var.environment}-alb"
  description = "Public HTTPS entry point for InsightVault."
  vpc_id      = aws_vpc.this[0].id

  ingress {
    description = "HTTP redirect"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  ingress {
    description = "HTTPS"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "api" {
  count       = local.enabled ? 1 : 0
  name        = "${var.project_name}-${var.environment}-api"
  description = "API tasks accept HTTP only from the load balancer."
  vpc_id      = aws_vpc.this[0].id

  ingress {
    description     = "ALB to API"
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb[0].id]
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "worker" {
  count       = local.enabled ? 1 : 0
  name        = "${var.project_name}-${var.environment}-worker"
  description = "Worker tasks receive no inbound network traffic."
  vpc_id      = aws_vpc.this[0].id
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "database" {
  count       = local.enabled ? 1 : 0
  name        = "${var.project_name}-${var.environment}-database"
  description = "PostgreSQL is reachable only by InsightVault tasks."
  vpc_id      = aws_vpc.this[0].id

  ingress {
    description     = "API PostgreSQL"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.api[0].id]
  }
  ingress {
    description     = "Worker PostgreSQL"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.worker[0].id]
  }
}
