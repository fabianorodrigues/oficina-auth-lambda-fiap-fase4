param(
    [ValidateSet("Lambda", "Api")]
    [string]$Mode = "Lambda",
    [string]$BaseUrl,
    [string]$Region = $env:AWS_REGION
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($Mode -eq "Api") {
    if ([string]::IsNullOrWhiteSpace($BaseUrl)) { throw "BaseUrl obrigatorio no modo Api." }
    $response = Invoke-WebRequest -Method Post -Uri "$BaseUrl/auth/cpf" -Body '{"cpf":"","password":""}' -ContentType "application/json" -SkipHttpErrorCheck
    if ($response.StatusCode -ne 400) { throw "Auth CPF invalido deveria retornar 400." }
    Write-Host "Smoke API concluido."
    return
}

if ([string]::IsNullOrWhiteSpace($Region)) { throw "AWS_REGION obrigatorio." }
$invalidPayload = Join-Path ([IO.Path]::GetTempPath()) "oficina-auth-invalid-$([Guid]::NewGuid().ToString('N')).json"
try {
    Set-Content -LiteralPath $invalidPayload -Value '{"body":"{\"cpf\":\"\",\"password\":\"\"}","headers":{}}' -NoNewline
    aws lambda invoke --function-name "oficina-auth-cpf:live" --payload "fileb://$invalidPayload" --region $Region "$invalidPayload.out" | Out-Null
    Write-Host "Smoke Lambda executado sem imprimir tokens."
}
finally {
    Remove-Item -LiteralPath $invalidPayload -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath "$invalidPayload.out" -Force -ErrorAction SilentlyContinue
}

