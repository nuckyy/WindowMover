$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$localDotnet = "$projectRoot\.dotnet\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
$env:DOTNET_CLI_HOME = "$projectRoot\.cli"
$env:NUGET_PACKAGES = "$projectRoot\.packages"
$env:APPDATA = "$projectRoot\.profile\AppData\Roaming"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$nugetConfig = "$projectRoot\NuGet.Config"

New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null

& $dotnet restore "$projectRoot\WindowMover.slnx" --configfile $nugetConfig
& $dotnet build "$projectRoot\WindowMover.slnx" --configuration Release --no-restore
& $dotnet run --project "$projectRoot\tests\WindowMover.Tests\WindowMover.Tests.csproj" --configuration Release --no-build
