data "aws_iam_policy_document" "lambda_assume_role" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "auth_cpf" {
  count = local.create_auth_cpf_role ? 1 : 0

  name               = "oficina-auth-cpf-lambda"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume_role.json
}

resource "aws_iam_role" "authorizer" {
  count = local.create_authorizer_role ? 1 : 0

  name               = "oficina-authorizer-lambda"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume_role.json
}

resource "aws_iam_role_policy_attachment" "auth_cpf_basic" {
  count = local.create_auth_cpf_role ? 1 : 0

  role       = aws_iam_role.auth_cpf[0].name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy_attachment" "auth_cpf_vpc" {
  count = local.create_auth_cpf_role ? 1 : 0

  role       = aws_iam_role.auth_cpf[0].name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

resource "aws_iam_role_policy_attachment" "authorizer_basic" {
  count = local.create_authorizer_role ? 1 : 0

  role       = aws_iam_role.authorizer[0].name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

data "aws_iam_policy_document" "auth_cpf_secrets" {
  statement {
    actions = ["secretsmanager:GetSecretValue"]
    resources = [
      aws_secretsmanager_secret.jwt.arn,
      data.aws_secretsmanager_secret.database.arn
    ]
  }
}

data "aws_iam_policy_document" "authorizer_secrets" {
  statement {
    actions   = ["secretsmanager:GetSecretValue"]
    resources = [aws_secretsmanager_secret.jwt.arn]
  }
}

resource "aws_iam_role_policy" "auth_cpf_secrets" {
  count = local.create_auth_cpf_role ? 1 : 0

  name   = "oficina-auth-cpf-secrets"
  role   = aws_iam_role.auth_cpf[0].id
  policy = data.aws_iam_policy_document.auth_cpf_secrets.json
}

resource "aws_iam_role_policy" "authorizer_secrets" {
  count = local.create_authorizer_role ? 1 : 0

  name   = "oficina-authorizer-secrets"
  role   = aws_iam_role.authorizer[0].id
  policy = data.aws_iam_policy_document.authorizer_secrets.json
}

