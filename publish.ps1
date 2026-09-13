param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = "$projectRoot\src\WindowMover\WindowMover.csproj"
$output = "$projectRoot\artifacts\publish\$Runtime"
$localDotnet = "$projectRoot\.dotnet\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
$env:DOTNET_CLI_HOME = "$projectRoot\.cli"
$env:NUGET_PACKAGES = "$projectRoot\.packages"
$env:APPDATA = "$projectRoot\.profile\AppData\Roaming"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null

& $dotnet publish $project `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $output

Write-Host "Julkaisu valmis: $output\WindowMover.exe"
