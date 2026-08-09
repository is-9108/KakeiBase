resource "random_password" "db" {
  length  = 32
  special = false # 特殊文字は接続文字列のエスケープ問題を避けるため除外
}

# ------- Secrets Manager -------

resource "aws_secretsmanager_secret" "db" {
  name                    = "${var.project}/${var.env}/db"
  recovery_window_in_days = 0 # ポートフォリオ環境のため即時削除可能にする

  tags = {
    Name    = "${var.project}-${var.env}-db-secret"
    Project = var.project
    Env     = var.env
  }
}

resource "aws_secretsmanager_secret_version" "db" {
  secret_id = aws_secretsmanager_secret.db.id
  secret_string = jsonencode({
    username         = var.db_username
    password         = random_password.db.result
    host             = aws_db_instance.this.address
    port             = tostring(aws_db_instance.this.port)
    dbname           = var.db_name
    connectionString = "Host=${aws_db_instance.this.address};Port=${aws_db_instance.this.port};Database=${var.db_name};Username=${var.db_username};Password=${random_password.db.result}"
  })
}

# ------- RDS -------

resource "aws_db_subnet_group" "this" {
  name       = "${var.project}-${var.env}-db-subnet-group"
  subnet_ids = var.private_db_subnet_ids

  tags = {
    Name    = "${var.project}-${var.env}-db-subnet-group"
    Project = var.project
    Env     = var.env
  }
}

resource "aws_db_instance" "this" {
  identifier        = "${var.project}-${var.env}-db"
  engine            = "postgres"
  engine_version    = "16"
  instance_class    = var.instance_class
  allocated_storage = 20

  db_name  = var.db_name
  username = var.db_username
  password = random_password.db.result

  db_subnet_group_name   = aws_db_subnet_group.this.name
  vpc_security_group_ids = [var.rds_sg_id]

  multi_az            = false
  publicly_accessible = false
  storage_encrypted   = true

  backup_retention_period = 7
  skip_final_snapshot     = true # ポートフォリオ環境のため削除時にスナップショットを省略

  tags = {
    Name    = "${var.project}-${var.env}-db"
    Project = var.project
    Env     = var.env
  }
}
