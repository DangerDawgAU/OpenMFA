# OpenMFA - PIV Self-Enrollment Client

## Project Overview

Self-enrollment client for PIV smart cards using MyEID 4.5 PKI cards and OpenSC. Designed for air-gapped, isolated environments with simple standalone operation.

Supports both native PIV cards and PKCS#15 cards with PIV emulation.

## Technical Stack

- **Smart Cards**: MyEID 4.5 PKI Card (Aventra)
  - PKCS#15 compliant with PIV emulation
  - RSA keys up to 4096 bits
  - ECC keys (P-256, P-384, P-521)
  - 144KB EEPROM storage
- **Interface**: OpenSC (pkcs15-init, pkcs11-tool, piv-tool)
- **Application**: .NET 9.0 (Windows Forms GUI + CLI)
- **Platform**: Windows (with OpenSC installed)

## Repository Structure

```
OpenMFA/
├── src/
│   ├── OpenMFA.SmartCard/   # Core smart card operations library
│   │   ├── OpenSC/          # OpenSC command-line tool wrappers
│   │   │   ├── OpenScContext.cs
│   │   │   └── OpenScTools.cs
│   │   └── Piv/             # PIV card implementations
│   │       ├── IPivCard.cs
│   │       ├── PivCardOpenSc.cs
│   │       └── PivSlot.cs
│   ├── OpenMFA.GUI/         # Windows Forms GUI application
│   │   ├── MainForm.cs
│   │   ├── MainForm.Designer.cs
│   │   └── Program.cs
│   └── OpenMFA.CLI/         # Command-line interface
│       └── Program.cs
├── run.cmd                  # Windows launcher script
├── run.ps1                  # PowerShell launcher script
├── .gitignore
├── README.md
└── CLAUDE.md                # This file
```

## Core Components

### 1. OpenScTools (OpenScTools.cs)
Wrapper around OpenSC command-line tools with intelligent fallback:
- **pkcs15-init**: For PKCS#15 cards (MyEID) - tried first
- **pkcs11-tool**: For PIV cards (YubiKey) - fallback
- **opensc-tool**: For card detection
- Automatic tool path detection on Windows

### 2. PivCardOpenSc (PivCardOpenSc.cs)
Implements IPivCard interface for OpenSC-based operations:
- Certificate reading/writing
- Key pair generation (RSA 1024-4096, ECC P-256/P-384)
- Certificate deletion (PKCS#15 cards only)
- PIN verification
- Supports both PIV and PKCS#15 card types

### 3. GUI Application (MainForm.cs)
Windows Forms interface providing:
- Card detection and slot enumeration
- Certificate management (read, write, delete, list)
- On-card key pair generation
- Session logging with timestamps
- Visual feedback for all operations

### 4. CLI Application (Program.cs)
Command-line interface for automation:
```bash
# Detect cards and readers
dotnet run --project src/OpenMFA.CLI detect

# List certificates on card
dotnet run --project src/OpenMFA.CLI list

# Read certificate from slot 9A
dotnet run --project src/OpenMFA.CLI read 9A cert.der

# Write certificate to slot 9A
dotnet run --project src/OpenMFA.CLI write 9A cert.der

# Generate 2048-bit RSA key in slot 9A
dotnet run --project src/OpenMFA.CLI generate-key 9A RSA2048

# Delete certificate from slot 9A
dotnet run --project src/OpenMFA.CLI delete 9A
```

## Enrollment Process

```
1. Insert card → Detect reader/card
2. Enter PIN → Set user PIN (6-8 digits)
3. Generate key → On-card RSA 2048 generation
4. Create CSR → Build certificate request
5. Sign cert → Local CA signs certificate
6. Write to card → Store cert in slot 9A
7. Done → Card ready for use
```

## Dependencies

### System
- OpenSC (0.23.0+)
- OpenSSL (3.0+)
- pcscd (PC/SC daemon)
- USB smart card reader

### Python
```
pyscard>=2.0
cryptography>=41.0
PyYAML>=6.0
```

## Configuration

### settings.yaml
```yaml
ca:
  directory: ./ca
  validity_days: 365
  key_size: 2048

piv:
  default_pin: "123456"
  key_slot: "9A"
  algorithm: "RSA2048"

database:
  path: ./data/enrollments.db
```

## Setup Instructions

```bash
# 1. Install system dependencies
./scripts/setup.sh

# 2. Install Python dependencies
pip install -r requirements.txt

# 3. Initialize CA
python -m src.cli init-ca

# 4. Enroll a card (card must be inserted)
python -m src.cli enroll --cn "User Name"
```

## OpenSC Commands Reference

```bash
# List readers and cards
opensc-tool --list-readers

# Test PIV card
pkcs15-tool --dump

# Generate key on card
pkcs15-init --generate-key rsa/2048 --auth-id 01 --id 01 --label "PIV AUTH"

# List keys
pkcs15-tool --list-keys

# List certificates
pkcs15-tool --list-certificates

# Read certificate
pkcs15-tool --read-certificate 01
```

## Security Notes

1. **Air-gapped**: No network connectivity required
2. **Key generation**: Always on-card, never exported
3. **PIN handling**: User sets PIN, never stored
4. **CA security**: Protect ca-key.pem file
5. **Audit log**: All enrollments logged to SQLite

## Standards Compliance

- NIST SP 800-73-4 (PIV Card Interface)
- ISO 7816 (Smart card standards)
- PKCS#11 v2.40

---

**Environment**: Air-gapped, isolated systems
**Last Updated**: 2025-12-19
