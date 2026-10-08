param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string]$Name
)

$ErrorActionPreference = "Stop"

$project = "./src/Mype.Infrastructure"
$startupProject = "./src/Mype.Api"
$outputDirectory = "Persistence/Migrations"

Write-Host "Generating migration '$Name'..."

dotnet ef migrations add $Name `
    --project $project `
    --startup-project $startupProject `
    --output-dir $outputDirectory

if ($LASTEXITCODE -ne 0) {
    Write-Error "Migration '$Name' could not be generated."
    exit $LASTEXITCODE
}

Write-Host "Migration '$Name' generated successfully."