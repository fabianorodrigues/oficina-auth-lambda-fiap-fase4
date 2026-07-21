locals {
  project = "oficina"

  auth_cpf_function_name    = "oficina-auth-cpf"
  authorizer_function_name  = "oficina-authorizer"
  live_alias_name           = "live"
  jwt_secret_name           = "/oficina/auth/jwt"
  database_secret_name      = "/oficina/auth/database"
  jwt_issuer                = "oficina"
  jwt_audience              = "oficina-api"
  jwt_expiration_minutes    = "60"
  jwt_clock_skew_seconds    = "60"
  secrets_cache_ttl_seconds = "300"

  external_auth_cpf_role_arn   = trimspace(var.auth_cpf_role_arn)
  external_authorizer_role_arn = trimspace(var.authorizer_role_arn)
  create_auth_cpf_role         = local.external_auth_cpf_role_arn == ""
  create_authorizer_role       = local.external_authorizer_role_arn == ""
  auth_cpf_role_arn            = local.create_auth_cpf_role ? aws_iam_role.auth_cpf[0].arn : local.external_auth_cpf_role_arn
  authorizer_role_arn          = local.create_authorizer_role ? aws_iam_role.authorizer[0].arn : local.external_authorizer_role_arn

  tags = {
    Project    = "oficina"
    Repository = "oficina-auth-lambda-fiap-fase4"
    ManagedBy  = "terraform"
  }
}

