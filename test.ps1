$ErrorActionPreference = "Stop"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$testRoot = Join-Path $PSScriptRoot "artifacts\tests"
New-Item -ItemType Directory -Force -Path $testRoot | Out-Null
$testExe = Join-Path $testRoot "RefreshPolicyTests.exe"
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot "src\QuotaOwl") -Filter *.cs | ForEach-Object FullName)
& $compiler /nologo /target:exe /platform:x64 /main:QuotaOwl.RefreshPolicyTests "/out:$testExe" /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll @sources (Join-Path $PSScriptRoot "tests\RefreshPolicyTests.cs")
if ($LASTEXITCODE -ne 0) { throw "Test compilation failed." }
& $testExe
if ($LASTEXITCODE -ne 0) { throw "Refresh policy tests failed." }
