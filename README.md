# Batch Parameter Update — Revit add-in

Revit add-in that batch-updates a writable **text** parameter across a selection, using Instance (Dialog Box 1) or Type (Dialog Box 2) binding discovered from the same scope.

**Author:** Juan Pablo Manrique

## Supported Revit versions

This add-in **supports only Autodesk Revit 2025, 2026, and 2027**.

- The installer detects those three years and never offers Install/Update/Uninstall for any other Revit version.
- No other Revit year is targeted, tested, or claimed as compatible (FR-045/FR-046, SC-009).

## Build

```powershell
dotnet test tests/BatchParamUpdate.Tests.Unit/BatchParamUpdate.Tests.Unit.csproj
dotnet build BatchParamUpdate.sln -c Release
```

Year adapters need that year's `RevitAPI.dll` / `RevitAPIUI.dll` under `%ProgramW6432%\Autodesk\Revit {year}`.

## Installer

From `src/BatchParamUpdate.Installer/`:

```powershell
.\pack.ps1 -Version 1.0.0
```

That publishes the WPF installer host (`Installer.exe`), copies each year add-in payload, and runs `vpk pack -u BatchParamUpdate -e Installer.exe`. Running `Setup.exe` / `Installer.exe` lists detected 2025/2026/2027 installs and copies the matching assembly plus `.addin` (`Application` = `App`).

Logs: `%TEMP%\juanManriqueHexagon\LOGS\revit-{runId}-{documentName}.txt`  
Metrics: `%TEMP%\juanManriqueHexagon\TRACKER\revit-{runId}-{documentName}.ndjson`

Manual host validation: `docs/specs/001-batch-parameter-update/quickstart.md`.
