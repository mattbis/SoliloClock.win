param(
    [string]$Runtime = 'win-x64',
    [switch]$FrameworkDependent
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/SoliloClock.App/SoliloClock.App.csproj'
$output = Join-Path $root "artifacts/$Runtime"

$arguments = @(
    'publish', $project,
    '-c', 'Release',
    '-r', $Runtime,
    '--self-contained', $(if ($FrameworkDependent) { 'false' } else { 'true' }),
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-o', $output
)

Write-Host "Publishing SoliloClock to $output ..."
dotnet @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Published: $(Join-Path $output 'SoliloClock.exe')"
