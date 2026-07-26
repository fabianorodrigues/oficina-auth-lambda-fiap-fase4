resource "aws_security_group" "auth_cpf" {
  name        = "oficina-auth-cpf-lambda"
  description = "Security group for oficina-auth-cpf Lambda."
  vpc_id      = data.aws_ssm_parameter.vpc_id.value

  egress {
    description     = "SQL Server to Cadastro RDS"
    from_port       = 1433
    to_port         = 1433
    protocol        = "tcp"
    security_groups = [data.aws_ssm_parameter.rds_security_group_id.value]
  }

  egress {
    description     = "HTTPS to Secrets Manager VPC endpoint"
    from_port       = 443
    to_port         = 443
    protocol        = "tcp"
    security_groups = [aws_security_group.secretsmanager_endpoint.id]
  }

  tags = merge(local.tags, { Name = "oficina-auth-cpf-lambda" })
}

resource "aws_security_group" "secretsmanager_endpoint" {
  name        = "oficina-auth-secretsmanager-endpoint"
  description = "Security group for the Secrets Manager VPC endpoint used by auth-cpf."
  vpc_id      = data.aws_ssm_parameter.vpc_id.value

  tags = merge(local.tags, { Name = "oficina-auth-secretsmanager-endpoint" })
}

resource "aws_vpc_security_group_ingress_rule" "secretsmanager_from_auth_cpf_https" {
  security_group_id            = aws_security_group.secretsmanager_endpoint.id
  description                  = "HTTPS from auth-cpf Lambda"
  ip_protocol                  = "tcp"
  from_port                    = 443
  to_port                      = 443
  referenced_security_group_id = aws_security_group.auth_cpf.id

  tags = merge(local.tags, { Name = "oficina-auth-secretsmanager-ingress-auth-cpf" })
}

resource "aws_vpc_security_group_ingress_rule" "secretsmanager_from_k8s_node_https" {
  security_group_id            = aws_security_group.secretsmanager_endpoint.id
  description                  = "HTTPS from K3s node"
  ip_protocol                  = "tcp"
  from_port                    = 443
  to_port                      = 443
  referenced_security_group_id = data.aws_ssm_parameter.k8s_security_group_id.value

  tags = merge(local.tags, { Name = "oficina-auth-secretsmanager-ingress-k3s" })
}

resource "aws_vpc_security_group_ingress_rule" "rds_from_auth_cpf_sql" {
  security_group_id            = data.aws_ssm_parameter.rds_security_group_id.value
  description                  = "SQL Server from auth-cpf Lambda"
  ip_protocol                  = "tcp"
  from_port                    = 1433
  to_port                      = 1433
  referenced_security_group_id = aws_security_group.auth_cpf.id

  tags = merge(local.tags, { Name = "oficina-auth-rds-ingress-auth-cpf" })
}

resource "aws_vpc_endpoint" "secretsmanager" {
  vpc_id              = data.aws_ssm_parameter.vpc_id.value
  service_name        = "com.amazonaws.${var.aws_region}.secretsmanager"
  vpc_endpoint_type   = "Interface"
  private_dns_enabled = true
  subnet_ids = [
    data.aws_ssm_parameter.private_subnet_1.value,
    data.aws_ssm_parameter.private_subnet_2.value
  ]
  security_group_ids = [aws_security_group.secretsmanager_endpoint.id]

  tags = merge(local.tags, { Name = "oficina-auth-secretsmanager" })
}
