#Requires -Version 5.1
param(
    [string]$TargetExe = 'C:\Program Files\IRONMACE\Tavern\Steam\TavernApp_1_1\TavernWorker.exe',
    [string]$Backup = "$TargetExe.poc_backup"
)

$ErrorActionPreference = 'Stop'
$key = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'
$backupFile = 'C:\ProgramData\poc_uac_registry_backup.txt'

# cleaning up after ironmace's oopsie
Write-Host 'putting uac back (ironmace wont)' -ForegroundColor Cyan

if (Test-Path -LiteralPath $backupFile) {
    Get-Content -LiteralPath $backupFile | ForEach-Object {
        if ($_ -match '^#' -or $_ -match '^\s*$') { return }
        if ($_ -match '^([^=]+)=(.+)$') {
            $name = $Matches[1].Trim()
            $val = [int]$Matches[2].Trim()
            Set-ItemProperty -LiteralPath $key -Name $name -Value $val -Type DWord -Force
            Write-Host "[+] Restored $name = $val"
        }
    }
} else {
    Write-Host '[*] no backup, using defaults' -ForegroundColor Yellow
    Set-ItemProperty -LiteralPath $key -Name EnableLUA -Value 1 -Type DWord -Force
    Set-ItemProperty -LiteralPath $key -Name ConsentPromptBehaviorAdmin -Value 5 -Type DWord -Force
    Set-ItemProperty -LiteralPath $key -Name ConsentPromptBehaviorUser -Value 3 -Type DWord -Force
    Set-ItemProperty -LiteralPath $key -Name PromptOnSecureDesktop -Value 1 -Type DWord -Force
}

# delete the scheduled task we had to make because nobody fixed the acl
schtasks.exe /delete /tn "POC_Tavern_UAC_Proof" /f 2>$null | Out-Null

if (Test-Path -LiteralPath $Backup) {
    $svc = 'TavernWorker_1_1'
    $s = Get-Service -Name $svc -ErrorAction SilentlyContinue
    if ($s -and $s.Status -eq 'Running') { Stop-Service -Name $svc -Force; Start-Sleep 2 }
    Copy-Item -LiteralPath $Backup -Destination $TargetExe -Force
    Write-Host "[+] restored their broken exe too, we're nice like that" -ForegroundColor Green
}

Write-Host '[!] reboot if uac still cooked' -ForegroundColor Yellow
