data "aws_caller_identity" "current" {}

data "aws_ssm_parameter" "vpc_id" {
  name = "/oficina/infra/vpc/id"
}

data "aws_ssm_parameter" "private_subnet_1" {
  name = "/oficina/infra/subnets/private/1"
}

data "aws_ssm_parameter" "private_subnet_2" {
  name = "/oficina/infra/subnets/private/2"
}

data "aws_ssm_parameter" "rds_security_group_id" {
  name = "/oficina/infra/rds/security-group-id"
}

data "aws_secretsmanager_secret" "database" {
  name = local.database_secret_name
}

