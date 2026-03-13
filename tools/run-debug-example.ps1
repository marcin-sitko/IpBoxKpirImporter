\
param(
    [string]$KpirFolder = "C:\Apps\KPIR",
    [string]$Template = "C:\Apps\Ewidencja IP BOX-2025- Marcin Sitko.xlsx",
    [string]$OutputFolder = "C:\Apps"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
dotnet run --project "$root\IpBoxKpirImporter.csproj" -- "$KpirFolder" "$Template" "$OutputFolder" --debug
