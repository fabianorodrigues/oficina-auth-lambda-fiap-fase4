param(
    [switch]$DryRun,
    [string]$SecretName = "/oficina/auth/jwt",
    [string]$Region = $env:AWS_REGION
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-JwtKey([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { throw "JWT_SIGNING_KEY ausente." }
    if ($Value -match "[`r`n`0]") { throw "JWT_SIGNING_KEY contem caractere proibido." }
    if ($Value -match "(?i)placeholder|change-me|default") { throw "JWT_SIGNING_KEY parece placeholder." }
    if ([Text.Encoding]::UTF8.GetByteCount($Value) -lt 32) { throw "JWT_SIGNING_KEY deve possuir ao menos 32 bytes efetivos." }
}

$key = $env:JWT_SIGNING_KEY
Assert-JwtKey $key
$payloadObject = @{ SigningKey = $key }
$payload = $payloadObject | ConvertTo-Json -Compress
$sha256 = [Security.Cryptography.SHA256]::Create()
try {
    $hashBytes = $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($payload))
    $clientToken = -join ($hashBytes | ForEach-Object { $_.ToString("x2") })
}
finally {
    $sha256.Dispose()
}

if ($DryRun) {
    Write-Host "DryRun: uma nova versao seria criada para $SecretName. Payload nao impresso."
    return
}

if ([string]::IsNullOrWhiteSpace($Region)) { throw "AWS_REGION obrigatorio." }
aws secretsmanager describe-secret --secret-id $SecretName --region $Region | Out-Null
$temp = Join-Path ([IO.Path]::GetTempPath()) "oficina-auth-jwt-$([Guid]::NewGuid().ToString('N')).json"
try {
    Set-Content -LiteralPath $temp -Value $payload -NoNewline
    $result = aws secretsmanager put-secret-value --secret-id $SecretName --secret-string "file://$temp" --client-request-token $clientToken --region $Region | ConvertFrom-Json
    Write-Host "SecretName=$($result.Name) Arn=$($result.ARN) VersionId=$($result.VersionId)"
}
finally {
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Force }
}
