Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'SoliloClock.sln'
$tests = Join-Path $root 'test/SoliloClock.Tests/SoliloClock.Tests.csproj'

Write-Host 'Building SoliloClock (Release)...'
dotnet build $solution -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Running tests...'
dotnet test $tests -c Release --no-build
exit $LASTEXITCODE
