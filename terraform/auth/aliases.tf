resource "aws_lambda_alias" "auth_cpf_live" {
  name             = local.live_alias_name
  description      = "Live alias for oficina-auth-cpf."
  function_name    = aws_lambda_function.auth_cpf.function_name
  function_version = aws_lambda_function.auth_cpf.version
}

resource "aws_lambda_alias" "authorizer_live" {
  name             = local.live_alias_name
  description      = "Live alias for oficina-authorizer."
  function_name    = aws_lambda_function.authorizer.function_name
  function_version = aws_lambda_function.authorizer.version
}

