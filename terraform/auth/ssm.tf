resource "aws_ssm_parameter" "auth_cpf_alias_arn" {
  name  = "/oficina/auth/cpf/alias-arn"
  type  = "String"
  value = aws_lambda_alias.auth_cpf_live.arn
}

resource "aws_ssm_parameter" "auth_cpf_function_name" {
  name  = "/oficina/auth/cpf/function-name"
  type  = "String"
  value = aws_lambda_function.auth_cpf.function_name
}

resource "aws_ssm_parameter" "authorizer_alias_arn" {
  name  = "/oficina/auth/authorizer/alias-arn"
  type  = "String"
  value = aws_lambda_alias.authorizer_live.arn
}

resource "aws_ssm_parameter" "authorizer_function_name" {
  name  = "/oficina/auth/authorizer/function-name"
  type  = "String"
  value = aws_lambda_function.authorizer.function_name
}

