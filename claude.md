# OpenMFA - PIV Self-Enrollment Client

## Project Overview

Self-enrollment client for PIV smart cards using Aventura PIV 4.5 cards and OpenSC. Designed for air-gapped, isolated environments with simple standalone operation.

## Technical Stack

- **Smart Cards**: Aventura PIV 4.5
- **Interface**: OpenSC + PKCS#11
- **Application**: Python CLI tool
- **CA**: Local filesystem-based CA (OpenSSL)
- **Database**: SQLite (local file)

## Repository Structure

```
OpenMFA/
├── src/
│   ├── card.py              # OpenSC/PKCS#11 operations
│   ├── enrollment.py        # Enrollment workflow
│   ├── ca.py                # Simple file-based CA
│   └── cli.py               # Command-line interface
├── scripts/
│   ├── setup.sh             # Install OpenSC and dependencies
│   ├── init-ca.sh           # Initialize CA structure
│   └── reset-card.sh        # Reset/reinitialize card
├── config/
│   ├── opensc.conf          # OpenSC configuration
│   └── settings.yaml        # Application settings
├── ca/                      # CA directory (created at init)
│   ├── ca-cert.pem
│   ├── ca-key.pem
│   ├── issued/              # Issued certificates
│   └── index.txt            # Certificate index
├── data/
│   └── enrollments.db       # SQLite database
├── requirements.txt
├── README.md
└── claude.md
```

## Core Components

### 1. Card Operations (card.py)
- Detect card reader and PIV card
- Initialize PIV applet
- Generate RSA 2048 key pair on-card (slot 9A)
- Extract public key
- Write certificate to card

### 2. Enrollment Workflow (enrollment.py)
1. Detect card and reader
2. Set user PIN (6-8 digits)
3. Generate key pair on card
4. Create CSR with user DN
5. Sign certificate with local CA
6. Write certificate to card
7. Log enrollment to SQLite

### 3. Simple CA (ca.py)
- File-based CA using OpenSSL
- Issue certificates from CSR
- Track issued certificates in index
- No revocation (air-gapped environment)

### 4. CLI Interface (cli.py)
```bash
# Initialize CA
openmfa init-ca

# Enroll new card
openmfa enroll --cn "John Doe" --email "john@example.com"

# List enrolled cards
openmfa list

# Reset card
openmfa reset-card
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
