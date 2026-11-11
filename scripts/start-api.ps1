param(
    [string]$EnvFile = (Join-Path (Split-Path -Parent $PSScriptRoot) '.env'),
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $EnvFile -PathType Leaf)) {
    throw "No se encuentra el archivo .env. Crea uno en la raíz del proyecto usando .env.example como plantilla."
}

$variables = @{}
foreach ($line in [System.IO.File]::ReadLines((Resolve-Path -LiteralPath $EnvFile))) {
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
    if ($trimmed.StartsWith('export ')) { $trimmed = $trimmed.Substring(7).Trim() }

    if ($trimmed -notmatch '^([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') { continue }

    $name = $Matches[1]
    $value = $Matches[2].Trim()
    if ($value.Length -ge 2) {
        $first = $value[0]
        $last = $value[$value.Length - 1]
        if (($first -eq '"' -and $last -eq '"') -or ($first -eq "'" -and $last -eq "'")) {
            $value = $value.Substring(1, $value.Length - 2)
        }
    }

    $variables[$name] = $value
}

$requiredVariables = @(
    'POSTGRES_DB',
    'POSTGRES_USER',
    'POSTGRES_PASSWORD',
    'BootstrapAdmin__Email',
    'BootstrapAdmin__Password'
)
$missingVariables = @($requiredVariables | Where-Object { -not $variables.ContainsKey($_) -or [string]::IsNullOrWhiteSpace($variables[$_]) })
if ($missingVariables.Count -gt 0) {
    throw "Faltan variables requeridas en .env: $($missingVariables -join ', ')."
}

foreach ($name in $variables.Keys) {
    [Environment]::SetEnvironmentVariable($name, $variables[$name], 'Process')
}

[Environment]::SetEnvironmentVariable(
    'ConnectionStrings__ClubDatabase',
    "Host=localhost;Port=5432;Database=$($variables['POSTGRES_DB']);Username=$($variables['POSTGRES_USER']);Password=$($variables['POSTGRES_PASSWORD'])",
    'Process'
)
[Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Development', 'Process')

if ($ValidateOnly) {
    Write-Host 'El archivo de entorno contiene las variables requeridas.'
    return
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $projectRoot 'backend/src/BalonmanoBunol.Api/BalonmanoBunol.Api.csproj'
if (-not (Test-Path -LiteralPath $apiProject -PathType Leaf)) {
    throw 'No se encuentra el proyecto API esperado.'
}

Write-Host 'Iniciando la API con configuración local cargada desde .env.'
& dotnet run --project $apiProject
exit $LASTEXITCODE
