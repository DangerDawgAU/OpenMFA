# OpenMFA - PIV Smart Card Operations

Minimal PIV smart card operations using **OpenSC** for .NET 9 C#.

## Overview

OpenMFA is a command-line tool for managing PIV (Personal Identity Verification) smart cards. It uses OpenSC's `pkcs11-tool` and `opensc-tool` for all card operations, providing a simple .NET interface for:

- Reading/writing certificates to/from PIV cards
- Generating RSA and ECC key pairs on-card
- Managing PIV authentication slots for Windows/Linux login

## Requirements

### System Requirements

**OpenSC** must be installed on your system:

**Linux:**
```bash
sudo apt-get install opensc pcscd
sudo systemctl start pcscd
```

**Windows:**
- Download from: https://github.com/OpenSC/OpenSC/releases
- Install to default location

**macOS:**
```bash
brew install opensc
```

### .NET Requirements
- .NET 9.0 SDK

## Project Structure

```
OpenMFA/
├── src/
│   ├── OpenMFA.SmartCard/     # Smart card library
│   │   ├── OpenSC/            # OpenSC CLI wrappers
│   │   └── Piv/               # PIV abstractions
│   └── OpenMFA.CLI/           # Command-line interface
├── ARCHITECTURE_SIMPLE.md     # Architecture document
└── README.md                  # This file
```

## Build

```bash
dotnet build
```

## Usage

### Detect Cards

```bash
dotnet run --project src/OpenMFA.CLI -- detect
```

### Generate Key Pair

```bash
# Generate RSA 2048-bit key in slot 9A (PIV Authentication)
dotnet run --project src/OpenMFA.CLI -- generate-key 9A RSA2048

# Generate ECC P-256 key
dotnet run --project src/OpenMFA.CLI -- generate-key 9A ECCP256
```

### Write Certificate

```bash
dotnet run --project src/OpenMFA.CLI -- write 9A certificate.der
```

### Read Certificate

```bash
# Read and display (hex dump)
dotnet run --project src/OpenMFA.CLI -- read 9A

# Read and save to file
dotnet run --project src/OpenMFA.CLI -- read 9A output.der
```

### List Certificates

```bash
dotnet run --project src/OpenMFA.CLI -- list
```

### Delete Certificate

```bash
dotnet run --project src/OpenMFA.CLI -- delete 9A
```

## PIV Slots

| Slot | Name | Purpose |
|------|------|---------|
| **9A** | PIV Authentication | Windows/Linux smart card login |
| **9C** | Digital Signature | Email/document signing |
| **9D** | Key Management | Encryption/decryption |
| **9E** | Card Authentication | Physical access control |

## Supported Algorithms

- **RSA1024** - RSA 1024-bit (legacy)
- **RSA2048** - RSA 2048-bit (recommended)
- **ECCP256** - ECC P-256 (NIST curve)
- **ECCP384** - ECC P-384 (NIST curve)

## How It Works

OpenMFA wraps OpenSC command-line tools:

1. **Card Detection**: Uses `opensc-tool --list-readers` and `pkcs11-tool --list-slots`
2. **Read Operations**: Uses `pkcs11-tool --read-object --type cert --id <id>`
3. **Write Operations**: Uses `pkcs11-tool --write-object --type cert --id <id>`
4. **Key Generation**: Uses `pkcs11-tool --keypairgen --key-type RSA:2048 --id <id>`
5. **Delete Operations**: Uses `pkcs11-tool --delete-object --type cert --id <id>`

This approach ensures:
- ✅ Maximum compatibility with PIV cards
- ✅ Leverages OpenSC's extensive testing and support
- ✅ No complex P/Invoke or PKCS#11 bindings needed
- ✅ Cross-platform (Windows, Linux, macOS)

## Troubleshooting

### "No card readers found"

1. Check OpenSC is installed:
   ```bash
   opensc-tool --version
   pkcs11-tool --version
   ```

2. Check PC/SC daemon is running:
   ```bash
   # Linux
   sudo systemctl status pcscd

   # Start if needed
   sudo systemctl start pcscd
   ```

3. Verify card reader is detected:
   ```bash
   opensc-tool --list-readers
   ```

### "OpenSC tool failed"

- Ensure card is inserted properly
- Try removing and reinserting the card
- Check card is PIV-compatible (e.g., Yubikey, Aventura PIV 4.5)

## Next Steps

After successfully reading/writing to your PIV card:

1. **Windows Login**: Configure Windows for smart card authentication
2. **Linux Login**: Configure PAM for smart card authentication
3. **SSH**: Use PIV card for SSH public key authentication
4. **Email Signing**: Configure email client (Outlook, Thunderbird) for S/MIME

## Standards Compliance

- NIST SP 800-73-4 (PIV Card Interface)
- ISO 7816 (Smart Card Standards)
- PKCS#11 v2.40

## License

MIT
