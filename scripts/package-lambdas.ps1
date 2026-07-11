param(
    [switch]$RunTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "Oficina.Auth.sln"
$output = Join-Path $root "artifacts/lambda"
New-Item -ItemType Directory -Force -Path $output | Out-Null

dotnet tool restore
dotnet restore $solution
dotnet build $solution -c Release --no-restore
if ($RunTests) {
    dotnet test $solution -c Release --no-build
}

$projects = @(
    @{ Name = "oficina-auth-cpf"; Project = "src/Oficina.Auth.Cpf/Oficina.Auth.Cpf.csproj"; Handler = "Oficina.Auth.Cpf::Oficina.Auth.Cpf.Function::FunctionHandler" },
    @{ Name = "oficina-authorizer"; Project = "src/Oficina.Auth.Authorizer/Oficina.Auth.Authorizer.csproj"; Handler = "Oficina.Auth.Authorizer::Oficina.Auth.Authorizer.Function::FunctionHandler" }
)

foreach ($item in $projects) {
    $publish = Join-Path $root "artifacts/publish/$($item.Name)"
    $zip = Join-Path $output "$($item.Name).zip"
    if (Test-Path $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
    if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }
    dotnet publish (Join-Path $root $item.Project) -c Release -o $publish --no-build
    Get-ChildItem -Path $publish -Recurse -File -Filter "*.pdb" | Remove-Item -Force

    $blocked = Get-ChildItem -Path $publish -Recurse -File | Where-Object {
        $_.FullName -match "\\tests\\" -or
        $_.Name -like "appsettings.Development*"
    }
    if ($blocked) { throw "Publicacao contem arquivo bloqueado: $($blocked[0].FullName)" }

    Compress-Archive -Path (Join-Path $publish "*") -DestinationPath $zip -Force
    $hash = (Get-FileHash -Algorithm SHA256 $zip).Hash
    Write-Host "$($item.Name) packaged SHA256=$hash Handler=$($item.Handler)"
}
