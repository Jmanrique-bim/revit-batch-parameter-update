#Requires -Version 5.1
param(
    [string]$Version = "1.0.1"
)

$ErrorActionPreference = "Stop"
$installerDir = $PSScriptRoot
$repo = Resolve-Path (Join-Path $installerDir "..\..")
$publish = Join-Path $installerDir "bin\Release\net8.0-windows\publish"

function Assert-NativeExit {
    param([Parameter(Mandatory = $true)][string]$Command)
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE."
    }
}

dotnet publish (Join-Path $installerDir "BatchParamUpdate.Installer.csproj") -c Release -o $publish
Assert-NativeExit "dotnet publish (Installer)"

foreach ($year in 2025, 2026) {
    $yearProj = Join-Path $repo "src\BatchParamUpdate.Adapters.Revit.$year\BatchParamUpdate.Adapters.Revit.$year.csproj"
    $addinSrc = Join-Path $repo "src\BatchParamUpdate.Adapters.Revit.$year"
    $outDir = Join-Path $publish "addins\$year"
    $payload = Join-Path $outDir "BatchParamUpdate"
    New-Item -ItemType Directory -Force -Path $payload | Out-Null
    dotnet build $yearProj -c Release --nologo
    Assert-NativeExit "dotnet build (Revit $year)"
    Copy-Item (Join-Path $addinSrc "*.addin") $outDir -Force
    $bin = Join-Path $addinSrc "bin\Release"
    if (Test-Path $bin) {
        Copy-Item (Join-Path $bin "*") $payload -Recurse -Force
    }
}

vpk pack -u BatchParamUpdate -v $Version -p $publish -e Installer.exe
Assert-NativeExit "vpk pack"
