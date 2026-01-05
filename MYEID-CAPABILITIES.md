# MyEID 4.5 PKI Card - Complete Capabilities

## Overview

The MyEID 4.5 is a PKCS#15 compliant smart card from Aventra Ltd. This document details all supported operations and how to use them with OpenMFA.

## Card Specifications

- **Manufacturer**: Aventra Ltd.
- **EEPROM Storage**: 144 KB
- **Controller**: NXP SmartMX2 SECID P60
- **Operating System**: JCOP3 Java Card
- **Standards**: ISO/IEC 7816, PKCS#15, PIV/CIV emulation

## Cryptographic Capabilities

### RSA Key Sizes
- 512 bits
- 1024 bits
- 2048 bits (recommended)
- 3072 bits
- 4096 bits

**Performance** (4096-bit):
- On-card key generation: ~60 seconds
- Digital signature: 3-4 seconds

### Elliptic Curve Cryptography
Supported curves:
- **NIST P-256** (secp256r1 / prime256v1)
- **NIST P-384** (secp384r1)
- **NIST P-521** (secp521r1)
- EC key sizes: 192-521 bits

### Symmetric Encryption
- **AES**: 128, 192, 256 bits
- **3DES**: Triple DES

## PIN Configuration

### User PIN
- **Length**: 4-8 digits
- **Encoding**: ASCII numeric
- **Attempts**: 3
- **Reference**: 1

### User PUK (PIN Unblock Key)
- **Length**: 4-8 digits
- **Attempts**: 10

### SO PIN (Security Officer)
- **Length**: 4-8 digits
- **Attempts**: 3
- **Reference**: 3
- **Purpose**: Administrative operations

### SO PUK
- **Length**: 4-8 digits
- **Attempts**: 10

## File System Structure

The MyEID card uses a hierarchical PKCS#15 file system:

```
MF (3F00) - Master File
├── DIR (2F00) - Directory file
└── PKCS15-AppDF (5015) - Application DF
    ├── PKCS15-ODF (5031) - Object Directory File
    ├── PKCS15-TokenInfo (5032) - Token Information
    ├── PKCS15-UnusedSpace (5033)
    ├── PKCS15-AODF (4401) - Authentication Object DF
    ├── PKCS15-PrKDF (4402) - Private Key DF
    ├── PKCS15-PuKDF (4404) - Public Key DF
    ├── PKCS15-SKDF (4407) - Secret Key DF
    ├── PKCS15-CDF (4403) - Certificate DF
    ├── PKCS15-CDF-TRUSTED (4405) - Trusted Certificate DF
    └── PKCS15-DODF (4406) - Data Object DF
```

## Supported Operations

### 1. Card Initialization

```csharp
var myeid = new MyEidOperations(readerNumber: 0);

// Initialize card with default PINs
await myeid.InitializeCardAsync(
    userPin: "1111",
    userPuk: "1111",
    soPin: "12345678",
    soPuk: "12345678"
);
```

**Command Line**:
```bash
pkcs15-init --erase-card --reader 0
pkcs15-init --create-pkcs15 --profile myeid --pin 1111 --puk 1111 --so-pin 12345678 --so-puk 12345678 --reader 0
```

### 2. Key Generation

#### RSA Keys

```csharp
// Generate 2048-bit RSA key
await myeid.GenerateRsaKeyAsync(
    keySize: 2048,
    authId: "01",
    objectId: "01",
    label: "My RSA Key",
    pin: "1111",
    outputFile: "pubkey.pem"
);
```

**Command Line**:
```bash
pkcs15-init --generate-key rsa/2048 --auth-id 01 --id 01 --label "My RSA Key" --pin 1111 --output-file pubkey.pem --reader 0
```

#### ECC Keys

```csharp
// Generate P-256 ECC key
await myeid.GenerateEccKeyAsync(
    curve: "prime256v1",
    authId: "02",
    objectId: "02",
    label: "My ECC Key",
    pin: "1111",
    outputFile: "pubkey_ec.pem"
);
```

**Command Line**:
```bash
pkcs15-init --generate-key ec/prime256v1 --auth-id 02 --id 02 --label "My ECC Key" --pin 1111 --output-file pubkey_ec.pem --reader 0
```

### 3. Certificate Management

#### Store Certificate

```csharp
await myeid.StoreCertificateAsync(
    certFile: "mycert.der",
    objectId: "01",
    label: "My Certificate",
    pin: "1111",
    isAuthority: false
);
```

**Command Line**:
```bash
pkcs15-init --store-certificate mycert.der --id 01 --label "My Certificate" --pin 1111 --reader 0
```

#### Update Certificate

```csharp
await myeid.UpdateCertificateAsync(
    certFile: "newcert.der",
    objectId: "01",
    pin: "1111"
);
```

#### Read Certificate

```csharp
byte[] certData = await myeid.ReadCertificateAsync("01");
```

**Command Line**:
```bash
pkcs15-tool --read-certificate 01 --output mycert.der --reader 0
```

#### Delete Certificate

```csharp
await myeid.DeleteCertificateAsync(
    objectId: "01",
    pin: "1111"
);
```

**Command Line**:
```bash
pkcs15-init --delete-objects cert --id 01 --pin 1111 --reader 0
```

### 4. Data Objects

Store arbitrary data on the card:

```csharp
await myeid.StoreDataObjectAsync(
    dataFile: "mydata.bin",
    applicationName: "My Application",
    applicationId: "DEADBEEF",
    label: "Application Data",
    pin: "1111"
);
```

**Command Line**:
```bash
pkcs15-init --store-data mydata.bin --application-name "My Application" --application-id DEADBEEF --label "Application Data" --pin 1111 --reader 0
```

### 5. PIN Management

#### Store Additional PIN

```csharp
await myeid.StorePinAsync(
    authId: "02",
    label: "Signature PIN",
    pin: "2222",
    puk: "22222222",
    soPin: "12345678"
);
```

#### List PINs

```csharp
string pins = await myeid.ListPinsAsync();
Console.WriteLine(pins);
```

### 6. Card Information

```csharp
// Get card serial number
string serial = await myeid.GetSerialNumberAsync();

// Get card name
string name = await myeid.GetCardNameAsync();

// Get detailed card info
string info = await myeid.GetCardInfoAsync();

// Dump all objects
string dump = await myeid.DumpCardAsync();
```

### 7. Finalization

After completing all initialization, finalize the card:

```csharp
await myeid.FinalizeCardAsync();
```

**Command Line**:
```bash
pkcs15-init --finalize --reader 0
```

## Object ID Mapping

For consistency with PIV slots:

| PIV Slot | Object ID | Label | Purpose |
|----------|-----------|-------|---------|
| 9A | 01 | PIV AUTH | Authentication |
| 9C | 02 | SIGNATURE | Digital Signature |
| 9D | 03 | KEY MGMT | Key Management/Encryption |
| 9E | 04 | CARD AUTH | Card Authentication |
| 82 | 05 | RETIRED1 | Retired Key 1 |
| 83 | 06 | RETIRED2 | Retired Key 2 |

## Access Control

The MyEID profile defines the following access rules:

- **PIN protected**: Private keys, certificates, secret keys
- **SO PIN protected**: Token info, directory files
- **User PIN**: Can create/update/delete keys and certificates
- **SO PIN**: Can erase card, modify structure, update token info

## Typical Workflow

### Initial Setup

1. **Erase and initialize card**
   ```bash
   pkcs15-init --erase-card
   pkcs15-init --create-pkcs15 --profile myeid --pin 1111 --puk 1111 --so-pin 12345678 --so-puk 12345678
   ```

2. **Generate key pair**
   ```bash
   pkcs15-init --generate-key rsa/2048 --auth-id 01 --id 01 --label "PIV AUTH" --pin 1111
   ```

3. **Store certificate**
   ```bash
   pkcs15-init --store-certificate cert.der --id 01 --label "My Certificate" --pin 1111
   ```

4. **Finalize card**
   ```bash
   pkcs15-init --finalize
   ```

### Verification

```bash
# List all objects
pkcs15-tool --dump

# List keys
pkcs15-tool --list-keys

# List certificates
pkcs15-tool --list-certificates

# Get card info
opensc-tool --name --serial
```

## Troubleshooting

### Slow Performance
- Add to `opensc.conf`:
  ```
  reader_driver pcsc {
      max_recv_size = 192;
  }
  ```

### PIN Blocked
- Unblock with PUK:
  ```bash
  pkcs15-tool --unblock-pin --puk <PUK> --new-pin <NEW_PIN>
  ```

### Erase Failed Card
- Use SO PIN:
  ```bash
  pkcs15-init --erase-card --so-pin 12345678
  ```

## References

- [MyEID Wiki Page](https://github.com/OpenSC/OpenSC/wiki/Aventra-MyEID-PKI-card)
- [MyEID Profile](https://github.com/OpenSC/OpenSC/blob/master/src/pkcs15init/myeid.profile)
- [MyEID Driver](https://github.com/OpenSC/OpenSC/blob/master/src/libopensc/card-myeid.c)
- [PKCS#15 Standard](https://www.cryptsoft.com/pkcs11doc/v220/group__SEC__12__1__6__PKCS____15.html)
