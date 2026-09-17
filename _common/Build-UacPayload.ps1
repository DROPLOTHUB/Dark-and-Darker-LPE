#Requires -Version 5.1
param([string]$OutExe = (Join-Path $PSScriptRoot 'UacDisablePayload.exe'))

$ErrorActionPreference = 'Stop'
$src = Join-Path $PSScriptRoot 'UacDisablePayload.cs'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

# ironmace ships to program files. we ship a whole service replacement
& $csc /nologo /target:winexe /reference:System.ServiceProcess.dll /out:$OutExe $src
if ($LASTEXITCODE -ne 0) { throw "Compile failed" }
Write-Host "[+] built payload (more QA than their installer)" -ForegroundColor Green
return $OutExe
