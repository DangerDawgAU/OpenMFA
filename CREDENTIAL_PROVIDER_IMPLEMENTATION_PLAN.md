# OpenMFA Credential Provider Implementation Plan

## Project Overview

Build a Windows Credential Provider that enables smart card logon on standalone/workgroup Windows computers, integrated with OpenMFA's existing smart card management capabilities.

**Goal:** Allow users to log into Windows using MyEID smart cards initialized by OpenMFA, without requiring Active Directory.

**Based on:** EIDAuthentication open-source implementation (LGPL 2.1)

---

## Architecture Overview

### Components to Build

```
┌─────────────────────────────────────────────────────────────────┐
│                         OpenMFA Solution                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────┐         ┌──────────────────────────┐      │
│  │   OpenMFA.GUI    │◄────────┤ OpenMFA.SmartCard        │      │
│  │  (Existing)      │         │ (Existing)               │      │
│  └────────┬─────────┘         └──────────────────────────┘      │
│           │                                                       │
│           │ Calls Registration                                   │
│           ▼                                                       │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │   OpenMFA.CredentialProvider (NEW)                       │   │
│  │   - User registration management                         │   │
│  │   - Certificate-to-user mapping storage                  │   │
│  │   - Configuration wizard integration                     │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
                            │
                            │ P/Invoke or COM
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Native C++ DLLs (NEW)                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌────────────────────────────────────────────────────────┐     │
│  │  OpenMFACredentialProvider.dll                         │     │
│  │  - ICredentialProvider interface                       │     │
│  │  - ICredentialProviderCredential interface             │     │
│  │  - Smart card enumeration                              │     │
│  │  - PIN collection UI                                   │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                   │
│  ┌────────────────────────────────────────────────────────┐     │
│  │  OpenMFAAuthenticationPackage.dll                      │     │
│  │  - LSA Authentication Package                          │     │
│  │  - LsaApLogonUser implementation                       │     │
│  │  - Certificate validation                              │     │
│  │  - Token creation                                      │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                   │
│  ┌────────────────────────────────────────────────────────┐     │
│  │  OpenMFACardLibrary.dll                                │     │
│  │  - Smart card communication                            │     │
│  │  - Certificate retrieval                               │     │
│  │  - Credential storage (LSA Secrets)                    │     │
│  │  - User mapping logic                                  │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
                            │
                            │ Windows APIs
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Windows Services                            │
├─────────────────────────────────────────────────────────────────┤
│  • lsass.exe (Authentication Package runs here)                 │
│  • Smart Card Service (SCardSvr)                                │
│  • Windows Smart Card API (Winscard.dll)                        │
│  • OpenSC Minidriver (via Smart Card API)                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Development Phases

### Phase 1: Foundation & Research (Week 1-2)

**Objective:** Set up development environment and understand Windows credential provider architecture.

**Tasks:**
1. Install and configure development tools
2. Study EIDAuthentication source code thoroughly
3. Build and test EIDAuthentication
4. Create minimal credential provider sample
5. Test sample on clean VM

### Phase 2: Core Library Implementation (Week 3-4)

**Objective:** Build the smart card communication and credential storage library.

**Tasks:**
1. Create OpenMFACardLibrary project
2. Implement smart card enumeration
3. Implement certificate reading from cards
4. Implement LSA Secret storage for certificate mappings
5. Implement user enumeration and mapping

### Phase 3: Authentication Package (Week 5-6)

**Objective:** Build the LSA authentication package that validates credentials.

**Tasks:**
1. Create OpenMFAAuthenticationPackage project
2. Implement LsaApInitializePackage
3. Implement LsaApLogonUser
4. Implement certificate validation logic
5. Implement Windows token creation
6. Add comprehensive logging

### Phase 4: Credential Provider UI (Week 7-8)

**Objective:** Build the credential provider that appears at Windows logon.

**Tasks:**
1. Create OpenMFACredentialProvider project
2. Implement ICredentialProvider interface
3. Implement ICredentialProviderCredential interface
4. Create PIN collection UI
5. Implement smart card detection and enumeration
6. Handle credential serialization

### Phase 5: .NET Integration (Week 9-10)

**Objective:** Integrate credential provider with existing OpenMFA .NET application.

**Tasks:**
1. Create OpenMFA.CredentialProvider .NET project
2. Implement P/Invoke wrappers for native DLLs
3. Add user registration UI to OpenMFA.GUI
4. Implement certificate-to-user mapping in GUI
5. Add configuration wizard workflow

### Phase 6: Testing & Hardening (Week 11-12)

**Objective:** Comprehensive testing and security hardening.

**Tasks:**
1. Unit tests for all components
2. Integration testing on multiple Windows versions
3. Security audit
4. Performance optimization
5. Error handling improvements

### Phase 7: Deployment & Documentation (Week 13-14)

**Objective:** Create installer and documentation.

**Tasks:**
1. Create WiX installer
2. Write user documentation
3. Write developer documentation
4. Create video tutorials
5. Beta testing

---

## Detailed Implementation Plan

### Part 1: OpenMFACardLibrary.dll

**Purpose:** Core library for smart card communication and credential management.

**Key Classes:**

#### 1. CStoredCredentialManager
```cpp
class CStoredCredentialManager {
public:
    // Store certificate-to-user mapping in LSA Secret
    static BOOL StoreCredential(
        DWORD dwUserRid,           // User's RID from NetUserGetInfo
        PCCERT_CONTEXT pCertContext, // Certificate from smart card
        PBYTE pbHash               // SHA1 hash of certificate
    );

    // Retrieve username from certificate
    static BOOL GetUsernameFromCertContext(
        PCCERT_CONTEXT pContext,
        PWSTR* pszUsername,
        PDWORD pdwRid
    );

    // Check if certificate has stored credential
    static BOOL HasStoredCredential(PCCERT_CONTEXT pContext);

    // Remove stored credential
    static BOOL RemoveCredential(DWORD dwUserRid);

private:
    static BOOL RetrievePrivateData(DWORD dwRid, PEID_PRIVATE_DATA* ppData);
    static BOOL StorePrivateData(DWORD dwRid, PEID_PRIVATE_DATA pData);
};
```

**Storage Format:**
```cpp
typedef struct _EID_PRIVATE_DATA {
    DWORD dwVersion;              // Structure version
    BYTE Hash[CERT_HASH_LENGTH];  // SHA1 hash of certificate
    DWORD dwCertificatOffset;     // Offset to certificate data
    DWORD dwCertificatSize;       // Size of certificate data
    BYTE Data[1];                 // Variable length: certificate DER bytes
} EID_PRIVATE_DATA, *PEID_PRIVATE_DATA;
```

**LSA Secret Name:** `L$_OPENMFA_{RID}` where RID is user's relative identifier

#### 2. CSmartCardEnumerator
```cpp
class CSmartCardEnumerator {
public:
    // Enumerate all smart cards with certificates
    static BOOL EnumerateSmartCards(
        std::vector<CSmartCardInfo*>& cards
    );

    // Get certificate from specific card
    static BOOL GetCertificateFromCard(
        LPCWSTR szReaderName,
        PCCERT_CONTEXT* ppCertContext
    );

    // Validate PIN
    static BOOL ValidatePin(
        LPCWSTR szReaderName,
        LPCWSTR szPin,
        PCCERT_CONTEXT pCertContext
    );
};
```

#### 3. CertificateUtilities
```cpp
namespace CertificateUtilities {
    // Get certificate hash
    BOOL GetCertificateHash(
        PCCERT_CONTEXT pCertContext,
        PBYTE pbHash,
        DWORD cbHash
    );

    // Validate certificate (check not expired, etc.)
    BOOL ValidateCertificate(PCCERT_CONTEXT pCertContext);

    // Get certificate subject name
    BOOL GetCertificateSubject(
        PCCERT_CONTEXT pCertContext,
        PWSTR szSubject,
        DWORD cchSubject
    );
}
```

### Part 2: OpenMFAAuthenticationPackage.dll

**Purpose:** LSA Authentication Package that validates credentials and creates logon sessions.

**Key Functions:**

#### LsaApInitializePackage
```cpp
NTSTATUS NTAPI LsaApInitializePackage(
    ULONG AuthenticationPackageId,
    PLSA_DISPATCH_TABLE LsaDispatchTable,
    PLSA_STRING Database,
    PLSA_STRING Confidentiality,
    PLSA_STRING* AuthenticationPackageName
)
{
    // 1. Store dispatch table for later use
    // 2. Set package name: "OpenMFA Authentication Package"
    // 3. Initialize logging
    // 4. Register for smart card notifications
    // 5. Return STATUS_SUCCESS
}
```

#### LsaApLogonUser
```cpp
NTSTATUS NTAPI LsaApLogonUser(
    PLSA_CLIENT_REQUEST ClientRequest,
    SECURITY_LOGON_TYPE LogonType,
    PVOID AuthenticationInformation,
    PVOID ClientAuthenticationBase,
    ULONG AuthenticationInformationLength,
    PVOID* ProfileBuffer,
    PULONG ProfileBufferLength,
    PLUID LogonId,
    PNTSTATUS SubStatus,
    PLSA_TOKEN_INFORMATION_TYPE TokenInformationType,
    PVOID* TokenInformation,
    PLSA_UNICODE_STRING* AccountName,
    PLSA_UNICODE_STRING* AuthenticatingAuthority
)
{
    // 1. Parse authentication information (certificate hash + PIN)
    // 2. Call CStoredCredentialManager::GetUsernameFromCertContext()
    // 3. If user found, validate PIN with smart card
    // 4. If PIN valid, get user info via NetUserGetInfo()
    // 5. Build LSA_TOKEN_INFORMATION_V1 with user SID and groups
    // 6. Return token information
    // 7. Return STATUS_SUCCESS or appropriate error
}
```

**Authentication Information Structure:**
```cpp
typedef struct _OPENMFA_LOGON_REQUEST {
    DWORD dwVersion;
    BYTE CertificateHash[CERT_HASH_LENGTH];
    USHORT PinLength;
    WCHAR Pin[256];
} OPENMFA_LOGON_REQUEST, *POPENMFA_LOGON_REQUEST;
```

### Part 3: OpenMFACredentialProvider.dll

**Purpose:** Credential provider that appears at Windows logon screen.

**Key Classes:**

#### COpenMFAProvider
```cpp
class COpenMFAProvider : public ICredentialProvider {
public:
    // ICredentialProvider
    IFACEMETHODIMP SetUsageScenario(
        CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus,
        DWORD dwFlags
    );

    IFACEMETHODIMP SetSerialization(
        const CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs
    );

    IFACEMETHODIMP Advise(
        ICredentialProviderEvents* pcpe,
        UINT_PTR upAdviseContext
    );

    IFACEMETHODIMP UnAdvise();

    IFACEMETHODIMP GetFieldDescriptorCount(DWORD* pdwCount);

    IFACEMETHODIMP GetFieldDescriptorAt(
        DWORD dwIndex,
        CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** ppcpfd
    );

    IFACEMETHODIMP GetCredentialCount(
        DWORD* pdwCount,
        DWORD* pdwDefault,
        BOOL* pbAutoLogonWithDefault
    );

    IFACEMETHODIMP GetCredentialAt(
        DWORD dwIndex,
        ICredentialProviderCredential** ppcpc
    );

private:
    std::vector<COpenMFACredential*> _credentials;
    CREDENTIAL_PROVIDER_USAGE_SCENARIO _cpus;
};
```

#### COpenMFACredential
```cpp
class COpenMFACredential : public ICredentialProviderCredential {
public:
    // ICredentialProviderCredential
    IFACEMETHODIMP Advise(ICredentialProviderCredentialEvents* pcpce);
    IFACEMETHODIMP UnAdvise();

    IFACEMETHODIMP SetSelected(BOOL* pbAutoLogon);
    IFACEMETHODIMP SetDeselected();

    IFACEMETHODIMP GetFieldState(
        DWORD dwFieldID,
        CREDENTIAL_PROVIDER_FIELD_STATE* pcpfs,
        CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE* pcpfis
    );

    IFACEMETHODIMP GetStringValue(DWORD dwFieldID, LPWSTR* ppwsz);
    IFACEMETHODIMP GetBitmapValue(DWORD dwFieldID, HBITMAP* phbmp);
    IFACEMETHODIMP GetCheckboxValue(DWORD dwFieldID, BOOL* pbChecked, LPWSTR* ppwszLabel);
    IFACEMETHODIMP GetComboBoxValueCount(DWORD dwFieldID, DWORD* pcItems, DWORD* pdwSelectedItem);
    IFACEMETHODIMP GetComboBoxValueAt(DWORD dwFieldID, DWORD dwItem, LPWSTR* ppwszItem);
    IFACEMETHODIMP GetSubmitButtonValue(DWORD dwFieldID, DWORD* pdwAdjacentTo);

    IFACEMETHODIMP SetStringValue(DWORD dwFieldID, LPCWSTR pwz);
    IFACEMETHODIMP SetCheckboxValue(DWORD dwFieldID, BOOL bChecked);
    IFACEMETHODIMP SetComboBoxSelectedValue(DWORD dwFieldID, DWORD dwSelectedItem);
    IFACEMETHODIMP CommandLinkClicked(DWORD dwFieldID);

    IFACEMETHODIMP GetSerialization(
        CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* pcpgsr,
        CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs,
        LPWSTR* ppwszOptionalStatusText,
        CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon
    );

    IFACEMETHODIMP ReportResult(
        NTSTATUS ntsStatus,
        NTSTATUS ntsSubstatus,
        LPWSTR* ppwszOptionalStatusText,
        CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon
    );

private:
    BYTE _certHash[CERT_HASH_LENGTH];
    WCHAR _wszReaderName[256];
    WCHAR _wszPin[256];
    WCHAR _wszUsername[256];
};
```

**UI Fields:**
```cpp
enum OPENMFA_FIELD_ID {
    OFI_LARGE_TEXT = 0,      // "OpenMFA Smart Card Logon"
    OFI_SMALL_TEXT,          // "Insert smart card and enter PIN"
    OFI_READER_COMBOBOX,     // Dropdown of available smart cards
    OFI_PIN_EDIT,            // PIN entry field
    OFI_SUBMIT_BUTTON,       // "Sign In" button
    OFI_NUM_FIELDS
};
```

### Part 4: OpenMFA.CredentialProvider (.NET Project)

**Purpose:** .NET wrapper and integration with OpenMFA GUI.

**Key Classes:**

#### CredentialProviderManager
```csharp
public class CredentialProviderManager
{
    // Register user with their certificate
    public bool RegisterUser(
        string username,
        byte[] certificateDer)
    {
        // 1. Get user RID via NetUserGetInfo P/Invoke
        // 2. Call native StoreCredential function
        // 3. Return success/failure
    }

    // Unregister user
    public bool UnregisterUser(string username)
    {
        // 1. Get user RID
        // 2. Call native RemoveCredential function
        // 3. Return success/failure
    }

    // Check if user is registered
    public bool IsUserRegistered(
        string username,
        out byte[] certificateHash)
    {
        // 1. Get user RID
        // 2. Call native HasStoredCredential function
        // 3. Return registration status
    }

    // List all registered users
    public List<RegisteredUser> GetRegisteredUsers()
    {
        // 1. Enumerate Windows users
        // 2. Check each for stored credential
        // 3. Return list
    }
}
```

#### NativeMethods (P/Invoke)
```csharp
internal static class NativeMethods
{
    const string CARD_LIBRARY = "OpenMFACardLibrary.dll";

    [DllImport(CARD_LIBRARY, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StoreCredential(
        uint dwUserRid,
        IntPtr pCertContext,
        byte[] pbHash);

    [DllImport(CARD_LIBRARY, CallingConvention = CallingConvention.StdCall)]
    public static extern bool RemoveCredential(uint dwUserRid);

    [DllImport(CARD_LIBRARY, CallingConvention = CallingConvention.StdCall)]
    public static extern bool HasStoredCredential(IntPtr pCertContext);

    // Windows API imports
    [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
    public static extern int NetUserGetInfo(
        string servername,
        string username,
        int level,
        out IntPtr bufptr);

    [DllImport("netapi32.dll")]
    public static extern int NetApiBufferFree(IntPtr buffer);
}
```

### Part 5: OpenMFA.GUI Integration

**New UI Components:**

#### 1. Windows Logon Tab
```csharp
public class WindowsLogonForm : Form
{
    private Button btnRegisterUser;
    private Button btnUnregisterUser;
    private ListView lvRegisteredUsers;
    private Label lblStatus;

    private async void BtnRegisterUser_Click(object sender, EventArgs e)
    {
        // 1. Check if card is detected
        // 2. Check if certificate exists on card
        // 3. Prompt for Windows username
        // 4. Validate username exists
        // 5. Export certificate from card
        // 6. Call CredentialProviderManager.RegisterUser()
        // 7. Show success/error message
        // 8. Refresh registered users list
    }

    private async void BtnUnregisterUser_Click(object sender, EventArgs e)
    {
        // 1. Get selected user from list
        // 2. Confirm with user
        // 3. Call CredentialProviderManager.UnregisterUser()
        // 4. Show success/error message
        // 5. Refresh registered users list
    }

    private void RefreshRegisteredUsers()
    {
        // 1. Call CredentialProviderManager.GetRegisteredUsers()
        // 2. Update ListView with results
    }
}
```

#### 2. User Registration Dialog
```csharp
public class UserRegistrationDialog : Form
{
    private ComboBox cmbWindowsUsers;
    private TextBox txtCertificateInfo;
    private Button btnRegister;
    private Button btnCancel;

    private void LoadWindowsUsers()
    {
        // Enumerate local Windows users
        // Populate combobox
        // Exclude already-registered users
    }

    private void BtnRegister_Click(object sender, EventArgs e)
    {
        // Validate selection
        // Perform registration
        // Close dialog with OK result
    }
}
```

---

## File Structure

```
OpenMFA/
├── src/
│   ├── OpenMFA.GUI/                  (Existing - add Windows Logon tab)
│   ├── OpenMFA.SmartCard/            (Existing - no changes needed)
│   ├── OpenMFA.CredentialProvider/   (NEW - .NET wrapper)
│   │   ├── CredentialProviderManager.cs
│   │   ├── NativeMethods.cs
│   │   ├── RegisteredUser.cs
│   │   └── OpenMFA.CredentialProvider.csproj
│   │
│   └── Native/                       (NEW - C++ projects)
│       ├── OpenMFACardLibrary/
│       │   ├── CStoredCredentialManager.cpp
│       │   ├── CStoredCredentialManager.h
│       │   ├── CSmartCardEnumerator.cpp
│       │   ├── CSmartCardEnumerator.h
│       │   ├── CertificateUtilities.cpp
│       │   ├── CertificateUtilities.h
│       │   ├── Tracing.cpp
│       │   ├── Tracing.h
│       │   ├── dllmain.cpp
│       │   └── OpenMFACardLibrary.vcxproj
│       │
│       ├── OpenMFAAuthenticationPackage/
│       │   ├── LsaFunctions.cpp
│       │   ├── LsaFunctions.h
│       │   ├── AuthenticationLogic.cpp
│       │   ├── AuthenticationLogic.h
│       │   ├── dllmain.cpp
│       │   ├── OpenMFAAuthenticationPackage.def
│       │   └── OpenMFAAuthenticationPackage.vcxproj
│       │
│       ├── OpenMFACredentialProvider/
│       │   ├── COpenMFAProvider.cpp
│       │   ├── COpenMFAProvider.h
│       │   ├── COpenMFACredential.cpp
│       │   ├── COpenMFACredential.h
│       │   ├── helpers.cpp
│       │   ├── helpers.h
│       │   ├── guid.cpp
│       │   ├── Dll.cpp
│       │   ├── OpenMFACredentialProvider.def
│       │   ├── OpenMFACredentialProvider.rc
│       │   ├── Register.reg
│       │   ├── Unregister.reg
│       │   └── OpenMFACredentialProvider.vcxproj
│       │
│       └── OpenMFA.Native.sln
│
├── installer/                        (NEW - WiX installer)
│   ├── OpenMFAInstaller.wxs
│   ├── CredentialProviderCA.js       (Custom actions)
│   └── OpenMFAInstaller.wixproj
│
├── docs/
│   ├── CredentialProviderDevelopment.md
│   ├── UserGuide.md
│   └── TroubleshootingGuide.md
│
└── NOUPLOAD/
    └── EIDAuthentication/            (Reference implementation)
```

---

## Registry Configuration

### Credential Provider Registration
```
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\
    {YOUR-GUID-HERE}
        (Default) = "OpenMFA Credential Provider"
```

### Authentication Package Registration
```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa
    Authentication Packages = REG_MULTI_SZ
        Add: "OpenMFAAuthenticationPackage"
```

### LSA Secret Storage
```
HKEY_LOCAL_MACHINE\SECURITY\Policy\Secrets\
    L$_OPENMFA_{RID}\
        CurrVal\(Default) = (encrypted binary data)
```

---

## Development Environment Setup

### Prerequisites

1. **Visual Studio 2022** with:
   - Desktop development with C++
   - .NET desktop development
   - Windows SDK (latest)
   - ATL/MFC libraries

2. **Windows Driver Kit (WDK)** or **Windows SDK**:
   - For LSA headers (ntsecapi.h, ntsecpkg.h)

3. **WiX Toolset 3.11+**:
   - For creating installer
   - https://wixtoolset.org/

4. **Test Environment**:
   - Windows 10/11 VM with snapshots
   - Smart card reader
   - MyEID test cards

### Build Configuration

**Platform:** x64 (64-bit Windows)
**Configuration:** Debug and Release

**C++ Projects:**
- Platform Toolset: v143 (Visual Studio 2022)
- C++ Language Standard: C++17
- Character Set: Unicode

**Output Paths:**
- `$(SolutionDir)bin\$(Platform)\$(Configuration)\`

---

## Security Considerations

### Code Signing

**CRITICAL:** All DLLs must be code-signed for production:
1. Credential Provider DLL
2. Authentication Package DLL
3. Card Library DLL
4. Installer MSI

**Testing:** Use test-signing mode during development:
```cmd
bcdedit /set testsigning on
```

### LSA Protection

Windows 10+ has LSA protection. During development:
```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa
    RunAsPPL = 0  (disable LSA protection for testing)
```

**Production:** Must work with LSA protection enabled (RunAsPPL=1).

### Credential Storage Security

1. **Encryption:** Use Windows DPAPI for additional encryption
2. **Access Control:** Restrict LSA secret access
3. **PIN Handling:** Never log or store PINs
4. **Certificate Validation:** Validate expiration, trust chain

---

## Testing Strategy

### Unit Tests

**OpenMFACardLibrary.Tests:**
- Test certificate storage and retrieval
- Test user enumeration
- Test certificate comparison logic
- Mock Windows APIs

**OpenMFA.CredentialProvider.Tests:**
- Test P/Invoke wrappers
- Test user registration/unregistration
- Test error handling

### Integration Tests

1. **VM Test Matrix:**
   - Windows 10 21H2 (Clean install)
   - Windows 10 22H2 (With existing users)
   - Windows 11 22H2 (Domain-joined)
   - Windows 11 23H2 (Workgroup)

2. **Test Scenarios:**
   - Register user with smart card
   - Log in with smart card
   - Log in with password (should still work)
   - Multiple users on same computer
   - Card removed during logon
   - Wrong PIN handling
   - Expired certificate handling
   - Unregister user

3. **Security Tests:**
   - Attempt to bypass authentication
   - Test with modified LSA secrets
   - Test with invalid certificates
   - Memory dump analysis (PIN not in memory)

### Performance Tests

- Logon time vs password logon
- Credential enumeration time
- Smart card detection latency

---

## Debugging

### Enable Detailed Logging

```cpp
// In Tracing.h
#define ENABLE_DETAILED_LOGGING 1
#define LOG_FILE L"C:\\OpenMFA\\Logs\\CredentialProvider.log"
```

**Log Locations:**
- Credential Provider: `C:\OpenMFA\Logs\CredentialProvider.log`
- Authentication Package: `C:\OpenMFA\Logs\AuthPackage.log`
- Application logs: Windows Event Viewer

### Debugging Authentication Package

**Challenge:** Authentication package runs in lsass.exe (protected process)

**Solutions:**
1. Use `OutputDebugString()` and DebugView
2. Write to log files
3. Use Windows Event Tracing (ETW)

**Attach Debugger to lsass.exe:**
```cmd
# Enable debugging (requires admin)
gflags -i lsass.exe +d9c

# Attach WinDbg
windbg -pn lsass.exe
```

**WARNING:** Crashing lsass.exe will reboot Windows!

### Debugging Credential Provider

**Test Credential Provider UI:**
```cmd
# Use credential provider test tool
"%WindowsSdkDir%\bin\%WindowsSDKVersion%\x64\CredentialProviderTest.exe"
```

### Remote Debugging

Use **Remote Desktop** for testing, NOT VM console (credential providers behave differently).

---

## Installation Process

### Manual Installation (Development)

1. **Copy DLLs:**
```cmd
copy OpenMFACardLibrary.dll %SystemRoot%\System32\
copy OpenMFAAuthenticationPackage.dll %SystemRoot%\System32\
copy OpenMFACredentialProvider.dll %SystemRoot%\System32\
```

2. **Register Credential Provider:**
```cmd
reg import OpenMFACredentialProvider\Register.reg
```

3. **Register Authentication Package:**
```cmd
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Lsa" /v "Authentication Packages" /t REG_MULTI_SZ /d "msv1_0\0OpenMFAAuthenticationPackage" /f
```

4. **Restart Computer** (Required for LSA changes)

### Automated Installation (Production)

**WiX Installer will:**
1. Check prerequisites (Windows version, .NET runtime)
2. Install DLLs to System32
3. Register credential provider
4. Add authentication package
5. Create log directories
6. Install OpenMFA.GUI with credential provider integration
7. Prompt for restart

**Custom Actions:**
- Pre-install: Check for conflicting credential providers
- Post-install: Verify registration
- Pre-uninstall: Check for registered users (warn)
- Post-uninstall: Clean up LSA secrets

---

## Troubleshooting Guide

### Common Issues

#### Issue 1: Credential Provider Not Appearing
**Symptoms:** OpenMFA option doesn't show at logon screen

**Debugging:**
1. Check registry: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers`
2. Verify DLL exists in System32
3. Check Event Viewer: Application and System logs
4. Run: `eventvwr.msc` → Windows Logs → System
5. Look for credential provider errors

**Solutions:**
- Re-register credential provider
- Verify DLL is not blocked (Properties → Unblock)
- Check DLL dependencies: `dumpbin /dependents OpenMFACredentialProvider.dll`

#### Issue 2: Authentication Fails
**Symptoms:** PIN accepted but login fails

**Debugging:**
1. Check authentication package logs
2. Verify LSA secret exists: `reg query HKLM\SECURITY\Policy\Secrets` (needs SYSTEM privileges)
3. Check certificate on card matches stored certificate
4. Verify user account is enabled

**Solutions:**
- Re-register user
- Check certificate expiration
- Verify smart card service is running: `sc query SCardSvr`

#### Issue 3: Wrong PIN Error
**Symptoms:** Correct PIN rejected

**Debugging:**
1. Test PIN with: `pkcs15-tool --verify-pin --reader 0 --pin 1111`
2. Check card is not locked
3. Verify PIN counter: `pkcs15-tool --list-pins --reader 0`

**Solutions:**
- Unlock card with PUK
- Reset PIN via OpenMFA GUI
- Check card is not in error state

---

## Performance Optimization

### Credential Enumeration

**Goal:** < 500ms to enumerate and display credentials

**Optimizations:**
1. Cache smart card detection results
2. Use asynchronous smart card enumeration
3. Lazy-load certificate details
4. Implement timeout for unresponsive cards

### Authentication

**Goal:** < 2 seconds from PIN entry to desktop

**Optimizations:**
1. Pre-load certificate comparison data
2. Optimize LSA secret retrieval
3. Use indexed lookup for user mapping
4. Minimize registry access

### Memory Usage

**Goal:** < 10 MB additional memory in lsass.exe

**Optimizations:**
1. Free certificate contexts immediately after use
2. Use stack allocation where possible
3. Implement resource pooling
4. Profile with Windows Performance Toolkit

---

## Compliance & Licensing

### LGPL 2.1 Compliance

Since EIDAuthentication is LGPL 2.1:

**Requirements:**
1. **Include LGPL license** text in distribution
2. **Provide source code** access for LGPL portions
3. **Document modifications** made to LGPL code
4. **Allow relinking** with modified LGPL libraries

**Recommended Approach:**
- OpenMFA application: Your own license (MIT, proprietary, etc.)
- Native DLLs: LGPL 2.1 (if derived from EIDAuthentication)
- OR: Clean-room implementation (no LGPL obligations)

**Documentation:**
```
OpenMFA uses libraries derived from EIDAuthentication by Vincent Le Toux,
licensed under GNU Lesser General Public License version 2.1.

Full LGPL 2.1 license text: [Include LICENSE-LGPL.txt]
Source code available at: [Your repository URL]
```

---

## Deployment Checklist

### Pre-Release

- [ ] All components code-signed
- [ ] Tested on all supported Windows versions
- [ ] Security audit completed
- [ ] Performance benchmarks met
- [ ] Documentation complete
- [ ] Installer tested (install/upgrade/uninstall)
- [ ] License compliance verified
- [ ] Beta testing with 10+ users

### Release Package

- [ ] Installer MSI
- [ ] User documentation PDF
- [ ] Administrator guide PDF
- [ ] Troubleshooting guide
- [ ] License files
- [ ] Release notes
- [ ] Code signing certificates validated

### Post-Release

- [ ] Monitor for issues
- [ ] Establish update mechanism
- [ ] Plan for Windows updates compatibility
- [ ] Set up telemetry/crash reporting (optional)

---

## Maintenance & Updates

### Windows Updates

**Risk:** Windows updates can break credential providers

**Mitigation:**
1. Test on Windows Insider builds
2. Maintain compatibility matrix
3. Automated testing on Windows Update releases
4. Quick patch mechanism

### Certificate Expiration

**Handling:**
1. Warn users 30 days before expiration
2. Provide certificate renewal workflow in GUI
3. Allow emergency password fallback

### Smart Card Support

**New Cards:**
1. Test with OpenSC compatibility list
2. Document any card-specific issues
3. Provide configuration for different card types

---

## Future Enhancements

### Phase 2 Features (Future)

1. **Multi-factor Authentication**
   - Require both smart card AND password
   - Biometric support (Windows Hello integration)

2. **Centralized Management**
   - Remote user registration
   - Certificate policy enforcement
   - Audit logging server

3. **Mobile Credentials**
   - NFC smartphone as smart card
   - Bluetooth smart card readers

4. **Azure AD Integration**
   - Sync with Azure AD certificates
   - Hybrid join support

---

## Support & Community

### Documentation

- User Guide: `docs/UserGuide.md`
- Developer Guide: `docs/DeveloperGuide.md`
- API Reference: XML documentation comments
- Architecture Diagrams: `docs/architecture/`

### Issue Tracking

- GitHub Issues for bugs
- Discussions for feature requests
- Security issues: Private disclosure

### Contributing

1. Code contributions: Follow C# and C++ style guides
2. Testing: Provide test results from different Windows versions
3. Documentation: Improvements always welcome

---

## Success Criteria

### Minimum Viable Product (MVP)

- [x] User can register smart card for Windows logon
- [x] User can log in with smart card + PIN
- [x] Password logon still works
- [x] Works on Windows 10 21H2+ and Windows 11
- [x] Uninstaller removes all components cleanly

### Production Ready

- [x] All MVP features
- [x] Code signed binaries
- [x] Comprehensive documentation
- [x] Tested on 5+ different systems
- [x] Error handling for all failure scenarios
- [x] Logging for troubleshooting
- [x] Performance meets targets

---

## Timeline Estimate

**Total Duration:** 12-14 weeks (3-3.5 months)

**Resource Requirements:**
- 1 Senior C++ developer (familiar with Windows security APIs)
- 1 .NET developer (C# and WPF)
- 1 QA tester
- Part-time: Security consultant for code review

**Milestones:**
- Week 4: Core library functional
- Week 6: Authentication package working in test VM
- Week 8: Credential provider UI complete
- Week 10: Full integration with OpenMFA GUI
- Week 12: Beta ready
- Week 14: Release candidate

---

## Cost Estimate

### Development Tools (One-time)
- Visual Studio Professional: $499/year (or use Community Edition - Free)
- Code Signing Certificate: $200-400/year (required)
- WiX Toolset: Free
- **Total:** ~$500-900

### Infrastructure
- Test Windows VMs: Use Hyper-V (Free with Windows) or Azure ($50-100/month)
- Source control: GitHub (Free for public repos)
- CI/CD: GitHub Actions (Free tier sufficient)
- **Total:** $0-100/month

### Third-Party Components
- None required (all based on Windows SDK and open source)

**Total Initial Investment:** ~$500-1000
**Ongoing:** ~$300-500/year (certificate renewal + testing infrastructure)

---

## Conclusion

This implementation plan provides a complete roadmap for integrating Windows smart card logon into OpenMFA. By studying the EIDAuthentication architecture and adapting it to OpenMFA's existing codebase, you'll create a powerful and unique feature that sets OpenMFA apart.

**Key Success Factors:**
1. Thorough understanding of Windows security architecture
2. Careful attention to LSA programming (crashes reboot Windows!)
3. Comprehensive testing across Windows versions
4. Clear documentation for users and developers
5. Compliance with LGPL licensing if using EIDAuthentication code

**Next Steps:**
1. Review this plan with your team
2. Set up development environment
3. Start with Phase 1: Build and study EIDAuthentication
4. Create proof-of-concept with minimal credential provider
5. Iterate based on learnings

Good luck with the implementation! This is an ambitious but achievable project that will provide significant value to OpenMFA users.
