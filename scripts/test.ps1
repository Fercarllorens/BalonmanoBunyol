$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $projectRoot '.env'
if (Test-Path -LiteralPath $envFile -PathType Leaf) {
    $values = @{}
    foreach ($line in [System.IO.File]::ReadLines($envFile)) {
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
        $values[$name] = $value
    }

    if ($values.ContainsKey('POSTGRES_DB') -and $values.ContainsKey('POSTGRES_USER') -and $values.ContainsKey('POSTGRES_PASSWORD')) {
        $database = $values['POSTGRES_DB']
        if ($database -match '^[A-Za-z0-9_]+$') {
            $env:BALONMANO_TEST_DATABASE = "Host=localhost;Port=5432;Database=$database;Username=$($values['POSTGRES_USER']);Password=$($values['POSTGRES_PASSWORD'])"
        }
    }
}

Push-Location $projectRoot
try {
    & dotnet test 'BalonmanoBunyol.slnx'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & npm --prefix frontend test
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
