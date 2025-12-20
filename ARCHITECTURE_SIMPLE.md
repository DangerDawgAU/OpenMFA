# OpenMFA - Minimal Smart Card Operations

## Overview

Minimal viable implementation for PIV smart card operations. Focus: Read, Write, Delete data from PIV cards for Windows/Linux authentication.

## Scope

**IN SCOPE:**
- Detect smart card readers
- Connect to PIV cards
- Read data from card slots
- Write data to card slots
- Delete data from card slots
- Issue basic PIV commands (SELECT, GET DATA, PUT DATA, GENERATE KEY PAIR)

**OUT OF SCOPE (for now):**
- Certificate Authority
- Certificate signing
- Enrollment workflows
- Database/logging
- User management

## Solution Structure

```
OpenMFA.sln
├── src/
│   ├── OpenMFA.CLI/              # Command-line tool
│   │   ├── Program.cs
│   │   ├── Commands/
│   │   │   ├── DetectCommand.cs  # Detect readers/cards
│   │   │   ├── ReadCommand.cs    # Read from card
│   │   │   ├── WriteCommand.cs   # Write to card
│   │   │   ├── DeleteCommand.cs  # Delete from card
│   │   │   └── InfoCommand.cs    # Card information
│   │   └── OpenMFA.CLI.csproj
│   │
│   └── OpenMFA.SmartCard/        # Smart card library
│       ├── PcSc/
│       │   ├── IPcScContext.cs
│       │   ├── PcScContext.cs    # PC/SC wrapper
│       │   ├── ICardReader.cs
│       │   └── CardReader.cs
│       ├── Piv/
│       │   ├── IPivCard.cs
│       │   ├── PivCard.cs        # PIV operations
│       │   ├── PivSlot.cs        # Slot definitions
│       │   ├── PivDataObject.cs  # Data object IDs
│       │   └── Apdu/
│       │       ├── ApduCommand.cs
│       │       ├── ApduResponse.cs
│       │       └── PivCommands.cs
│       └── OpenMFA.SmartCard.csproj
│
├── Directory.Build.props
└── .gitignore
```

## Technology Stack

- .NET 9.0
- C# 13
- System.CommandLine (CLI parsing)
- Native P/Invoke for PC/SC (winscard.dll / libpcsclite.so)

## CLI Commands

```bash
# Detect readers and cards
openmfa detect

# Get card information
openmfa info

# Read data from card slot
openmfa read --slot 9A

# Write certificate to card
openmfa write --slot 9A --file cert.der

# Delete data from slot
openmfa delete --slot 9A

# Generate key pair on card
openmfa generate-key --slot 9A --algorithm RSA2048

# List all data objects on card
openmfa list
```

## PIV Card Slots (NIST SP 800-73-4)

| Slot | Name | Purpose | Object ID |
|------|------|---------|-----------|
| 9A | PIV Authentication | Windows/Linux login | 5FC105 |
| 9C | Digital Signature | Email signing | 5FC10A |
| 9D | Key Management | Encryption | 5FC10B |
| 9E | Card Authentication | Physical access | 5FC101 |

## PC/SC Architecture

```
┌─────────────────────────────────────────────┐
│              OpenMFA.CLI                     │
│  (User Commands: detect, read, write, etc.) │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│          OpenMFA.SmartCard                   │
│  ┌──────────────────────────────────────┐   │
│  │         PivCard Class                 │   │
│  │  - SelectApplet()                     │   │
│  │  - GetData(slot)                      │   │
│  │  - PutData(slot, data)                │   │
│  │  - GenerateKeyPair(slot, algorithm)   │   │
│  └──────────────┬───────────────────────┘   │
│                 │                             │
│  ┌──────────────▼───────────────────────┐   │
│  │       CardReader Class                │   │
│  │  - Transmit(apdu)                     │   │
│  │  - GetAtr()                           │   │
│  └──────────────┬───────────────────────┘   │
│                 │                             │
│  ┌──────────────▼───────────────────────┐   │
│  │      PcScContext Class                │   │
│  │  - ListReaders()                      │   │
│  │  - Connect(readerName)                │   │
│  └──────────────┬───────────────────────┘   │
│                 │                             │
└─────────────────┼─────────────────────────────┘
                  │ P/Invoke
                  ▼
┌─────────────────────────────────────────────┐
│     Native PC/SC Library                     │
│  Windows: winscard.dll                       │
│  Linux: libpcsclite.so.1                     │
└─────────────────────────────────────────────┘
```

## APDU Command Structure

All PIV commands follow ISO 7816-4 APDU format:

```
Command APDU:
┌────┬────┬────┬────┬────┬─────────┬────┐
│ CLA│ INS│ P1 │ P2 │ Lc │  Data   │ Le │
└────┴────┴────┴────┴────┴─────────┴────┘

Response APDU:
┌─────────────┬────┬────┐
│    Data     │ SW1│ SW2│
└─────────────┴────┴────┘
```

### Key PIV Commands

| Command | CLA | INS | Description |
|---------|-----|-----|-------------|
| SELECT | 00 | A4 | Select PIV applet |
| GET DATA | 00 | CB | Read data from card |
| PUT DATA | 00 | DB | Write data to card |
| GENERATE KEY PAIR | 00 | 47 | Generate key on card |
| VERIFY | 00 | 20 | Verify PIN |
| CHANGE REFERENCE DATA | 00 | 24 | Change PIN |

## Implementation Details

### 1. PC/SC Context (Cross-Platform)

```csharp
public interface IPcScContext : IDisposable
{
    Task<IReadOnlyList<string>> ListReadersAsync(CancellationToken ct = default);
    Task<ICardReader> ConnectAsync(string readerName, CancellationToken ct = default);
}
```

**Platform Detection:**
- Windows: Load winscard.dll
- Linux: Load libpcsclite.so.1
- Use NativeLibrary.Load() for cross-platform P/Invoke

### 2. Card Reader Operations

```csharp
public interface ICardReader : IDisposable
{
    string Name { get; }
    Task<ApduResponse> TransmitAsync(ApduCommand command, CancellationToken ct = default);
    Task<byte[]> GetAtrAsync(CancellationToken ct = default);
}
```

### 3. PIV Card Operations

```csharp
public interface IPivCard : IDisposable
{
    Task<bool> SelectPivAppletAsync(CancellationToken ct = default);
    Task<byte[]> GetDataAsync(PivDataObject dataObject, CancellationToken ct = default);
    Task PutDataAsync(PivDataObject dataObject, byte[] data, CancellationToken ct = default);
    Task DeleteDataAsync(PivDataObject dataObject, CancellationToken ct = default);
    Task<PivPublicKey> GenerateKeyPairAsync(PivSlot slot, PivAlgorithm algorithm, CancellationToken ct = default);
    Task VerifyPinAsync(string pin, CancellationToken ct = default);
}
```

## Example Usage

```csharp
// Detect cards
using var context = new PcScContext();
var readers = await context.ListReadersAsync();
Console.WriteLine($"Found {readers.Count} reader(s)");

// Connect to card
using var reader = await context.ConnectAsync(readers[0]);
using var pivCard = new PivCard(reader);

// Select PIV applet
await pivCard.SelectPivAppletAsync();

// Verify PIN
await pivCard.VerifyPinAsync("123456");

// Generate key pair
var publicKey = await pivCard.GenerateKeyPairAsync(
    PivSlot.Authentication,
    PivAlgorithm.Rsa2048);

// Read certificate (if exists)
var certData = await pivCard.GetDataAsync(
    PivDataObject.AuthenticationCertificate);

// Write certificate
await pivCard.PutDataAsync(
    PivDataObject.AuthenticationCertificate,
    certDerBytes);

// Delete certificate
await pivCard.DeleteDataAsync(
    PivDataObject.AuthenticationCertificate);
```

## Build Configuration

**Directory.Build.props:**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
</Project>
```

## Dependencies

- **System.CommandLine** (2.0.0-beta4) - CLI parsing
- **No external smart card libraries** - Direct P/Invoke to native APIs

## Testing Approach

Manual testing with actual PIV cards:
1. Insert card
2. Run `openmfa detect` - verify card found
3. Run `openmfa info` - verify card details
4. Run `openmfa generate-key --slot 9A` - verify key generation
5. Run `openmfa read --slot 9A` - verify can read (will fail until cert written)
6. Write test certificate with `openmfa write`
7. Read back and verify
8. Delete with `openmfa delete`

## Security Notes

- PIN never logged or stored
- All operations require physical card presence
- Private keys never leave the card
- Administrative operations may require PUK (PIN Unlock Key)

## Next Steps (Future)

After smart card operations are working:
1. Add certificate generation
2. Add local CA for signing
3. Add enrollment workflow
4. Add Windows/Linux login integration
5. Add management database

---

**Status:** Minimal Viable Implementation
**Focus:** Smart card read/write operations only
**Target:** Windows/Linux login with card + PIN
