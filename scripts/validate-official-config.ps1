Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$path = Join-Path $root "config/official.json"
$config = Get-Content -Raw $path | ConvertFrom-Json
$raw = Get-Content -Raw $path

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

Assert-True ($config.version -eq 1) "version invalida"
Assert-True ($config.functions.authCpf.name -eq "oficina-auth-cpf") "authCpf name invalido"
Assert-True ($config.functions.authorizer.name -eq "oficina-authorizer") "authorizer name invalido"
Assert-True ($config.functions.authCpf.alias -eq "live") "authCpf alias invalido"
Assert-True ($config.functions.authorizer.alias -eq "live") "authorizer alias invalido"
Assert-True ($config.functions.authCpf.inVpc -eq $true) "authCpf deve estar em VPC"
Assert-True ($config.functions.authorizer.inVpc -eq $false) "authorizer deve ficar fora da VPC"
Assert-True ($config.jwt.algorithm -eq "HS256") "algoritmo JWT invalido"
Assert-True ($config.jwt.issuer -eq "oficina") "issuer invalido"
Assert-True ($config.jwt.audience -eq "oficina-api") "audience invalida"
Assert-True ($config.jwt.expirationMinutes -eq 60) "expiracao invalida"
Assert-True ($config.jwt.clockSkewSeconds -eq 60) "clock skew invalido"
Assert-True ($config.jwt.secretName -eq "/oficina/auth/jwt") "secret JWT invalido"
Assert-True ($config.database.secretName -eq "/oficina/auth/database") "secret database invalido"

$config.ssm.PSObject.Properties | ForEach-Object {
    Assert-True ($_.Value -like "/oficina/*") "SSM path invalido: $($_.Name)"
}

$forbidden = @(
    "arn:aws:",
    "\b\d{12}\b",
    "ConnectionString",
    "Password\s*=",
    "SigningKey",
    "JWT_SIGNING_KEY",
    "AWS_ACCESS_KEY_ID",
    "AWS_SECRET_ACCESS_KEY",
    "AWS_SESSION_TOKEN",
    "Fase3",
    "fase-3",
    "/dev/",
    "-dev",
    "-hml",
    "-prod"
)

foreach ($pattern in $forbidden) {
    if ($raw -match $pattern) { throw "official.json contem padrao proibido: $pattern" }
}

Write-Host "official.json valido."

