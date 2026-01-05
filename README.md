# OpenMFA - MyEID Windows Smart Card Logon Setup

Simple Windows application for setting up **MyEID 4.5** smart cards for Windows smart card authentication.

## Overview

This application provides a streamlined GUI for configuring MyEID cards for Windows logon:
- Initialize MyEID 4.5 cards with PKCS#15 structure
- Generate RSA authentication keys (slot 9A)
- Import certificates for Windows smart card logon
- Verify card setup for Windows compatibility

## Requirements

### Software
- **.NET 9.0** Runtime
- **OpenSC 0.23.0+** ([Download](https://github.com/OpenSC/OpenSC/releases))
  - Must be installed to: `C:\Program Files\OpenSC Project\OpenSC\`
- **PC/SC Smart Card Service** (built into Windows)

### Hardware
- **MyEID 4.5** smart card (Aventra)
- **PC/SC compatible smart card reader** (USB or NFC)

## Installation

1. Install OpenSC from [official releases](https://github.com/OpenSC/OpenSC/releases/latest)
2. Clone or download this repository
3. Build: `dotnet build`
4. Run: `dotnet run --project src\OpenMFA.GUI`

## Usage

### Step 1: Detect Card Reader
1. Insert MyEID card into reader
2. Click "Detect Reader & Card"
3. Verify reader is detected with green status

### Step 2: Initialize Card
⚠️ **WARNING**: This erases ALL data on the card!

The initialization process will:
1. Erase the card (tries `--no-so-pin` first for blank cards)
2. Create PKCS#15 structure with MyEID profile
3. Set SO-PIN and SO-PUK (administrative PINs)
4. Create user PIN context with ID 01
5. Set user PIN and PUK for authentication

Configure PINs:
- **User PIN**: 4-8 digits (default: `1111`) - for daily use
- **User PUK**: 6-8 digits (default: `111111`) - to unlock user PIN
- **SO PIN**: 4-8 digits (default: `12345678`) - for admin operations
- **SO PUK**: 4-8 digits (default: `12345678`) - to unlock SO-PIN

Steps:
1. Enter desired PINs in the form (or keep defaults)
2. Click "Initialize Card"
3. Confirm warning dialog
4. Wait for completion (creates PKCS#15 structure + user PIN context)

### Step 3: Generate Authentication Key
1. Select key size:
   - **RSA 2048** (recommended for Windows)
   - RSA 3072 (higher security, slower)
   - RSA 4096 (maximum security, slowest)
2. Enter PIN (default: `1111`)
3. Click "Generate Key Pair"
4. Wait 30-90 seconds for on-card generation

### Step 4: Import Certificate
Your certificate **MUST** have these extensions for Windows logon:
- **Key Usage**: `digitalSignature, keyEncipherment`
- **Enhanced Key Usage**:
  - Client Authentication (1.3.6.1.5.5.7.3.2)
  - **Smart Card Logon** (1.3.6.1.4.1.311.20.2.2) ← Required!

To import:
1. Click "Import Certificate"
2. Select certificate file (`.der`, `.crt`, or `.cer`)
3. Enter PIN
4. Certificate is stored in slot 9A (Authentication)

### Step 5: Verify Setup
1. Click "Verify Card Setup"
2. Check output log shows:
   - Certificate in slot 01 (maps to PIV slot 9A)
   - Key pair present
   - All objects readable

## Windows Logon Configuration

After card setup:

### 1. Trust the Certificate Chain
If using self-signed or internal CA:
```powershell
# Import CA certificate to Trusted Root
certutil -addstore "Root" ca-cert.cer
```

### 2. Verify Windows Recognizes Card
```powershell
# This should show your certificate
certutil -scinfo
```

### 3. Test Logon
1. Lock Windows (Win+L)
2. Insert smart card
3. Windows should prompt for PIN
4. Enter PIN and authenticate

## Troubleshooting

### Card Not Detected
- **Check reader connection**: Ensure USB reader is plugged in
- **Restart PC/SC service**:
  ```powershell
  net stop SCardSvr
  net start SCardSvr
  ```
- **Verify OpenSC installation**:
  ```powershell
  "C:\Program Files\OpenSC Project\OpenSC\tools\opensc-tool.exe" --version
  ```

### Initialize Hangs or Fails
- Remove and reinsert card
- Use "ERASE Card" button to factory reset
- Try with different reader (some NFC readers have issues)
- Check output log for specific error messages
- **If card shows "Unsupported card" error**: Card needs initialization from scratch
  - Click ERASE button (will try `--no-so-pin` for blank cards)
  - If ERASE fails with "invalid length", card may have unknown SO-PIN
  - For blank/corrupted cards, erase works without SO-PIN

### SO-PIN Lockout / Card Reset
- ⚠️ **MyEID cards automatically reset after ~5 failed SO-PIN attempts**
- If you enter the wrong SO-PIN multiple times during erase:
  - Card will perform a factory reset automatically
  - All PKCS#15 structure is erased
  - Card becomes blank/uninitialized again
  - You can then erase and initialize without SO-PIN using `--no-so-pin`
- **Good news**: If you're locked out, the card self-resets to factory state
- **After self-reset**: Run Initialize Card to set up fresh with new PINs

### Key Generation Fails
- **Most common issue**: User PIN context not created
  - Solution: Re-run Initialize Card (creates user PIN context automatically)
  - Verify with: `pkcs15-tool --reader 0 --list-pins` (should show "User PIN" with ID 01)
- Verify PIN is correct (default: `1111`)
- Try RSA 2048 instead of 4096 (4096 can take 2+ minutes)
- Check card has free space
- Error "Requested object not found" = missing user PIN context

### Certificate Import Fails
- Verify certificate is in DER binary format (not PEM text)
- Ensure key was generated on card first
- Check PIN is correct
- Certificate must match key algorithm (RSA)

### Windows Won't Authenticate
- **Verify Smart Card Logon EKU**:
  ```powershell
  certutil -dump certificate.cer
  # Look for: Enhanced Key Usage: Smart Card Logon (1.3.6.1.4.1.311.20.2.2)
  ```
- **Check certificate chain is trusted**:
  ```powershell
  certutil -verify certificate.cer
  ```
- **Ensure certificate matches user account**:
  - Subject CN must match Windows username, OR
  - Certificate must have UPN (User Principal Name) in Subject Alternative Name
- **Verify card is recognized**:
  ```powershell
  certutil -scinfo
  ```

## Technical Details

### Architecture
- **Language**: C# .NET 9 (Windows Forms)
- **Card Interface**: OpenSC PKCS#15 tools (`pkcs15-init`, `pkcs15-tool`)
- **Supported Cards**: MyEID 4.5 (PKCS#15 compliant with PIV emulation)
- **Authentication Slot**: 9A (Object ID 01 in PKCS#15)

### OpenSC Commands Used Internally
```bash
# Detect readers
opensc-tool --list-readers

# Initialize card (3-step process)
pkcs15-init --erase-card --no-so-pin
pkcs15-init --create-pkcs15 --profile myeid --pin 1111 --puk 111111 --so-pin 12345678 --so-puk 12345678
pkcs15-init --store-pin --label "User PIN" --auth-id 01 --pin 1111 --puk 111111 --so-pin 12345678

# Generate key in slot 9A (object ID 01) - requires user PIN context
pkcs15-init --generate-key rsa/2048 --auth-id 01 --id 01 --label "PIV AUTH" --pin 1111

# Store certificate
pkcs15-init --store-certificate cert.der --id 01 --label "User Certificate" --pin 1111

# List all objects
pkcs15-tool --dump

# Read certificate
pkcs15-tool --read-certificate 01
```

### File Structure
```
OpenMFA/
├── src/
│   ├── OpenMFA.SmartCard/
│   │   └── MyEid/
│   │       ├── MyEidCard.cs        # Core PKCS#15 operations
│   │       └── CardReader.cs       # Reader detection
│   └── OpenMFA.GUI/
│       └── WindowsLogonForm.cs     # Main application window
├── README.md                        # This file
└── LICENSE
```

### PKCS#15 vs PIV
MyEID cards use **PKCS#15 file structure** internally but present a **PIV-compatible interface** for Windows:
- Data stored using `pkcs15-init` (PKCS#15 tools)
- Object ID 01 maps to PIV slot 9A
- Windows queries PIV location 5FC105 (slot 9A)
- Card responds with certificate from PKCS#15 object 01
- Result: Windows sees a PIV card, MyEID stores data as PKCS#15

### Initialization Process Details
The initialization creates these structures on the card:
1. **PKCS#15 application** with MyEID profile
2. **SO-PIN** (ID: ff, Reference: 3) - administrative PIN
3. **User PIN** (ID: 01, Reference: 1) - authentication PIN
4. **PIN context with auth-id 01** - required before key generation

**Important**: The user PIN context MUST exist before generating keys. The app automatically creates it during initialization using `--store-pin` command.

## Security Notes

- **PINs**: Never stored - only passed to OpenSC tools during operations
- **Private Keys**: Generated on-card, never exported
- **Key Generation**: All cryptographic operations happen inside the secure element
- **SO-PIN**: Protects administrative functions (erase, initialize)
- **User PIN**: Required for authentication and signing
- **SO-PIN Lockout**: After ~5 failed SO-PIN attempts, MyEID cards automatically perform factory reset
  - All PKCS#15 data is erased
  - Card becomes blank/uninitialized
  - Can be re-initialized without SO-PIN using `--no-so-pin`
  - This is a security feature to prevent brute-force attacks

## Standards Compliance

- **PKCS#15**: ISO/IEC 7816-15 card file system
- **PIV**: NIST SP 800-73-4 compatible (emulation layer)
- **X.509**: RFC 5280 certificate format
- **OpenSC**: Official `myeid.profile` support

## Example: Creating Windows Logon Certificate

If you have a Windows CA, request a certificate with this template:
```powershell
# Request certificate (requires Enterprise CA)
certreq -new -f request.inf cert.req
certreq -submit -attrib "CertificateTemplate:SmartcardLogon" cert.req cert.cer

# Convert to DER format for import
certutil -encode cert.cer cert.der
```

**request.inf**:
```ini
[NewRequest]
Subject = "CN=YourUsername"
KeyLength = 2048
Exportable = FALSE
UserProtected = FALSE
MachineKeySet = FALSE
ProviderName = "Microsoft Base Smart Card Crypto Provider"
RequestType = PKCS10
KeyUsage = 0xA0 ; Digital Signature, Key Encipherment

[Extensions]
2.5.29.37 = "{text}"
_continue_ = "1.3.6.1.5.5.7.3.2," ; Client Authentication
_continue_ = "1.3.6.1.4.1.311.20.2.2" ; Smart Card Logon
```

## License

MIT License

## Support

- **Issues**: [GitHub Issues](https://github.com/YourUsername/OpenMFA/issues)
- **OpenSC Documentation**: https://github.com/OpenSC/OpenSC/wiki
- **MyEID Specifications**: https://github.com/OpenSC/OpenSC/wiki/Aventra-MyEID-PKI-card
- **Windows Smart Card Logon**: https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/component-updates/smart-card-windows-vista

## Credits

- **OpenSC Project** for PKCS#15 and smart card tools
- **Aventra** for MyEID 4.5 specifications and OpenSC driver support
