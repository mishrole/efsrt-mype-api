param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string]$Name
)

$ErrorActionPreference = "Stop"

$project = ".\src\Mype.Infrastructure"
$startupProject = ".\src\Mype.Api"
$addMigrationScript = ".\add-migration.ps1"

if (-not (Test-Path $addMigrationScript)) {
    Write-Error "No se encontró el script '$addMigrationScript'."
    exit 1
}

Write-Host "Removing the latest migration..."

dotnet ef migrations remove `
    --force `
    --project $project `
    --startup-project $startupProject

if ($LASTEXITCODE -ne 0) {
    Write-Error "The latest migration could not be removed."
    exit $LASTEXITCODE
}

Write-Host "The latest migration was removed successfully."
Write-Host "Generating migration '$Name'..."

powershell `
    -ExecutionPolicy Bypass `
    -File $addMigrationScript `
    $Name

if ($LASTEXITCODE -ne 0) {
    Write-Error "Migration '$Name' could not be regenerated."
    exit $LASTEXITCODE
}

Write-Host "Migration '$Name' regenerated successfully."