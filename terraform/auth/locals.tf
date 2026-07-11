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

  tags = {
    Project    = "oficina"
    Repository = "oficina-auth-lambda-fiap-fase4"
    ManagedBy  = "terraform"
  }
}

