resource "aws_secretsmanager_secret" "jwt" {
  name                    = local.jwt_secret_name
  description             = "JWT signing key container for Oficina Auth Lambdas. Value is synchronized by workflow."
  recovery_window_in_days = 7
}

