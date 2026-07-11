output "auth_cpf_alias_arn" {
  value       = aws_lambda_alias.auth_cpf_live.arn
  description = "Auth CPF live alias ARN."
}

output "authorizer_alias_arn" {
  value       = aws_lambda_alias.authorizer_live.arn
  description = "Authorizer live alias ARN."
}

output "jwt_secret_name" {
  value       = aws_secretsmanager_secret.jwt.name
  description = "JWT secret container name."
}

