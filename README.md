# OpenMFA - PIV Smart Card Operations

Minimal viable implementation for PIV smart card operations in .NET 9 C#.

## Current Status

✅ **Implemented:**
- PC/SC wrapper with cross-platform support (Windows/Linux)
- PIV APDU command layer
- Smart card detection and connection
- CLI framework with commands:
  - `detect` - Find card readers and PIV cards
  - `info` - Show card information
  - `read` - Read certificate from slot
  - `write` - Write certificate to slot
  - `delete` - Delete certificate from slot
  - `generate-key` - Generate key pair on card
  - `list` - List all data objects

🚧 **In Progress:**
- Fixing C# syntax issues in CLI commands
- Testing with actual PIV cards

## Project Structure

```
OpenMFA/
├── src/
│   ├── OpenMFA.SmartCard/     # Core smart card library
│   │   ├── PcSc/              # PC/SC native wrapper
│   │   └── Piv/               # PIV card operations
│   └── OpenMFA.CLI/           # Command-line interface
├── ARCHITECTURE_SIMPLE.md     # Simplified architecture doc
└── README.md                  # This file
```

## Build

```bash
dotnet build
```

## Usage

```bash
# Detect card readers
dotnet run --project src/OpenMFA.CLI -- detect

# Generate RSA 2048 key in slot 9A
dotnet run --project src/OpenMFA.CLI -- generate-key 9A RSA2048

# Write certificate to card
dotnet run --project src/OpenMFA.CLI -- write 9A certificate.der

# Read certificate from card
dotnet run --project src/OpenMFA.CLI -- read 9A output.der

# Delete certificate
dotnet run --project src/OpenMFA.CLI -- delete 9A
```

## Requirements

**Linux:**
- pcscd (PC/SC daemon)
- OpenSC (optional, for additional tools)

```bash
sudo apt-get install pcscd pcsc-tools
sudo systemctl start pcscd
```

**Windows:**
- Smart Card service (built-in, starts automatically)

## PIV Slots

- **9A** - PIV Authentication (Windows/Linux login)
- **9C** - Digital Signature
- **9D** - Key Management (encryption)
- **9E** - Card Authentication

## Next Steps

1. Fix remaining compilation issues
2. Test with actual Aventura PIV 4.5 cards
3. Add PIN verification support
4. Add certificate parsing/display
5. Add CA and enrollment features (future)

## License

MIT
