param([switch]$Test)
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'Build-Icons.ps1')
& (Join-Path $PSScriptRoot 'Prepare-Web.ps1')
dotnet build (Join-Path $PSScriptRoot 'PetbirdsLite.csproj') -c Release
if($LASTEXITCODE -ne 0){throw '桌面版编译失败'}
if($Test) {
    dotnet run --project (Join-Path $PSScriptRoot 'tests/StateTests.csproj') -c Release
    if($LASTEXITCODE -ne 0){throw '状态测试失败'}
}
