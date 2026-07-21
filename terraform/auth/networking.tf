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
}

