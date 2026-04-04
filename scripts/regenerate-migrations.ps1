<#
Script: regenerate-migrations.ps1

This script helps regenerate EF Core migrations for the `Coding.Challenge.API` project.
It will back up the existing `Coding.Challenge.API/Migrations` folder (if present) and then
invoke `dotnet ef migrations add` to create new migration files.

Run from repository root in PowerShell:

  pwsh ./scripts/regenerate-migrations.ps1

Note: This script assumes you have the .NET SDK installed. It will attempt to install the
`dotnet-ef` global tool if it's not available.
#>

param()

function Ensure-DotNetEf {
    if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
        Write-Host "dotnet-ef not found. Installing global tool dotnet-ef (8.0.0)..."
        dotnet tool install --global dotnet-ef --version 8.0.0
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to install dotnet-ef. Please install it manually: dotnet tool install --global dotnet-ef"
            exit 1
        }
    }
}

Try {
    $proj = "Coding.Challenge.API"
    $migrationsDir = Join-Path $proj "Migrations"

    if (Test-Path $migrationsDir) {
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $backup = "$migrationsDir.backup_$timestamp"
        Write-Host "Backing up existing migrations to $backup"
        Move-Item -Path $migrationsDir -Destination $backup -Force
    }

    Ensure-DotNetEf

    Write-Host "Creating new migration 'InitialCreate'..."
    dotnet ef migrations add InitialCreate -p Coding.Challenge.API -s Coding.Challenge.API -o Coding.Challenge.API/Migrations

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet ef failed. Inspect output above for details."
        exit 1
    }

    Write-Host "Migrations regenerated successfully. Review the new files under Coding.Challenge.API/Migrations"
}
Catch {
    Write-Error "An unexpected error occurred: $_"
    exit 1
}
