#Requires -Version 5.1
param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$installerDir = $PSScriptRoot
$repo = Resolve-Path (Join-Path $installerDir "..\..")
$publish = Join-Path $installerDir "bin\Release\net8.0-windows\publish"

dotnet publish (Join-Path $installerDir "BatchParamUpdate.Installer.csproj") -c Release -o $publish

foreach ($year in 2025, 2026, 2027) {
    $yearProj = Join-Path $repo "src\BatchParamUpdate.Adapters.Revit.$year\BatchParamUpdate.Adapters.Revit.$year.csproj"
    $addinSrc = Join-Path $repo "src\BatchParamUpdate.Adapters.Revit.$year"
    $outDir = Join-Path $publish "addins\$year"
    $payload = Join-Path $outDir "BatchParamUpdate"
    New-Item -ItemType Directory -Force -Path $payload | Out-Null
    dotnet build $yearProj -c Release --nologo
    Copy-Item (Join-Path $addinSrc "*.addin") $outDir -Force
    $bin = Join-Path $addinSrc "bin\Release"
    if (Test-Path $bin) {
        Copy-Item (Join-Path $bin "*") $payload -Recurse -Force
    }
}

vpk pack -u BatchParamUpdate -v $Version -p $publish -e Installer.exe
