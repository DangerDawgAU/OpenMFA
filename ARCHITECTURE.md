# OpenMFA - .NET 9 Architecture & Design Document

## Executive Summary

OpenMFA is a PIV (Personal Identity Verification) self-enrollment client for air-gapped environments. This document outlines the complete .NET 9 C# implementation architecture for a secure, standalone smart card enrollment system.

---

## 1. Application Overview

### 1.1 Purpose
Self-service enrollment system for PIV smart cards (Aventura PIV 4.5) that:
- Generates cryptographic keys directly on smart cards
- Issues certificates from a local Certificate Authority
- Maintains enrollment audit logs
- Operates in completely isolated, air-gapped environments

### 1.2 Key Requirements
- **Security**: Private keys never leave the smart card
- **Isolation**: Zero network dependencies
- **Simplicity**: CLI-based operation for administrators
- **Compliance**: NIST SP 800-73-4, ISO 7816, PKCS#11 v2.40
- **Audit**: Complete enrollment logging

---

## 2. Application Workflow

### 2.1 High-Level Process Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    OpenMFA Enrollment Flow                   │
└─────────────────────────────────────────────────────────────┘

1. CA Initialization (One-time Setup)
   ├─> Generate Root CA private key (RSA 4096)
   ├─> Create self-signed CA certificate
   ├─> Initialize certificate index database
   └─> Store CA files securely

2. Card Enrollment (Per User)
   ├─> Detect smart card reader
   ├─> Verify PIV applet present
   ├─> Set user PIN (6-8 digits)
   ├─> Generate RSA 2048 key pair ON CARD
   ├─> Extract public key from card
   ├─> Build Certificate Signing Request (CSR)
   ├─> Sign certificate with CA
   ├─> Write certificate back to card (Slot 9A)
   ├─> Log enrollment to SQLite
   └─> Display success/serial number

3. Management Operations
   ├─> List enrolled cards
   ├─> View certificate details
   ├─> Reset/re-initialize cards
   └─> Export CA certificate
```

### 2.2 Detailed Enrollment Steps

```
Step 1: Card Detection
├─> Initialize PC/SC connection
├─> Enumerate card readers
├─> Select PIV applet (AID: A000000308000010000100)
└─> Verify card is blank or resettable

Step 2: PIN Configuration
├─> Prompt for user PIN (6-8 digits)
├─> Verify PIN complexity
├─> Set PIV PIN
└─> Set PUK (PIN Unlock Key)

Step 3: Key Generation
├─> Send GENERATE ASYMMETRIC KEY PAIR command
├─> Specify key slot (9A - PIV Authentication)
├─> Algorithm: RSA 2048-bit
├─> Card generates key pair internally
└─> Return public key modulus and exponent

Step 4: CSR Creation
├─> Collect subject information (CN, Email, etc.)
├─> Build X.509 Name (Distinguished Name)
├─> Create PKCS#10 CSR with public key
├─> No signature needed (key can't sign yet)
└─> Format as PEM

Step 5: Certificate Issuance
├─> Load CA private key
├─> Parse CSR
├─> Create X.509v3 certificate
├─> Add extensions (Key Usage, Extended Key Usage)
├─> Sign with CA key (SHA-256)
├─> Generate unique serial number
├─> Set validity period (365 days default)
└─> Store in CA index

Step 6: Certificate Installation
├─> Convert certificate to DER format
├─> Send PUT DATA command to card
├─> Write to PIV certificate slot (5FC105)
└─> Verify write success

Step 7: Audit Logging
├─> Record enrollment timestamp
├─> Log certificate serial number
├─> Store subject DN
├─> Record card GUID
└─> Commit to SQLite database
```

---

## 3. .NET 9 Project Architecture

### 3.1 Solution Structure

```
OpenMFA.sln
│
├── src/
│   ├── OpenMFA.CLI/                      # Command-line interface
│   │   ├── OpenMFA.CLI.csproj
│   │   ├── Program.cs                    # Entry point, DI setup
│   │   ├── Commands/
│   │   │   ├── InitCaCommand.cs          # CA initialization
│   │   │   ├── EnrollCommand.cs          # Card enrollment
│   │   │   ├── ListCommand.cs            # List enrollments
│   │   │   ├── ResetCardCommand.cs       # Card reset
│   │   │   └── ExportCaCommand.cs        # Export CA cert
│   │   └── Options/
│   │       ├── EnrollOptions.cs          # CLI arguments
│   │       └── GlobalOptions.cs
│   │
│   ├── OpenMFA.Core/                     # Core business logic
│   │   ├── OpenMFA.Core.csproj
│   │   ├── Services/
│   │   │   ├── IEnrollmentService.cs
│   │   │   ├── EnrollmentService.cs      # Orchestrates enrollment
│   │   │   ├── ICardService.cs
│   │   │   ├── CardService.cs            # Smart card operations
│   │   │   ├── ICertificateAuthority.cs
│   │   │   ├── CertificateAuthority.cs   # CA operations
│   │   │   └── IAuditService.cs
│   │   │   └── AuditService.cs           # Logging service
│   │   ├── Models/
│   │   │   ├── EnrollmentRequest.cs
│   │   │   ├── EnrollmentResult.cs
│   │   │   ├── CardInfo.cs
│   │   │   ├── CertificateInfo.cs
│   │   │   └── SubjectName.cs
│   │   └── Exceptions/
│   │       ├── CardException.cs
│   │       ├── EnrollmentException.cs
│   │       └── CaException.cs
│   │
│   ├── OpenMFA.SmartCard/                # PKCS#11/OpenSC wrapper
│   │   ├── OpenMFA.SmartCard.csproj
│   │   ├── Pkcs11/
│   │   │   ├── IPkcs11Provider.cs
│   │   │   ├── Pkcs11Provider.cs         # P/Invoke wrapper
│   │   │   ├── Pkcs11Session.cs
│   │   │   └── Pkcs11Slot.cs
│   │   ├── Piv/
│   │   │   ├── IPivCard.cs
│   │   │   ├── PivCard.cs                # PIV-specific operations
│   │   │   ├── PivCommands.cs            # APDU commands
│   │   │   ├── PivSlots.cs               # Slot constants
│   │   │   └── PivKeyGenerator.cs
│   │   └── PcSc/
│   │       ├── IPcScContext.cs
│   │       ├── PcScContext.cs            # PC/SC context
│   │       └── CardReader.cs
│   │
│   ├── OpenMFA.Cryptography/             # Crypto operations
│   │   ├── OpenMFA.Cryptography.csproj
│   │   ├── CA/
│   │   │   ├── CaKeyGenerator.cs
│   │   │   ├── CertificateSigner.cs
│   │   │   └── CsrParser.cs
│   │   ├── Extensions/
│   │   │   ├── CertificateExtensions.cs
│   │   │   └── KeyExtensions.cs
│   │   └── Utilities/
│   │       ├── SerialNumberGenerator.cs
│   │       └── DistinguishedNameBuilder.cs
│   │
│   ├── OpenMFA.Data/                     # Data access layer
│   │   ├── OpenMFA.Data.csproj
│   │   ├── Context/
│   │   │   └── EnrollmentDbContext.cs    # EF Core context
│   │   ├── Entities/
│   │   │   ├── Enrollment.cs
│   │   │   └── CertificateRecord.cs
│   │   ├── Repositories/
│   │   │   ├── IEnrollmentRepository.cs
│   │   │   └── EnrollmentRepository.cs
│   │   └── Migrations/                   # EF migrations
│   │
│   └── OpenMFA.Configuration/            # Configuration
│       ├── OpenMFA.Configuration.csproj
│       ├── Models/
│       │   ├── OpenMfaSettings.cs
│       │   ├── CaSettings.cs
│       │   ├── PivSettings.cs
│       │   └── DatabaseSettings.cs
│       └── Validators/
│           └── SettingsValidator.cs
│
├── tests/
│   ├── OpenMFA.Core.Tests/
│   │   ├── Services/
│   │   │   ├── EnrollmentServiceTests.cs
│   │   │   └── CertificateAuthorityTests.cs
│   │   └── Models/
│   │
│   ├── OpenMFA.SmartCard.Tests/
│   │   └── Piv/
│   │       └── PivCardTests.cs
│   │
│   └── OpenMFA.Integration.Tests/
│       └── EndToEndEnrollmentTests.cs
│
├── scripts/
│   ├── setup-linux.sh                    # Linux dependencies
│   ├── setup-windows.ps1                 # Windows setup
│   └── init-development.sh               # Dev environment
│
├── config/
│   ├── appsettings.json                  # Default settings
│   ├── appsettings.Development.json
│   └── opensc.conf                       # OpenSC config
│
├── ca/                                   # CA directory (gitignored)
├── data/                                 # Database directory
│
├── .gitignore
├── Directory.Build.props                 # Common build props
├── Directory.Packages.props              # Central package management
├── README.md
├── claude.md
└── ARCHITECTURE.md                       # This file
```

### 3.2 Project Dependencies

```
OpenMFA.CLI
├─> OpenMFA.Core
├─> OpenMFA.Configuration
└─> OpenMFA.Data

OpenMFA.Core
├─> OpenMFA.SmartCard
├─> OpenMFA.Cryptography
├─> OpenMFA.Data
└─> OpenMFA.Configuration

OpenMFA.SmartCard
└─> (System libraries via P/Invoke)

OpenMFA.Cryptography
└─> (System.Security.Cryptography)

OpenMFA.Data
└─> Microsoft.EntityFrameworkCore.Sqlite
```

---

## 4. Technology Stack

### 4.1 Core Technologies

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| Runtime | .NET | 9.0 | Latest LTS runtime |
| Language | C# | 13.0 | Modern language features |
| CLI Framework | System.CommandLine | 2.0.0-beta4 | Command parsing |
| Cryptography | System.Security.Cryptography | Built-in | X.509, RSA, signing |
| Database | SQLite | 3.x | Local storage |
| ORM | Entity Framework Core | 9.0 | Data access |
| Logging | Microsoft.Extensions.Logging | 9.0 | Structured logging |
| DI Container | Microsoft.Extensions.DependencyInjection | 9.0 | IoC container |
| Configuration | Microsoft.Extensions.Configuration | 9.0 | Settings management |
| Testing | xUnit | 2.6+ | Unit testing |
| Mocking | Moq | 4.20+ | Test mocking |

### 4.2 External Dependencies

**System Libraries (Linux)**
- OpenSC (0.23.0+) - PKCS#11 implementation
- pcscd - PC/SC daemon
- OpenSSL (3.0+) - Crypto operations

**System Libraries (Windows)**
- Windows Smart Card Base Components (built-in)
- OpenSC Windows build (optional, for consistency)

---

## 5. Core Component Design

### 5.1 Smart Card Layer (OpenMFA.SmartCard)

#### 5.1.1 PC/SC Context Management

```csharp
public interface IPcScContext : IDisposable
{
    Task<IReadOnlyList<string>> ListReadersAsync(CancellationToken ct = default);
    Task<ICardReader> ConnectAsync(string readerName, CancellationToken ct = default);
}

public interface ICardReader : IDisposable
{
    string Name { get; }
    Task<byte[]> TransmitAsync(byte[] apdu, CancellationToken ct = default);
    Task<CardAtr> GetAtrAsync(CancellationToken ct = default);
    Task ReconnectAsync(CancellationToken ct = default);
}
```

**Implementation Strategy**:
- P/Invoke to winscard.dll (Windows) or libpcsclite.so (Linux)
- Async wrapper over native SCard* functions
- Automatic resource cleanup with IDisposable
- Thread-safe context management

#### 5.1.2 PIV Card Operations

```csharp
public interface IPivCard : IDisposable
{
    Task<bool> SelectPivAppletAsync(CancellationToken ct = default);
    Task VerifyPinAsync(string pin, CancellationToken ct = default);
    Task SetPinAsync(string newPin, CancellationToken ct = default);
    Task<PivPublicKey> GenerateKeyPairAsync(PivKeySlot slot, PivAlgorithm algorithm, CancellationToken ct = default);
    Task StoreCertificateAsync(PivKeySlot slot, byte[] certificateDer, CancellationToken ct = default);
    Task<byte[]> GetCertificateAsync(PivKeySlot slot, CancellationToken ct = default);
    Task<CardCapabilities> GetCapabilitiesAsync(CancellationToken ct = default);
}

public class PivCard : IPivCard
{
    private readonly ICardReader _reader;
    private readonly ILogger<PivCard> _logger;

    public async Task<PivPublicKey> GenerateKeyPairAsync(
        PivKeySlot slot,
        PivAlgorithm algorithm,
        CancellationToken ct = default)
    {
        // APDU: 00 47 00 <slot> <template>
        var apdu = PivCommands.GenerateAsymmetricKeyPair(slot, algorithm);
        var response = await _reader.TransmitAsync(apdu, ct);

        // Parse TLV response to extract public key
        return PivResponseParser.ParsePublicKey(response);
    }
}
```

**Key Design Decisions**:
- All operations async for responsiveness
- APDU commands follow NIST SP 800-73-4
- TLV parsing for PIV data objects
- Proper error handling for card errors (63 Cx, 69 82, etc.)

#### 5.1.3 PKCS#11 Integration

```csharp
public interface IPkcs11Provider : IDisposable
{
    Task<IReadOnlyList<IPkcs11Slot>> GetSlotsAsync(bool tokenPresent, CancellationToken ct = default);
}

public interface IPkcs11Session : IDisposable
{
    Task<byte[]> GenerateKeyPairAsync(Pkcs11MechanismType mechanism, CancellationToken ct = default);
    Task<byte[]> SignAsync(byte[] data, CancellationToken ct = default);
}
```

**Implementation Notes**:
- Dynamic loading of PKCS#11 libraries
- Support for both OpenSC and native Windows CSP
- Fallback mechanism for different environments

---

### 5.2 Certificate Authority Layer (OpenMFA.Cryptography)

#### 5.2.1 CA Service

```csharp
public interface ICertificateAuthority
{
    Task InitializeAsync(CaInitializationOptions options, CancellationToken ct = default);
    Task<X509Certificate2> IssueCertificateAsync(CertificateRequest csr, SubjectName subject, CancellationToken ct = default);
    Task<X509Certificate2> GetCaCertificateAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IssuedCertificate>> ListIssuedCertificatesAsync(CancellationToken ct = default);
}

public class CertificateAuthority : ICertificateAuthority
{
    private readonly CaSettings _settings;
    private readonly ILogger<CertificateAuthority> _logger;

    public async Task InitializeAsync(CaInitializationOptions options, CancellationToken ct)
    {
        // Generate CA private key (RSA 4096)
        using var rsa = RSA.Create(4096);

        // Create self-signed CA certificate
        var request = new CertificateRequest(
            options.SubjectName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add CA extensions
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: true,
                hasPathLengthConstraint: true,
                pathLengthConstraint: 0,
                critical: true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                critical: true));

        // Create self-signed certificate
        var caCert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(10));

        // Save CA certificate and private key
        await SaveCaFilesAsync(caCert, rsa, ct);
    }

    public async Task<X509Certificate2> IssueCertificateAsync(
        CertificateRequest csr,
        SubjectName subject,
        CancellationToken ct)
    {
        // Load CA certificate and private key
        var caCert = await LoadCaCertificateAsync(ct);

        // Build certificate request with public key from CSR
        var request = new CertificateRequest(
            subject.ToString(),
            csr.PublicKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add PIV-required extensions
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.2"), // Client Auth
                    new Oid("1.3.6.1.4.1.311.20.2.2") // Smart Card Logon
                },
                critical: true));

        // Generate serial number
        var serialNumber = SerialNumberGenerator.Generate();

        // Sign certificate
        var issuedCert = request.Create(
            caCert,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(_settings.ValidityDays),
            serialNumber);

        // Log to CA index
        await LogIssuedCertificateAsync(issuedCert, subject, ct);

        return issuedCert;
    }
}
```

**Security Considerations**:
- CA private key stored with file permissions 0600 (Linux) or ACL protection (Windows)
- No password protection on CA key (air-gapped environment assumption)
- SHA-256 signing (FIPS 140-2 approved)
- Proper certificate extension handling

---

### 5.3 Enrollment Service (OpenMFA.Core)

#### 5.3.1 Enrollment Orchestration

```csharp
public interface IEnrollmentService
{
    Task<EnrollmentResult> EnrollAsync(EnrollmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<EnrollmentRecord>> ListEnrollmentsAsync(CancellationToken ct = default);
    Task<EnrollmentRecord> GetEnrollmentAsync(string serialNumber, CancellationToken ct = default);
}

public class EnrollmentService : IEnrollmentService
{
    private readonly ICardService _cardService;
    private readonly ICertificateAuthority _ca;
    private readonly IAuditService _auditService;
    private readonly ILogger<EnrollmentService> _logger;

    public async Task<EnrollmentResult> EnrollAsync(
        EnrollmentRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting enrollment for {Subject}", request.SubjectName);

        try
        {
            // Step 1: Detect and connect to card
            var card = await _cardService.ConnectToCardAsync(ct);

            // Step 2: Set PIN
            await card.SetPinAsync(request.Pin, ct);
            _logger.LogInformation("PIN configured successfully");

            // Step 3: Generate key pair on card
            var publicKey = await card.GenerateKeyPairAsync(
                PivKeySlot.Authentication,
                PivAlgorithm.Rsa2048,
                ct);
            _logger.LogInformation("Key pair generated on card");

            // Step 4: Build CSR
            var csr = BuildCertificateRequest(publicKey, request.SubjectName);

            // Step 5: Issue certificate from CA
            var certificate = await _ca.IssueCertificateAsync(csr, request.SubjectName, ct);
            _logger.LogInformation("Certificate issued: {SerialNumber}", certificate.SerialNumber);

            // Step 6: Write certificate to card
            await card.StoreCertificateAsync(
                PivKeySlot.Authentication,
                certificate.RawData,
                ct);
            _logger.LogInformation("Certificate written to card");

            // Step 7: Audit logging
            await _auditService.LogEnrollmentAsync(new EnrollmentRecord
            {
                Timestamp = DateTimeOffset.UtcNow,
                SerialNumber = certificate.SerialNumber,
                Subject = request.SubjectName.ToString(),
                CardGuid = await card.GetCardGuidAsync(ct),
                Status = EnrollmentStatus.Success
            }, ct);

            return new EnrollmentResult
            {
                Success = true,
                SerialNumber = certificate.SerialNumber,
                Certificate = certificate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enrollment failed");

            await _auditService.LogEnrollmentAsync(new EnrollmentRecord
            {
                Timestamp = DateTimeOffset.UtcNow,
                Subject = request.SubjectName.ToString(),
                Status = EnrollmentStatus.Failed,
                ErrorMessage = ex.Message
            }, ct);

            throw new EnrollmentException("Enrollment failed", ex);
        }
    }

    private CertificateRequest BuildCertificateRequest(PivPublicKey publicKey, SubjectName subject)
    {
        // Convert PIV public key to RSA parameters
        var rsaParams = new RSAParameters
        {
            Modulus = publicKey.Modulus,
            Exponent = publicKey.Exponent
        };

        using var rsa = RSA.Create(rsaParams);

        return new CertificateRequest(
            subject.ToString(),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
    }
}
```

---

### 5.4 Data Access Layer (OpenMFA.Data)

#### 5.4.1 Entity Framework Core Context

```csharp
public class EnrollmentDbContext : DbContext
{
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<CertificateRecord> Certificates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CardGuid).HasMaxLength(36);
            entity.HasIndex(e => e.SerialNumber).IsUnique();
            entity.HasIndex(e => e.Timestamp);
        });

        modelBuilder.Entity<CertificateRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SerialNumber).IsRequired();
            entity.HasOne<Enrollment>()
                .WithMany()
                .HasForeignKey(e => e.EnrollmentId);
        });
    }
}

public class Enrollment
{
    public int Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string SerialNumber { get; set; }
    public string Subject { get; set; }
    public string CardGuid { get; set; }
    public EnrollmentStatus Status { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

### 5.5 CLI Application (OpenMFA.CLI)

#### 5.5.1 Command Structure

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services.AddSingleton<IPcScContext, PcScContext>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<ICertificateAuthority, CertificateAuthority>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Configure EF Core
builder.Services.AddDbContext<EnrollmentDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure settings
builder.Services.Configure<OpenMfaSettings>(
    builder.Configuration.GetSection("OpenMFA"));

var host = builder.Build();

// Build command-line interface
var rootCommand = new RootCommand("OpenMFA - PIV Self-Enrollment Client");

// Add commands
rootCommand.AddCommand(new InitCaCommand(host.Services));
rootCommand.AddCommand(new EnrollCommand(host.Services));
rootCommand.AddCommand(new ListCommand(host.Services));
rootCommand.AddCommand(new ResetCardCommand(host.Services));
rootCommand.AddCommand(new ExportCaCommand(host.Services));

// Execute
return await rootCommand.InvokeAsync(args);
```

#### 5.5.2 Enroll Command

```csharp
public class EnrollCommand : Command
{
    public EnrollCommand(IServiceProvider services) : base("enroll", "Enroll a new PIV card")
    {
        var cnOption = new Option<string>("--cn", "Common Name (required)") { IsRequired = true };
        var emailOption = new Option<string>("--email", "Email address");
        var ouOption = new Option<string>("--ou", "Organizational Unit");
        var pinOption = new Option<string>("--pin", "User PIN (6-8 digits)");

        AddOption(cnOption);
        AddOption(emailOption);
        AddOption(ouOption);
        AddOption(pinOption);

        this.SetHandler(async (context) =>
        {
            var cn = context.ParseResult.GetValueForOption(cnOption);
            var email = context.ParseResult.GetValueForOption(emailOption);
            var ou = context.ParseResult.GetValueForOption(ouOption);
            var pin = context.ParseResult.GetValueForOption(pinOption);

            using var scope = services.CreateScope();
            var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

            // Prompt for PIN if not provided
            if (string.IsNullOrEmpty(pin))
            {
                Console.Write("Enter new PIN (6-8 digits): ");
                pin = ReadPassword();
            }

            // Validate PIN
            if (!PinValidator.IsValid(pin))
            {
                Console.WriteLine("Error: PIN must be 6-8 digits");
                context.ExitCode = 1;
                return;
            }

            // Build enrollment request
            var request = new EnrollmentRequest
            {
                SubjectName = new SubjectName
                {
                    CommonName = cn,
                    Email = email,
                    OrganizationalUnit = ou
                },
                Pin = pin
            };

            try
            {
                Console.WriteLine("Enrolling card...");
                var result = await enrollmentService.EnrollAsync(request, context.GetCancellationToken());

                Console.WriteLine($"✓ Enrollment successful!");
                Console.WriteLine($"  Certificate Serial: {result.SerialNumber}");
                Console.WriteLine($"  Subject: {result.Certificate.Subject}");
                Console.WriteLine($"  Valid Until: {result.Certificate.NotAfter:yyyy-MM-dd}");
            }
            catch (EnrollmentException ex)
            {
                Console.WriteLine($"✗ Enrollment failed: {ex.Message}");
                context.ExitCode = 1;
            }
        });
    }

    private static string ReadPassword()
    {
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        Console.WriteLine();
        return password.ToString();
    }
}
```

---

## 6. Configuration Management

### 6.1 appsettings.json

```json
{
  "OpenMFA": {
    "CA": {
      "Directory": "./ca",
      "ValidityDays": 365,
      "KeySize": 4096,
      "HashAlgorithm": "SHA256",
      "SubjectName": "CN=OpenMFA Root CA, O=OpenMFA, C=US"
    },
    "PIV": {
      "DefaultPinLength": 8,
      "KeySlot": "9A",
      "Algorithm": "RSA2048",
      "CardReaderTimeout": 30000
    },
    "Database": {
      "Provider": "SQLite",
      "MigrationMode": "Automatic"
    },
    "Security": {
      "RequireStrongPins": true,
      "MinPinLength": 6,
      "MaxPinLength": 8,
      "AllowNumericOnly": true
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/enrollments.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "OpenMFA": "Debug",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "File": {
      "Path": "./logs/openmfa-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  }
}
```

---

## 7. Security Architecture

### 7.1 Key Security Principles

1. **Private Key Protection**
   - Keys generated on card, never exported
   - CA private key file permissions: 0600 (owner read/write only)
   - No key material in memory longer than necessary

2. **PIN Security**
   - Never logged or persisted
   - Transmitted only to card via secure channel
   - Minimum complexity requirements enforced

3. **Certificate Security**
   - SHA-256 signatures (FIPS 140-2)
   - Proper key usage extensions
   - Serial number randomness (cryptographically secure RNG)

4. **Audit Trail**
   - All enrollments logged with timestamp
   - Failed attempts recorded
   - Immutable audit log (append-only)

5. **Air-Gap Compliance**
   - Zero network calls
   - No telemetry or analytics
   - All operations local

### 7.2 Threat Model

| Threat | Mitigation |
|--------|-----------|
| CA key compromise | File permissions, physical security |
| Card cloning | Private keys never exportable |
| PIN brute force | Card lockout after failed attempts |
| Certificate forgery | Strong signatures, serial tracking |
| Audit tampering | Database integrity checks, backups |

---

## 8. Testing Strategy

### 8.1 Unit Tests
- All service classes with mocked dependencies
- Cryptographic operations (key generation, signing)
- APDU command construction
- Configuration validation

### 8.2 Integration Tests
- End-to-end enrollment flow (with test card)
- CA initialization and certificate issuance
- Database operations
- Card reader detection

### 8.3 Manual Test Scenarios
```
1. Fresh Installation
   - Run init-ca
   - Verify CA files created
   - Verify database initialized

2. First Enrollment
   - Insert blank card
   - Run enroll command
   - Verify certificate on card
   - Verify audit log entry

3. Multiple Enrollments
   - Enroll 10 cards
   - List enrollments
   - Verify no serial collisions

4. Error Handling
   - No card inserted
   - Invalid PIN
   - Card removed during enrollment
   - Corrupted CA key
```

---

## 9. Build & Deployment

### 9.1 Build Configuration

**Directory.Build.props**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

**Directory.Packages.props** (Central Package Management)
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageVersion Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
    <PackageVersion Include="xunit" Version="2.6.3" />
    <PackageVersion Include="Moq" Version="4.20.70" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="5.0.0" />
  </ItemGroup>
</Project>
```

### 9.2 Publish Profiles

**Self-Contained Linux x64**
```bash
dotnet publish src/OpenMFA.CLI/OpenMFA.CLI.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -o ./publish/linux-x64
```

**Self-Contained Windows x64**
```powershell
dotnet publish src/OpenMFA.CLI/OpenMFA.CLI.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -o ./publish/win-x64
```

### 9.3 Installation Package Structure

```
openmfa-1.0.0-linux-x64/
├── openmfa                    # Executable
├── appsettings.json
├── config/
│   └── opensc.conf
├── scripts/
│   └── install-dependencies.sh
└── README.txt

openmfa-1.0.0-win-x64/
├── openmfa.exe
├── appsettings.json
└── README.txt
```

---

## 10. Development Roadmap

### Phase 1: Foundation (Week 1-2)
- [ ] Solution structure setup
- [ ] Core models and interfaces
- [ ] Configuration system
- [ ] Logging infrastructure
- [ ] Database schema with migrations

### Phase 2: Smart Card Layer (Week 3-4)
- [ ] PC/SC wrapper implementation
- [ ] PIV APDU commands
- [ ] Card reader detection
- [ ] Key generation on card
- [ ] Certificate storage operations

### Phase 3: Cryptography Layer (Week 5-6)
- [ ] CA initialization
- [ ] Certificate signing
- [ ] CSR parsing
- [ ] X.509 extensions
- [ ] Serial number generation

### Phase 4: Business Logic (Week 7-8)
- [ ] Enrollment service
- [ ] Card service
- [ ] Audit service
- [ ] Error handling and validation

### Phase 5: CLI Interface (Week 9-10)
- [ ] Command implementation
- [ ] User input handling
- [ ] Progress indicators
- [ ] Error messaging
- [ ] Help system

### Phase 6: Testing & Polish (Week 11-12)
- [ ] Unit test coverage (>80%)
- [ ] Integration tests
- [ ] Manual testing with real cards
- [ ] Documentation
- [ ] Packaging and distribution

---

## 11. Performance Considerations

### 11.1 Expected Operations

| Operation | Expected Time | Notes |
|-----------|--------------|-------|
| CA Initialization | 2-5 seconds | RSA 4096 key generation |
| Card Detection | 100-500ms | PC/SC enumeration |
| On-Card Key Gen | 10-30 seconds | RSA 2048 on smart card |
| Certificate Signing | 100-200ms | RSA signature |
| Certificate Write | 500-1000ms | APDU communication |
| **Total Enrollment** | **15-40 seconds** | Dominated by key gen |

### 11.2 Optimization Strategies
- Async I/O for all card operations
- Connection pooling for database
- Lazy loading of CA certificate
- Caching of configuration settings

---

## 12. Compliance & Standards

### 12.1 NIST SP 800-73-4 Compliance

| Requirement | Implementation |
|-------------|----------------|
| PIV Applet Selection | AID: A000000308000010000100 |
| Authentication Key (9A) | RSA 2048-bit |
| PIN Format | 6-8 numeric digits |
| Certificate Slot | 5FC105 (PIV Authentication) |
| Key Generation | On-card (CKM_RSA_PKCS_KEY_PAIR_GEN) |

### 12.2 PKCS#11 v2.40 Support
- CKM_RSA_PKCS_KEY_PAIR_GEN mechanism
- CKA_TOKEN attribute for persistent keys
- CKA_PRIVATE for key protection

---

## 13. Future Enhancements (Post-MVP)

1. **Multi-Card Support**
   - Batch enrollment workflows
   - Queue management

2. **Advanced Key Algorithms**
   - ECDSA P-256, P-384
   - RSA 4096

3. **Additional PIV Slots**
   - Digital Signature (9C)
   - Key Management (9D)
   - Card Authentication (9E)

4. **Certificate Revocation**
   - CRL generation
   - OCSP responder (for connected environments)

5. **GUI Application**
   - Avalonia UI for cross-platform desktop
   - Self-service kiosk mode

6. **Hardware Security Module (HSM)**
   - HSM-backed CA private key
   - PKCS#11 HSM integration

7. **Enhanced Reporting**
   - Enrollment statistics
   - Certificate expiration tracking
   - Export to CSV/PDF

---

## 14. Summary

This architecture provides a robust, secure, and maintainable implementation of the OpenMFA PIV self-enrollment system using .NET 9 and C#. Key highlights:

- **Modern Stack**: .NET 9, C# 13, Entity Framework Core 9
- **Clean Architecture**: Separated concerns with clear layer boundaries
- **Security-First**: Air-gapped design, on-card key generation, proper cryptography
- **Testable**: Dependency injection, interface-based design, comprehensive test strategy
- **Standards-Compliant**: NIST SP 800-73-4, ISO 7816, PKCS#11 v2.40
- **Production-Ready**: Logging, error handling, configuration management, audit trails

The implementation follows .NET best practices, modern C# patterns, and security guidelines suitable for high-assurance environments.

---

**Document Version**: 1.0
**Last Updated**: 2025-12-20
**Target .NET Version**: 9.0
**Status**: Architecture Design Complete - Ready for Implementation
