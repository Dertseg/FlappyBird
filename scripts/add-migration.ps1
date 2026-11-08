param([Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z][A-Za-z0-9_]*$')][string]$Name)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$checksProject = Join-Path $repoRoot 'tests/FlappyBird.Checks'
$migrationsDirectory = Join-Path $repoRoot 'src/FlappyBird/Data/Migrations'
Push-Location -LiteralPath $repoRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Tool restore failed' }
    dotnet ef migrations add $Name --project $checksProject --output-dir $migrationsDirectory --namespace FlappyBird.Data.Migrations
    if ($LASTEXITCODE -ne 0) { throw 'Migration generation failed' }
    # EF scaffolds a linked snapshot relative to the tools project. Keep one canonical copy.
    $generatedSnapshot = Join-Path $checksProject 'FlappyBird/Data/Migrations/GameDbContextModelSnapshot.cs'
    if (Test-Path -LiteralPath $generatedSnapshot) {
        Move-Item -LiteralPath $generatedSnapshot -Destination (Join-Path $migrationsDirectory 'GameDbContextModelSnapshot.cs') -Force
    }
} finally { Pop-Location }
