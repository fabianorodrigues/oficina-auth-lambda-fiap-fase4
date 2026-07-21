resource "aws_cloudwatch_log_group" "auth_cpf" {
  name              = "/aws/lambda/${local.auth_cpf_function_name}"
  retention_in_days = var.log_retention_days
}

resource "aws_cloudwatch_log_group" "authorizer" {
  name              = "/aws/lambda/${local.authorizer_function_name}"
  retention_in_days = var.log_retention_days
}

resource "aws_lambda_function" "auth_cpf" {
  function_name    = local.auth_cpf_function_name
  description      = "Autenticacao por CPF e senha para Oficina."
  role             = local.auth_cpf_role_arn
  handler          = "Oficina.Auth.Cpf::Oficina.Auth.Cpf.Function::FunctionHandler"
  runtime          = "dotnet10"
  filename         = var.auth_cpf_zip_path
  source_code_hash = filebase64sha256(var.auth_cpf_zip_path)
  timeout          = 15
  memory_size      = 512
  publish          = true

  environment {
    variables = {
      JWT__ISSUER                = local.jwt_issuer
      JWT__AUDIENCE              = local.jwt_audience
      JWT__EXPIRATION_MINUTES    = local.jwt_expiration_minutes
      JWT__CLOCK_SKEW_SECONDS    = local.jwt_clock_skew_seconds
      JWT__SECRET_NAME           = local.jwt_secret_name
      DATABASE__SECRET_NAME      = local.database_secret_name
      SECRETS__CACHE_TTL_SECONDS = local.secrets_cache_ttl_seconds
    }
  }

  tracing_config {
    mode = "Active"
  }

  vpc_config {
    subnet_ids         = [data.aws_ssm_parameter.private_subnet_1.value, data.aws_ssm_parameter.private_subnet_2.value]
    security_group_ids = [aws_security_group.auth_cpf.id]
  }

  depends_on = [
    aws_cloudwatch_log_group.auth_cpf,
    aws_iam_role_policy_attachment.auth_cpf_basic,
    aws_iam_role_policy_attachment.auth_cpf_vpc,
    aws_iam_role_policy.auth_cpf_secrets
  ]
}

resource "aws_lambda_function" "authorizer" {
  function_name    = local.authorizer_function_name
  description      = "Lambda authorizer JWT para API Gateway HTTP API."
  role             = local.authorizer_role_arn
  handler          = "Oficina.Auth.Authorizer::Oficina.Auth.Authorizer.Function::FunctionHandler"
  runtime          = "dotnet10"
  filename         = var.authorizer_zip_path
  source_code_hash = filebase64sha256(var.authorizer_zip_path)
  timeout          = 5
  memory_size      = 256
  publish          = true

  environment {
    variables = {
      JWT__ISSUER                = local.jwt_issuer
      JWT__AUDIENCE              = local.jwt_audience
      JWT__EXPIRATION_MINUTES    = local.jwt_expiration_minutes
      JWT__CLOCK_SKEW_SECONDS    = local.jwt_clock_skew_seconds
      JWT__SECRET_NAME           = local.jwt_secret_name
      SECRETS__CACHE_TTL_SECONDS = local.secrets_cache_ttl_seconds
    }
  }

  tracing_config {
    mode = "Active"
  }

  depends_on = [
    aws_cloudwatch_log_group.authorizer,
    aws_iam_role_policy_attachment.authorizer_basic,
    aws_iam_role_policy.authorizer_secrets
  ]
}
