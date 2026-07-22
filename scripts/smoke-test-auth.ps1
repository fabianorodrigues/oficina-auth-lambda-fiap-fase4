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
    $base = $BaseUrl.TrimEnd('/')
    $response = Invoke-WebRequest -Method Post -Uri "$base/api/auth/cpf" -Body '{"cpf":"","password":""}' -ContentType "application/json" -SkipHttpErrorCheck
    if ($response.StatusCode -ne 400) { throw "Auth CPF invalido deveria retornar 400." }
    Write-Host "Smoke API concluido."
    return
}

if ([string]::IsNullOrWhiteSpace($Region)) { throw "AWS_REGION obrigatorio." }

function Write-Utf8NoBom {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Value)
    [IO.File]::WriteAllText($Path, $Value, [Text.UTF8Encoding]::new($false))
}

function Invoke-LambdaJson {
    param(
        [Parameter(Mandatory = $true)][string]$FunctionName,
        [Parameter(Mandatory = $true)][string]$Payload
    )

    $suffix = [Guid]::NewGuid().ToString('N')
    $payloadPath = Join-Path ([IO.Path]::GetTempPath()) "oficina-$FunctionName-$suffix.payload.json"
    $outputPath = Join-Path ([IO.Path]::GetTempPath()) "oficina-$FunctionName-$suffix.output.json"

    try {
        Write-Utf8NoBom -Path $payloadPath -Value $Payload
        $invokeRaw = aws lambda invoke --function-name "$FunctionName`:live" --payload "fileb://$payloadPath" --region $Region $outputPath 2>&1
        if ($LASTEXITCODE -ne 0) { throw "aws lambda invoke $FunctionName failed: $invokeRaw" }

        $invoke = ($invokeRaw | Out-String) | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace($invoke.FunctionError)) {
            $body = if (Test-Path -LiteralPath $outputPath) { Get-Content -LiteralPath $outputPath -Raw } else { "" }
            throw "Lambda $FunctionName retornou FunctionError=$($invoke.FunctionError). Payload: $body"
        }

        if (-not (Test-Path -LiteralPath $outputPath)) { throw "Lambda $FunctionName nao gerou arquivo de resposta." }
        $raw = Get-Content -LiteralPath $outputPath -Raw
        if ([string]::IsNullOrWhiteSpace($raw)) { throw "Lambda $FunctionName retornou resposta vazia." }
        return $raw | ConvertFrom-Json
    }
    finally {
        Remove-Item -LiteralPath $payloadPath -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $outputPath -Force -ErrorAction SilentlyContinue
    }
}

$cpfResponse = Invoke-LambdaJson -FunctionName "oficina-auth-cpf" -Payload '{"body":"{\"cpf\":\"00000000000\",\"password\":\"invalid-smoke-test\"}","headers":{}}'
if (($cpfResponse.statusCode -as [int]) -ne 400) {
    throw "Auth CPF invalido deveria retornar statusCode 400 pela Lambda; retornou '$($cpfResponse.statusCode)'."
}

$authorizerResponse = Invoke-LambdaJson -FunctionName "oficina-authorizer" -Payload '{"version":"2.0","type":"REQUEST","routeArn":"arn:aws:execute-api:us-east-1:123456789012:api/$default/GET/api/clientes","headers":{"authorization":"Bearer not-a-real-jwt"}}'
if ($authorizerResponse.isAuthorized -ne $false) {
    throw "Authorizer com token malformado deveria retornar isAuthorized=false."
}

Write-Host "Smoke Lambda validou auth-cpf 400 e authorizer deny sem imprimir tokens."
