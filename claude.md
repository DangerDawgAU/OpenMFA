# OpenMFA - PIV PKI Self-Enrollment Client

## Project Overview

A self-enrollment client for PIV PKI infrastructure using Aventura PIV 4.5 Smart cards and OpenSC. This solution enables users to self-provision their PIV credentials for multi-factor authentication in enterprise environments.

## Technical Stack

### Smart Card Hardware
- **Card Type**: Aventura PIV 4.5 Smart Cards
- **Interface**: PC/SC compliant readers
- **Standard**: NIST SP 800-73-4 (PIV)

### Core Dependencies
- **OpenSC**: Open source smart card tools and libraries
- **PKCS#11**: Cryptographic token interface
- **OpenSSL**: Certificate and key management
- **PC/SC Lite** (Linux) / **PC/SC** (Windows/macOS): Smart card middleware

### Application Layer
- **Backend**: Python/Node.js for enrollment server
- **Frontend**: Electron or web-based UI for enrollment client
- **Database**: PostgreSQL/SQLite for enrollment tracking
- **CA Integration**: EJBCA, Microsoft ADCS, or custom CA

## Repository Structure

```
OpenMFA/
├── docs/
│   ├── architecture.md          # System architecture overview
│   ├── api-reference.md         # API documentation
│   ├── deployment.md            # Deployment guide
│   ├── user-guide.md            # End-user enrollment guide
│   └── security.md              # Security considerations
├── client/
│   ├── src/
│   │   ├── card/                # Smart card operations
│   │   │   ├── opensc.js        # OpenSC wrapper
│   │   │   ├── pkcs11.js        # PKCS#11 interface
│   │   │   ├── piv.js           # PIV operations
│   │   │   └── reader.js        # Card reader detection
│   │   ├── crypto/              # Cryptographic operations
│   │   │   ├── keygen.js        # Key generation
│   │   │   ├── csr.js           # Certificate signing request
│   │   │   └── cert.js          # Certificate operations
│   │   ├── ui/                  # User interface
│   │   │   ├── enrollment.js    # Enrollment workflow
│   │   │   ├── status.js        # Status display
│   │   │   └── validation.js    # Input validation
│   │   ├── api/                 # API client
│   │   │   └── enrollment.js    # Enrollment API calls
│   │   └── utils/
│   │       ├── config.js        # Configuration management
│   │       ├── logger.js        # Logging utilities
│   │       └── errors.js        # Error handling
│   ├── tests/
│   │   ├── unit/
│   │   └── integration/
│   ├── package.json
│   └── README.md
├── server/
│   ├── src/
│   │   ├── api/
│   │   │   ├── routes/
│   │   │   │   ├── enrollment.js    # Enrollment endpoints
│   │   │   │   ├── certificate.js   # Certificate management
│   │   │   │   └── status.js        # Status endpoints
│   │   │   └── middleware/
│   │   │       ├── auth.js          # Authentication
│   │   │       ├── validation.js    # Request validation
│   │   │       └── ratelimit.js     # Rate limiting
│   │   ├── ca/
│   │   │   ├── interface.js         # CA abstraction layer
│   │   │   ├── ejbca.js             # EJBCA integration
│   │   │   ├── adcs.js              # Microsoft ADCS integration
│   │   │   └── mock.js              # Mock CA for testing
│   │   ├── database/
│   │   │   ├── models/
│   │   │   │   ├── enrollment.js
│   │   │   │   ├── certificate.js
│   │   │   │   └── user.js
│   │   │   ├── migrations/
│   │   │   └── seeds/
│   │   ├── services/
│   │   │   ├── enrollment.js        # Enrollment business logic
│   │   │   ├── verification.js      # Identity verification
│   │   │   └── notification.js      # Email/SMS notifications
│   │   └── utils/
│   │       ├── config.js
│   │       ├── logger.js
│   │       └── crypto.js
│   ├── tests/
│   ├── package.json
│   └── README.md
├── scripts/
│   ├── setup/
│   │   ├── install-opensc.sh        # OpenSC installation
│   │   ├── configure-readers.sh     # Reader configuration
│   │   └── test-card.sh             # Card testing script
│   ├── admin/
│   │   ├── revoke-cert.sh           # Certificate revocation
│   │   ├── backup-db.sh             # Database backup
│   │   └── reset-card.sh            # Card reset utility
│   └── development/
│       ├── mock-card.sh             # Mock card for testing
│       └── seed-data.sh             # Test data generation
├── config/
│   ├── opensc.conf.example          # OpenSC configuration
│   ├── server.yaml.example          # Server configuration
│   ├── ca-config.yaml.example       # CA integration config
│   └── client.json.example          # Client configuration
├── docker/
│   ├── Dockerfile.client            # Client container
│   ├── Dockerfile.server            # Server container
│   ├── docker-compose.yml           # Development environment
│   └── docker-compose.prod.yml      # Production environment
├── .github/
│   └── workflows/
│       ├── ci.yml                   # Continuous integration
│       ├── security-scan.yml        # Security scanning
│       └── release.yml              # Release automation
├── .gitignore
├── LICENSE
├── README.md
└── claude.md                        # This file

```

## Core Components

### 1. Card Management Module
**Purpose**: Interface with Aventura PIV 4.5 cards via OpenSC

**Key Features**:
- Card detection and reader enumeration
- PIV applet initialization
- PIN/PUK management
- Key slot management (9A, 9C, 9D, 9E)
- Certificate storage operations

**OpenSC Integration**:
```bash
# Key operations
pkcs15-init --create-pkcs15
pkcs15-init --generate-key rsa/2048 --auth-id 01
pkcs15-tool --list-keys
pkcs11-tool --module opensc-pkcs11.so --list-slots
```

### 2. Enrollment Workflow
**Steps**:
1. **Pre-enrollment**
   - Verify card reader connectivity
   - Detect Aventura PIV 4.5 card
   - Validate card is uninitialized or ready for enrollment

2. **Identity Verification**
   - User authentication (LDAP/AD/OAuth)
   - Multi-factor verification
   - Authorization check

3. **PIN Setup**
   - User sets PIN (6-8 digits)
   - PUK generation and secure delivery
   - Admin key handling

4. **Key Generation**
   - On-card RSA 2048/4096 key generation
   - Key slots: Authentication (9A), Signature (9C), Key Management (9D), Card Authentication (9E)
   - Public key extraction

5. **Certificate Request**
   - Generate CSR with user DN
   - Submit to CA
   - Retrieve signed certificate

6. **Certificate Installation**
   - Write certificate to card
   - Verify installation
   - Test authentication

7. **Completion**
   - Generate enrollment receipt
   - Backup recovery codes
   - User notification

### 3. CA Integration Layer
**Supported CAs**:
- EJBCA (open source)
- Microsoft Active Directory Certificate Services (ADCS)
- Custom CA via REST API

**Certificate Profiles**:
- PIV Authentication Certificate (9A)
- Digital Signature Certificate (9C)
- Key Management Certificate (9D)
- Card Authentication Certificate (9E)

### 4. Security Features

**Card Security**:
- PIN retry limits (3 attempts)
- PUK for PIN unblocking
- Secure key generation (on-card)
- Certificate pinning

**Transport Security**:
- TLS 1.3 for all API communication
- Certificate-based client authentication
- Mutual TLS (mTLS) support

**Audit & Compliance**:
- Comprehensive audit logging
- Enrollment tracking
- Certificate lifecycle management
- NIST SP 800-73-4 compliance

### 5. Platform Support

**Operating Systems**:
- Windows 10/11 (with Windows Hello integration)
- macOS 11+ (with Keychain integration)
- Linux (Ubuntu, RHEL, Debian)

**Smart Card Readers**:
- USB CCID-compliant readers
- Built-in readers (laptops)
- NFC readers (optional)

## Development Phases

### Phase 1: Foundation (Weeks 1-2)
- [ ] Repository setup and structure
- [ ] OpenSC integration and testing
- [ ] Card detection and basic operations
- [ ] Development environment with mock cards

### Phase 2: Core Enrollment (Weeks 3-5)
- [ ] Key generation workflow
- [ ] CSR generation and submission
- [ ] Mock CA integration
- [ ] Certificate installation
- [ ] Basic UI/CLI interface

### Phase 3: Server Infrastructure (Weeks 6-8)
- [ ] Enrollment server API
- [ ] Database schema and models
- [ ] Authentication and authorization
- [ ] Real CA integration (EJBCA/ADCS)

### Phase 4: Production Features (Weeks 9-11)
- [ ] PIN/PUK management
- [ ] Certificate renewal
- [ ] Revocation handling
- [ ] Multi-platform client builds
- [ ] Admin dashboard

### Phase 5: Security & Testing (Weeks 12-14)
- [ ] Security audit
- [ ] Penetration testing
- [ ] Compliance validation
- [ ] Documentation completion
- [ ] Deployment guides

## Configuration Examples

### OpenSC Configuration (`opensc.conf`)
```conf
app default {
    card_drivers = piv;

    card_driver piv {
        enable_pin_cache = false;
        pin_cache_counter = 3;
    }
}
```

### Aventura PIV 4.5 Specifications
- **Chip**: JavaCard platform
- **Memory**: 144KB EEPROM
- **Algorithm Support**: RSA 2048/4096, ECC P-256/P-384
- **PIV Slots**: 9A, 9C, 9D, 9E, 82-95 (retired)
- **PIN Length**: 6-8 digits
- **Interface**: ISO 7816 contact, optional contactless

## API Endpoints

### Enrollment API
```
POST   /api/v1/enrollment/initialize    - Start enrollment
POST   /api/v1/enrollment/verify         - Verify identity
POST   /api/v1/enrollment/request-cert   - Submit CSR
GET    /api/v1/enrollment/status/:id     - Check status
POST   /api/v1/enrollment/complete       - Finalize enrollment

GET    /api/v1/certificates/:serial      - Get certificate
POST   /api/v1/certificates/renew        - Renew certificate
POST   /api/v1/certificates/revoke       - Revoke certificate

GET    /api/v1/health                    - Health check
GET    /api/v1/ca/status                 - CA connectivity
```

## Testing Strategy

### Unit Tests
- Card operations (mocked)
- Cryptographic functions
- API endpoints
- Database operations

### Integration Tests
- End-to-end enrollment workflow
- CA integration
- Multi-platform testing
- Reader compatibility

### Security Tests
- PIN brute-force protection
- TLS configuration
- Input validation
- SQL injection prevention
- XSS prevention

## Deployment Models

### 1. Enterprise On-Premises
- Deployed within corporate network
- Integrated with AD/LDAP
- Internal CA (ADCS)

### 2. Cloud-Hosted
- SaaS enrollment service
- Multi-tenant support
- Cloud CA integration

### 3. Hybrid
- Cloud management
- On-premises enrollment kiosks
- Federated identity

## Dependencies

### Client
```json
{
  "node-pcsclite": "^0.7.x",
  "pkcs11js": "^2.x.x",
  "node-forge": "^1.3.x",
  "electron": "^28.x.x"
}
```

### Server
```json
{
  "express": "^4.18.x",
  "sequelize": "^6.x.x",
  "pg": "^8.x.x",
  "jsonwebtoken": "^9.x.x",
  "helmet": "^7.x.x"
}
```

### System
- OpenSC 0.23.0+
- OpenSSL 3.0+
- PC/SC Lite (Linux) or native PC/SC
- Compatible smart card reader

## Security Considerations

1. **Key Generation**: Always generate keys on-card, never import
2. **PIN Handling**: Never log or transmit PINs in plaintext
3. **Certificate Validation**: Verify entire certificate chain
4. **Audit Trail**: Log all enrollment and certificate operations
5. **Access Control**: Role-based access for admin functions
6. **Data Protection**: Encrypt sensitive data at rest
7. **Secure Defaults**: Fail-safe configurations

## Compliance & Standards

- **NIST SP 800-73-4**: PIV Card Interface
- **NIST SP 800-78-4**: Cryptographic Algorithms
- **FIPS 201-3**: PIV of Federal Employees and Contractors
- **ISO/IEC 7816**: Smart card standards
- **PKCS#11 v2.40**: Cryptographic Token Interface

## Future Enhancements

- [ ] Biometric integration
- [ ] Mobile device enrollment (via NFC)
- [ ] Derived PIV credentials
- [ ] Certificate transparency logging
- [ ] Hardware security module (HSM) integration
- [ ] Automated certificate lifecycle management
- [ ] Web authentication (WebAuthn/FIDO2) support

## Resources

### Documentation
- [OpenSC Wiki](https://github.com/OpenSC/OpenSC/wiki)
- [NIST PIV Standards](https://csrc.nist.gov/projects/piv)
- [Aventura Technologies](https://aventura.fi/)

### Tools
- OpenSC tools suite
- pkcs11-tool
- pkcs15-tool
- OpenSSL
- pcscd (PC/SC daemon)

### Testing
- Virtual Smart Card (Windows)
- vsmartcard (Linux)
- Mock PKCS#11 libraries

---

**Project Status**: Planning Phase
**Last Updated**: 2025-12-19
**Maintainer**: OpenMFA Team
