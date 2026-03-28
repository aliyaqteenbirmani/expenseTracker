param(
    [string]$Configuration = "Release",
    [string]$Runtime = "linux-x64",
    [string]$Output = ".\artifacts\publish\linux-x64"
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "ExpenseTrackingSystem\SpendwiseSystem.API.csproj"
$outputPath = Join-Path $PSScriptRoot $Output

if (Test-Path $outputPath) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    /p:UseAppHost=false `
    -o $outputPath

Write-Host ""
Write-Host "Linux publish output:"
Write-Host $outputPath


