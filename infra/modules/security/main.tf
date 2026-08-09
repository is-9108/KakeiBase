# Security Groups are created without inline rules to avoid circular dependencies
# (ALB SG ↔ ECS SG). All cross-SG rules are added via aws_security_group_rule.

# ------- Security Groups -------

resource "aws_security_group" "alb" {
  name        = "${var.project}-${var.env}-alb-sg"
  description = "ALB: HTTPS/HTTP from allowed CIDR, egress to ECS 8080"
  vpc_id      = var.vpc_id

  tags = {
    Name    = "${var.project}-${var.env}-alb-sg"
    Project = var.project
    Env     = var.env
  }
}

resource "aws_security_group" "ecs" {
  name        = "${var.project}-${var.env}-ecs-sg"
  description = "ECS: ingress 8080 from ALB, egress to RDS 5432 and HTTPS"
  vpc_id      = var.vpc_id

  tags = {
    Name    = "${var.project}-${var.env}-ecs-sg"
    Project = var.project
    Env     = var.env
  }
}

resource "aws_security_group" "lambda" {
  name        = "${var.project}-${var.env}-lambda-sg"
  description = "Lambda: no ingress, egress to RDS 5432 and HTTPS"
  vpc_id      = var.vpc_id

  tags = {
    Name    = "${var.project}-${var.env}-lambda-sg"
    Project = var.project
    Env     = var.env
  }
}

resource "aws_security_group" "rds" {
  name        = "${var.project}-${var.env}-rds-sg"
  description = "RDS: ingress 5432 from ECS and Lambda only"
  vpc_id      = var.vpc_id

  tags = {
    Name    = "${var.project}-${var.env}-rds-sg"
    Project = var.project
    Env     = var.env
  }
}

# ------- ALB Rules -------

resource "aws_security_group_rule" "alb_ingress_https" {
  security_group_id = aws_security_group.alb.id
  type              = "ingress"
  description       = "HTTPS from allowed CIDR"
  from_port         = 443
  to_port           = 443
  protocol          = "tcp"
  cidr_blocks       = [var.allowed_cidr]
}

resource "aws_security_group_rule" "alb_ingress_http" {
  security_group_id = aws_security_group.alb.id
  type              = "ingress"
  description       = "HTTP from allowed CIDR"
  from_port         = 80
  to_port           = 80
  protocol          = "tcp"
  cidr_blocks       = [var.allowed_cidr]
}

resource "aws_security_group_rule" "alb_egress_to_ecs" {
  security_group_id        = aws_security_group.alb.id
  type                     = "egress"
  description              = "To ECS on port 8080"
  from_port                = 8080
  to_port                  = 8080
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.ecs.id
}

# ------- ECS Rules -------

resource "aws_security_group_rule" "ecs_ingress_from_alb" {
  security_group_id        = aws_security_group.ecs.id
  type                     = "ingress"
  description              = "HTTP from ALB"
  from_port                = 8080
  to_port                  = 8080
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.alb.id
}

resource "aws_security_group_rule" "ecs_egress_to_rds" {
  security_group_id        = aws_security_group.ecs.id
  type                     = "egress"
  description              = "To RDS on port 5432"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.rds.id
}

resource "aws_security_group_rule" "ecs_egress_https" {
  security_group_id = aws_security_group.ecs.id
  type              = "egress"
  description       = "HTTPS outbound (ECR, SSM, AWS APIs, etc.)"
  from_port         = 443
  to_port           = 443
  protocol          = "tcp"
  cidr_blocks       = ["0.0.0.0/0"]
}

# ------- Lambda Rules -------

resource "aws_security_group_rule" "lambda_egress_to_rds" {
  security_group_id        = aws_security_group.lambda.id
  type                     = "egress"
  description              = "To RDS on port 5432"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.rds.id
}

resource "aws_security_group_rule" "lambda_egress_https" {
  security_group_id = aws_security_group.lambda.id
  type              = "egress"
  description       = "HTTPS outbound (Textract, SES, AWS APIs, etc.)"
  from_port         = 443
  to_port           = 443
  protocol          = "tcp"
  cidr_blocks       = ["0.0.0.0/0"]
}

# ------- RDS Rules -------

resource "aws_security_group_rule" "rds_ingress_from_ecs" {
  security_group_id        = aws_security_group.rds.id
  type                     = "ingress"
  description              = "PostgreSQL from ECS"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.ecs.id
}

resource "aws_security_group_rule" "rds_ingress_from_lambda" {
  security_group_id        = aws_security_group.rds.id
  type                     = "ingress"
  description              = "PostgreSQL from Lambda"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.lambda.id
}
