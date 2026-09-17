# POCs

Local security proof-of-concept research. For authorized testing on systems you own or have permission to test.

## Index

| POC | Target | Summary |
|-----|--------|---------|
| [01-IRONMACE-TavernWorker](01-IRONMACE-TavernWorker/) | IRONMACE Tavern (Steam) | Writable `TavernWorker.exe` + SYSTEM service → LPE, UAC tampering demo |

## Layout

```
_common/                  Shared scripts and payload source
01-IRONMACE-TavernWorker/ First POC — TavernWorker service abuse
```

Each POC folder has its own README with run/restore steps.

## Requirements

- Windows 10/11 x64
- PowerShell 5.1+
- .NET Framework 4.x (for compiling the payload via `Build-UacPayload.ps1`)

## Disclaimer

These are research tools. Only run against machines you control. Each PoC includes restore steps where applicable.
