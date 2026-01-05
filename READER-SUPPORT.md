# Smart Card Reader Support

OpenMFA supports multiple types of smart card readers through the OpenSC framework.

## Supported Reader Types

### 1. Contact-Based Readers
**Examples:**
- Broadcom Corp Contacted Smartcard (laptop built-in)
- Generic USB contact readers
- CCID-compliant contact readers

**Detection:** Shows as `Broadcom Corp Contacted Smartcard` or similar
**Card Insertion:** Physical contact required
**Use Case:** Traditional PKI cards, government ID cards

### 2. NFC/Contactless Readers
**Examples:**
- ACS ACR122U PICC Interface
- ACS ACR1252U
- SCM SCL3711
- Identiv uTrust readers

**Detection:** Shows reader model name (e.g., `ACS ACR122U PICC Interface 0`)
**Card Insertion:** Place card near reader (no physical contact)
**Use Case:** NFC-enabled smart cards, contactless PIV cards

### 3. USB Cryptographic Tokens
**Examples:**
- Yubico YubiKey (CCID mode)
- Nitrokey
- SoloKeys

**Detection:** Shows as `Yubico YubiKey CCID 0` or similar
**Card Insertion:** USB device is the card
**Use Case:** Hardware security keys, MFA tokens

## How Detection Works

OpenMFA uses `opensc-tool --list-readers` which outputs:

```
# Detected readers (pcsc)
Nr.  Card  Features  Name
0    Yes             [Reader Name Here]
```

The application automatically detects:
- **Slot Number** (Nr.)
- **Card Presence** (Yes/No in Card column)
- **Reader Name** (Name column)

## Compatibility

### Fully Supported Cards
- **MyEID 4.5** (PKCS#15) - All operations
- **YubiKey PIV** - PIV operations via PKCS#11
- **Generic PKCS#15 cards** - Via pkcs15-init/pkcs15-tool
- **Generic PIV cards** - Via pkcs11-tool fallback

### Reader Requirements
- Must be PCSC-compatible (PC/SC Smart Card standard)
- Drivers must be installed and recognized by Windows Smart Card Service
- Must appear in `opensc-tool --list-readers` output

## Testing Your Reader

### Command Line Test
```cmd
# Check if reader is detected
"C:\Program Files\OpenSC Project\OpenSC\tools\opensc-tool.exe" --list-readers

# Expected output with card inserted:
Nr.  Card  Features  Name
0    Yes             [Your Reader Name]

# Without card:
Nr.  Card  Features  Name
0    No              [Your Reader Name]
```

### Windows System Check
```cmd
# Verify PC/SC service is running
powershell -Command "Get-Service -Name SCardSvr"

# Check for smart card readers in Device Manager
devmgmt.msc
# Look under: Smart card readers
```

## Common NFC Reader Models

### Recommended for Development
| Model | Type | Notes |
|-------|------|-------|
| ACS ACR122U | USB NFC | Popular, well-supported, affordable |
| ACS ACR1252U | USB NFC | Dual interface (contact + contactless) |
| Identiv uTrust 3700 F | USB NFC | Enterprise-grade, FIPS certified |
| SCM SCL3711 | USB NFC | Compact, portable |

### Features to Look For
- **ISO 14443 A/B support** - MyEID 4.5 cards use ISO 14443
- **CCID compliance** - Better OS support
- **PC/SC compatibility** - Required for OpenSC
- **Windows driver support** - Pre-installed or available drivers

## Troubleshooting

### Reader Not Detected
1. **Check PC/SC service:** Must be running
   ```cmd
   powershell -Command "Restart-Service SCardSvr"
   ```

2. **Install reader drivers:** Check manufacturer website

3. **Verify USB connection:** Try different port

4. **Check Device Manager:** Look for yellow exclamation marks

### Card Not Detected (Reader Shows "No")
1. **For contact readers:** Ensure card is fully inserted
2. **For NFC readers:**
   - Place card flat on reader surface
   - Remove card from wallet/holder
   - Try different positioning
   - Some cards need to be within 1-2cm

3. **Check card type:** Ensure it's ISO 14443 compatible

### Multiple Readers
- Each reader gets a unique slot number (0, 1, 2, ...)
- GUI shows all readers with cards present
- You can select which reader to use by slot number
- MyEID operations use the detected slot automatically

## Air-Gapped Deployment

For isolated/air-gapped environments:
- ✅ USB contact readers work offline
- ✅ NFC readers work offline
- ✅ No network required for card operations
- ✅ All operations local to PC/SC subsystem

## Summary

OpenMFA's reader support is designed to be **universal and flexible**:
- Works with any PCSC-compatible reader
- Automatically detects contact, NFC, and USB token readers
- No configuration needed - just plug in and detect
- Supports multiple readers simultaneously
- Works in air-gapped environments

The implementation uses OpenSC's native reader detection, ensuring compatibility with the widest range of hardware.
