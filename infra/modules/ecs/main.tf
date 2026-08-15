data "aws_region" "current" {}

# ------- CloudWatch Log Group -------

resource "aws_cloudwatch_log_group" "api" {
  name              = "/ecs/${var.project}-${var.env}"
  retention_in_days = 30

  tags = {
    Name    = "/ecs/${var.project}-${var.env}"
    Project = var.project
    Env     = var.env
  }
}

# ------- ALB -------

resource "aws_lb" "this" {
  name               = "${var.project}-${var.env}-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [var.alb_sg_id]
  subnets            = var.public_subnet_ids

  tags = {
    Name    = "${var.project}-${var.env}-alb"
    Project = var.project
    Env     = var.env
  }
}

resource "aws_lb_target_group" "api" {
  name        = "${var.project}-${var.env}-api-tg"
  port        = var.app_port
  protocol    = "HTTP"
  vpc_id      = var.vpc_id
  target_type = "ip" # Fargate では ip を指定

  health_check {
    path                = "/health"
    healthy_threshold   = 2
    unhealthy_threshold = 3
    interval            = 30
    timeout             = 5
  }

  tags = {
    Name    = "${var.project}-${var.env}-api-tg"
    Project = var.project
    Env     = var.env
  }
}

resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.this.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.api.arn
  }
}

# ------- ECS Cluster -------

resource "aws_ecs_cluster" "this" {
  name = "${var.project}-${var.env}"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }

  tags = {
    Name    = "${var.project}-${var.env}"
    Project = var.project
    Env     = var.env
  }
}

# ------- IAM: Task Execution Role (ECR pull + Secrets Manager read) -------

resource "aws_iam_role" "ecs_task_execution" {
  name = "${var.project}-${var.env}-ecs-task-execution"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ecs-tasks.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  tags = {
    Name    = "${var.project}-${var.env}-ecs-task-execution"
    Project = var.project
    Env     = var.env
  }
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution" {
  role       = aws_iam_role.ecs_task_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# ------- アプリケーションシークレット (JWT 署名鍵) -------
# database モジュールと同じ random_password -> Secrets Manager のパターンを踏襲する。
# 唯一の利用者が ECS タスクなので、専用モジュールには分けていない。

resource "random_password" "jwt_secret" {
  # HS256 の署名鍵は 32 バイト以上が必須。英数字のみで 64 バイト確保する
  length  = 64
  special = false
}

resource "aws_secretsmanager_secret" "app" {
  name = "${var.project}/${var.env}/app"
  # ポートフォリオ環境のため即時削除できるようにする (database モジュールと同じ方針)
  recovery_window_in_days = 0

  tags = {
    Name    = "${var.project}-${var.env}-app-secret"
    Project = var.project
    Env     = var.env
  }
}

resource "aws_secretsmanager_secret_version" "app" {
  secret_id     = aws_secretsmanager_secret.app.id
  secret_string = jsonencode({ jwtSecretKey = random_password.jwt_secret.result })
}

resource "aws_iam_role_policy" "ecs_read_secrets" {
  name = "${var.project}-${var.env}-ecs-read-secrets"
  role = aws_iam_role.ecs_task_execution.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["secretsmanager:GetSecretValue"]
      Resource = [var.db_secret_arn, aws_secretsmanager_secret.app.arn]
    }]
  })
}

# ------- IAM: Task Role (ECS Exec / SSM) -------

resource "aws_iam_role" "ecs_task" {
  name = "${var.project}-${var.env}-ecs-task"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ecs-tasks.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  tags = {
    Name    = "${var.project}-${var.env}-ecs-task"
    Project = var.project
    Env     = var.env
  }
}

# ECS Exec (踏み台なしでコンテナに接続するため ADR-0007)
resource "aws_iam_role_policy" "ecs_exec" {
  name = "${var.project}-${var.env}-ecs-exec"
  role = aws_iam_role.ecs_task.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "ssmmessages:CreateControlChannel",
        "ssmmessages:CreateDataChannel",
        "ssmmessages:OpenControlChannel",
        "ssmmessages:OpenDataChannel"
      ]
      Resource = "*"
    }]
  })
}

# レシートバケットへの S3 アクセス (Presigned URL 生成に必要)
resource "aws_iam_role_policy" "ecs_s3_receipts" {
  name = "${var.project}-${var.env}-ecs-s3-receipts"
  role = aws_iam_role.ecs_task.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["s3:PutObject", "s3:GetObject", "s3:DeleteObject"]
      Resource = "${var.receipt_bucket_arn}/*"
      }, {
      Effect   = "Allow"
      Action   = ["s3:GetBucketLocation"]
      Resource = var.receipt_bucket_arn
    }]
  })
}

# ------- ECS Task Definition -------

resource "aws_ecs_task_definition" "api" {
  family                   = "${var.project}-${var.env}-api"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = var.cpu
  memory                   = var.memory
  execution_role_arn       = aws_iam_role.ecs_task_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([{
    name      = "api"
    image     = var.container_image
    essential = true

    portMappings = [{
      containerPort = var.app_port
      protocol      = "tcp"
    }]

    environment = [
      { name = "ASPNETCORE_URLS", value = "http://+:${var.app_port}" },
      { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
      { name = "AWS__Region", value = data.aws_region.current.name },
      # S3ReceiptStorageService が読む。未設定だと Presigned URL 発行時に
      # InvalidOperationException になる
      { name = "Aws__S3__ReceiptBucket", value = var.receipt_bucket_name }
    ]

    secrets = [
      # ECS がコンテナ起動時に Secrets Manager から値を注入する
      # Npgsql 接続文字列形式: Host=...;Port=5432;Database=...;Username=...;Password=...
      { name = "ConnectionStrings__DefaultConnection", valueFrom = "${var.db_secret_arn}:connectionString::" },
      # Program.cs は Jwt:SecretKey を ?? string.Empty でフォールバックするため、
      # 未注入だと署名鍵が空文字になり認証が機能しない
      { name = "Jwt__SecretKey", valueFrom = "${aws_secretsmanager_secret.app.arn}:jwtSecretKey::" }
    ]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.api.name
        "awslogs-region"        = data.aws_region.current.name
        "awslogs-stream-prefix" = "ecs"
      }
    }
  }])

  tags = {
    Name    = "${var.project}-${var.env}-api"
    Project = var.project
    Env     = var.env
  }
}

# ------- ECS Service -------

resource "aws_ecs_service" "api" {
  name            = "${var.project}-${var.env}-api"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  # ECS Exec を有効化 (SSM 経由でコンテナに接続可能)
  enable_execute_command = true

  network_configuration {
    subnets          = var.private_app_subnet_ids
    security_groups  = [var.ecs_sg_id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.api.arn
    container_name   = "api"
    container_port   = var.app_port
  }

  depends_on = [
    aws_lb_listener.http,
    aws_iam_role_policy_attachment.ecs_task_execution,
  ]

  tags = {
    Name    = "${var.project}-${var.env}-api"
    Project = var.project
    Env     = var.env
  }
}
