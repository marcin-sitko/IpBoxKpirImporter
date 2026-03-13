\
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
dotnet run --project "$root\SmokeTests\SmokeTests.csproj"
