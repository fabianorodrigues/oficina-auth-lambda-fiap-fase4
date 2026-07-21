param(
    [string]$Region = $env:AWS_REGION
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Region)) { throw "AWS_REGION obrigatorio." }

$authCpfName = aws ssm get-parameter --name /oficina/auth/cpf/function-name --region $Region | ConvertFrom-Json
$authorizerName = aws ssm get-parameter --name /oficina/auth/authorizer/function-name --region $Region | ConvertFrom-Json

foreach ($name in @($authCpfName.Parameter.Value, $authorizerName.Parameter.Value)) {
    aws lambda get-function --function-name $name --region $Region | Out-Null
    aws lambda list-versions-by-function --function-name $name --region $Region | Out-Null
    aws lambda get-alias --function-name $name --name live --region $Region | Out-Null
}

aws secretsmanager describe-secret --secret-id /oficina/auth/jwt --region $Region | Out-Null
aws secretsmanager list-secret-version-ids --secret-id /oficina/auth/jwt --region $Region | Out-Null
aws secretsmanager describe-secret --secret-id /oficina/auth/database --region $Region | Out-Null
Write-Host "Validacao read-only concluida."

