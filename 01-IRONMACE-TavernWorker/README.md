# IRONMACE TavernWorker LPE

Releasing this PoC publicly.

I reported it by email and in their Discord — no response. Did the responsible thing first; seems like that only matters once it's out in the open. Small report, big company, whatever.

**What it is:** writable `TavernWorker.exe` + `TavernWorker_1_1` service running as SYSTEM. Local user → full SYSTEM. PoC also demos UAC tampering to show impact.

## Run

```powershell
cd 01-IRONMACE-TavernWorker
.\exploit.ps1
```

Proof log: `C:\ProgramData\poc_uac_proof.txt`

## Restore

```powershell
.\exploit.ps1 -Restore
```

Reboot if UAC still acts weird.

## Fix (for IRONMACE)

Remove `Everyone:(F)` from the install dir under `Program Files\IRONMACE\`. That kills the chain.
