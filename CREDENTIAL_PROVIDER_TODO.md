# OpenMFA Credential Provider - Implementation TODO

## Project Goals

Build a Windows Credential Provider using the **Windows Credential Provider V2 framework** to enable smart card logon on standalone/workgroup computers, integrated with OpenMFA.

**Key Principle:** Leverage Windows Credential Provider framework wherever possible, minimize custom authentication code.

---

## TODO List

### Phase 1: Environment Setup & Research (Week 1-2)

#### Machine Setup

- [ ] **Install Visual Studio 2022**
  ```powershell
  # Download from https://visualstudio.microsoft.com/
  # Required workloads:
  # - Desktop development with C++
  # - .NET desktop development
  # - Universal Windows Platform development (for Credential Provider SDK)
  ```

- [ ] **Install Windows SDK**
  ```powershell
  # Included with Visual Studio
  # Verify location: C:\Program Files (x86)\Windows Kits\10\
  # Required headers:
  # - credentialprovider.h
  # - ntsecapi.h
  # - ntsecpkg.h
  ```

- [ ] **Install WiX Toolset**
  ```powershell
  # Download from https://wixtoolset.org/releases/
  # Install WiX 3.11 or later
  winget install WiXToolset.WiX
  ```

- [ ] **Set up Test VM**
  ```powershell
  # Create Windows 10 22H2 VM in Hyper-V
  # Enable nested virtualization if testing in VM
  Set-VMProcessor -VMName "OpenMFA-Test" -ExposeVirtualizationExtensions $true

  # Take snapshot before any testing
  Checkpoint-VM -Name "OpenMFA-Test" -SnapshotName "Clean Install"
  ```

- [ ] **Install OpenSC on Test VM**
  ```powershell
  # Download from https://github.com/OpenSC/OpenSC/releases
  # Install both 32-bit and 64-bit versions
  # Verify: C:\Program Files\OpenSC Project\OpenSC\
  ```

#### Research & Study

- [ ] **Study Windows Credential Provider V2 samples**
  ```powershell
  # Download Windows SDK samples
  # Location after install: C:\Program Files (x86)\Windows Kits\10\Samples
  # Study: Samples\Security\CredentialProvider\SampleHardwareEventCredentialProvider
  # Study: Samples\Security\CredentialProvider\SampleWrapExistingCredentialProvider
  ```

  **Key files to understand:**
  - `CSampleProvider.cpp` - ICredentialProviderV2 implementation
  - `CSampleCredential.cpp` - ICredentialProviderCredentialV2 implementation
  - `helpers.cpp` - Serialization and field management
  - `CommandWindow.cpp` - Event handling

- [ ] **Build and test sample credential provider**
  ```cmd
  # Navigate to sample directory
  cd "C:\Program Files (x86)\Windows Kits\10\Samples\Security\CredentialProvider\SampleHardwareEventCredentialProvider"

  # Build with MSBuild
  msbuild SampleHardwareEventCredentialProvider.sln /p:Configuration=Release /p:Platform=x64

  # Register (as Administrator)
  regsvr32 x64\Release\SampleHardwareEventCredentialProvider.dll

  # Test
  # Lock Windows (Win+L) and observe

  # Unregister
  regsvr32 /u x64\Release\SampleHardwareEventCredentialProvider.dll
  ```

- [ ] **Study EIDAuthentication architecture**
  ```powershell
  # Analyze NOUPLOAD\EIDAuthentication\
  # Focus on:
  # - StoredCredentialManagement.cpp (certificate-to-user mapping)
  # - CSmartCardNotifier.cpp (smart card events)
  # - EIDAuthenticationPackage.cpp (LSA integration)
  ```

- [ ] **Document Windows Credential Provider V2 APIs**
  - ICredentialProviderV2
  - ICredentialProviderCredentialV2
  - ICredentialProviderEvents
  - IConnectableCredentialProviderCredential
  - ICredentialProviderSetUserArray

#### Decision Points

- [ ] **Decision: Authentication Package vs. Credential Provider Only**

  **Option A: Full Custom Authentication (like EIDAuthentication)**
  - Pros: Complete control, works with any certificate
  - Cons: Complex, runs in lsass.exe, requires LSA expertise

  **Option B: Credential Provider + Native Windows Smart Card Authentication**
  - Pros: Leverages Windows, simpler, more stable
  - Cons: May require certificates to meet Windows requirements

  **RECOMMENDED: Start with Option B, fall back to Option A if needed**

- [ ] **Decision: Storage Mechanism**

  **Option A: LSA Secrets (EIDAuthentication approach)**
  ```cpp
  // Store in: HKLM\SECURITY\Policy\Secrets\L$_OPENMFA_{RID}
  // Pros: Secure, encrypted by Windows
  // Cons: Requires SYSTEM privileges to read
  ```

  **Option B: Windows Credential Manager**
  ```cpp
  // Use CredWrite/CredRead APIs
  // Store as: Type=CRED_TYPE_DOMAIN_CERTIFICATE
  // Pros: Native Windows API, easier access
  // Cons: User-specific (not system-wide)
  ```

  **Option C: Registry + DPAPI**
  ```cpp
  // Store in: HKLM\SOFTWARE\OpenMFA\Users\{username}
  // Encrypt with: CryptProtectData (DPAPI)
  // Pros: Simple, secure
  // Cons: Custom implementation
  ```

  **RECOMMENDED: Option A (LSA Secrets) for security and compatibility with EIDAuthentication approach**

---

### Phase 2: Create Solution Structure (Week 2)

- [ ] **Create Native C++ Solution**
  ```powershell
  # Create directory
  New-Item -ItemType Directory -Path "src\Native" -Force
  cd src\Native

  # Create Visual Studio solution
  # File → New → Project → Blank Solution
  # Name: OpenMFA.Native
  # Save in: src\Native\
  ```

- [ ] **Create OpenMFACredentialProvider project**
  ```
  # In Visual Studio:
  # Add New Project → Windows Desktop → Dynamic-Link Library (DLL)
  # Project name: OpenMFACredentialProvider
  # Platform: x64
  # Configuration: Debug and Release

  # Project settings:
  # - C/C++ → General → Additional Include Directories:
  #   $(WindowsSdkDir)Include\$(WindowsSDKVersion)um;
  #   $(WindowsSdkDir)Include\$(WindowsSDKVersion)shared;
  # - Linker → Input → Additional Dependencies:
  #   credui.lib;Winscard.lib;Crypt32.lib;
  ```

- [ ] **Create OpenMFACardLibrary project**
  ```
  # Add New Project → Dynamic-Link Library (DLL)
  # Project name: OpenMFACardLibrary
  # Platform: x64

  # This will handle:
  # - Smart card communication
  # - Certificate storage/retrieval
  # - User mapping
  ```

- [ ] **Create .NET Wrapper Project**
  ```powershell
  cd ..
  # Create .NET class library
  dotnet new classlib -n OpenMFA.CredentialProvider -f net9.0
  cd OpenMFA.CredentialProvider

  # Add project reference to OpenMFA solution
  ```

---

### Phase 3: Implement Core Library (Week 3-4)

#### OpenMFACardLibrary - Certificate Storage

- [ ] **Implement LSA Secret storage (CStoredCredentialManager)**

  **File: `CStoredCredentialManager.h`**
  ```cpp
  #pragma once
  #include <windows.h>
  #include <ntsecapi.h>
  #include <wincrypt.h>

  #define OPENMFA_SECRET_PREFIX L"L$_OPENMFA_"

  typedef struct _OPENMFA_CREDENTIAL_DATA {
      DWORD dwVersion;                    // Version = 1
      BYTE CertHash[20];                  // SHA1 hash
      DWORD dwCertSize;                   // Certificate size
      BYTE CertData[1];                   // Variable: DER-encoded cert
  } OPENMFA_CREDENTIAL_DATA, *POPENMFA_CREDENTIAL_DATA;

  class CStoredCredentialManager {
  public:
      // Store certificate for user
      static BOOL StoreCredential(
          DWORD dwUserRid,
          PCCERT_CONTEXT pCertContext
      );

      // Get username from certificate
      static BOOL GetUsernameFromCertificate(
          PCCERT_CONTEXT pCertContext,
          LPWSTR* ppszUsername,
          PDWORD pdwRid
      );

      // Remove stored credential
      static BOOL RemoveCredential(DWORD dwUserRid);

      // Check if credential exists
      static BOOL HasCredential(DWORD dwUserRid);

  private:
      static BOOL OpenPolicy(PLSA_HANDLE phPolicy);
      static BOOL StoreSecret(
          LSA_HANDLE hPolicy,
          LPCWSTR szSecretName,
          PBYTE pbData,
          DWORD cbData
      );
      static BOOL RetrieveSecret(
          LSA_HANDLE hPolicy,
          LPCWSTR szSecretName,
          PBYTE* ppbData,
          PDWORD pcbData
      );
  };
  ```

  **Implementation tasks:**
  - [ ] Implement `OpenPolicy()` using `LsaOpenPolicy()`
  - [ ] Implement `StoreSecret()` using `LsaStorePrivateData()`
  - [ ] Implement `RetrieveSecret()` using `LsaRetrievePrivateData()`
  - [ ] Implement `StoreCredential()` - serialize cert data and store
  - [ ] Implement `GetUsernameFromCertificate()` - enumerate users, compare certs
  - [ ] Add error handling and logging
  - [ ] Test with unit tests

#### OpenMFACardLibrary - Smart Card Interface

- [ ] **Implement smart card enumeration (CSmartCardHelper)**

  **File: `CSmartCardHelper.h`**
  ```cpp
  #pragma once
  #include <windows.h>
  #include <winscard.h>
  #include <wincrypt.h>

  class CSmartCardHelper {
  public:
      // Enumerate readers with cards
      static BOOL EnumerateReaders(
          std::vector<std::wstring>& readers
      );

      // Get certificate from card using Windows API
      static BOOL GetCertificateFromCard(
          LPCWSTR szReaderName,
          PCCERT_CONTEXT* ppCertContext
      );

      // Validate PIN (returns TRUE if correct)
      static BOOL ValidatePin(
          LPCWSTR szReaderName,
          LPCWSTR szPin,
          PCCERT_CONTEXT pCertContext
      );

      // Get container name for certificate
      static BOOL GetContainerName(
          PCCERT_CONTEXT pCertContext,
          LPWSTR szContainer,
          DWORD cchContainer
      );
  };
  ```

  **Implementation tasks:**
  - [ ] Use `SCardEstablishContext()` to connect to Smart Card service
  - [ ] Use `SCardListReaders()` to enumerate readers
  - [ ] Use `CertOpenSystemStore()` with "MY" store
  - [ ] Use `CertFindCertificateInStore()` with smart card flag
  - [ ] Use `CryptAcquireCertificatePrivateKey()` to validate PIN
  - [ ] Test with OpenSC cards

#### OpenMFACardLibrary - User Management

- [ ] **Implement Windows user enumeration (CUserManager)**

  **File: `CUserManager.h`**
  ```cpp
  #pragma once
  #include <windows.h>
  #include <lm.h>

  class CUserManager {
  public:
      // Get user RID from username
      static BOOL GetUserRid(
          LPCWSTR szUsername,
          PDWORD pdwRid
      );

      // Get username from RID
      static BOOL GetUsernameFromRid(
          DWORD dwRid,
          LPWSTR* ppszUsername
      );

      // Enumerate all local users
      static BOOL EnumerateLocalUsers(
          std::vector<UserInfo>& users
      );

      // Check if user exists
      static BOOL UserExists(LPCWSTR szUsername);
  };
  ```

  **Implementation tasks:**
  - [ ] Use `NetUserEnum()` to list all local users
  - [ ] Use `NetUserGetInfo()` to get user details
  - [ ] Extract RID from user SID
  - [ ] Cache user list for performance
  - [ ] Handle special accounts (Administrator, Guest)

- [ ] **Build and test OpenMFACardLibrary**
  ```cmd
  # Build
  msbuild OpenMFACardLibrary.vcxproj /p:Configuration=Debug /p:Platform=x64

  # Test basic functions
  # Create test console app that:
  # 1. Enumerates users
  # 2. Stores test credential
  # 3. Retrieves credential
  # 4. Deletes credential
  ```

---

### Phase 4: Implement Credential Provider (Week 5-7)

#### Credential Provider - Core Classes

- [ ] **Create provider class (COpenMFAProvider)**

  **Base on:** Windows SDK `CSampleProvider` sample

  **File: `COpenMFAProvider.h`**
  ```cpp
  #pragma once
  #include <credentialprovider.h>
  #include <windows.h>

  class COpenMFAProvider :
      public ICredentialProvider,
      public ICredentialProviderSetUserArray
  {
  public:
      // IUnknown
      IFACEMETHODIMP_(ULONG) AddRef();
      IFACEMETHODIMP_(ULONG) Release();
      IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv);

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

      // ICredentialProviderSetUserArray
      IFACEMETHODIMP SetUserArray(ICredentialProviderUserArray* users);

      // Constructor/Destructor
      COpenMFAProvider();
      ~COpenMFAProvider();

  private:
      LONG _cRef;
      std::vector<ICredentialProviderCredential*> _credentials;
      CREDENTIAL_PROVIDER_USAGE_SCENARIO _cpus;
      ICredentialProviderEvents* _pEvents;
      UINT_PTR _upAdviseContext;

      HRESULT _EnumerateSmartCards();
      HRESULT _CreateCredentialForCard(
          LPCWSTR szReaderName,
          PCCERT_CONTEXT pCertContext
      );
  };
  ```

  **Implementation tasks:**
  - [ ] Implement IUnknown (AddRef, Release, QueryInterface)
  - [ ] Implement SetUsageScenario - handle CPUS_LOGON and CPUS_UNLOCK_WORKSTATION
  - [ ] Implement Advise/UnAdvise for event callbacks
  - [ ] Implement GetFieldDescriptorCount/At - define UI fields
  - [ ] Implement GetCredentialCount/At - enumerate smart cards
  - [ ] Implement _EnumerateSmartCards - use CSmartCardHelper
  - [ ] Implement _CreateCredentialForCard - create credential objects

- [ ] **Create credential class (COpenMFACredential)**

  **Base on:** Windows SDK `CSampleCredential` sample

  **File: `COpenMFACredential.h`**
  ```cpp
  #pragma once
  #include <credentialprovider.h>
  #include <windows.h>

  // UI Field IDs
  enum OPENMFA_FIELD_ID {
      OFI_TILEIMAGE = 0,       // Smart card icon
      OFI_LABEL,               // "Smart Card"
      OFI_LARGE_TEXT,          // Reader name
      OFI_SMALL_TEXT,          // "Insert card and enter PIN"
      OFI_PIN_EDIT,            // PIN entry
      OFI_SUBMIT_BUTTON,       // "Sign In"
      OFI_NUM_FIELDS
  };

  class COpenMFACredential :
      public ICredentialProviderCredential2,
      public IConnectableCredentialProviderCredential
  {
  public:
      // IUnknown
      IFACEMETHODIMP_(ULONG) AddRef();
      IFACEMETHODIMP_(ULONG) Release();
      IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv);

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
      IFACEMETHODIMP GetSubmitButtonValue(DWORD dwFieldID, DWORD* pdwAdjacentTo);
      IFACEMETHODIMP SetStringValue(DWORD dwFieldID, LPCWSTR pwz);
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

      // ICredentialProviderCredential2
      IFACEMETHODIMP GetUserSid(LPWSTR* ppszSid);

      // IConnectableCredentialProviderCredential
      IFACEMETHODIMP Connect(IQueryContinueWithStatus* pqcws);
      IFACEMETHODIMP Disconnect();

      // Constructor
      COpenMFACredential(
          LPCWSTR szReaderName,
          PCCERT_CONTEXT pCertContext
      );
      ~COpenMFACredential();

  private:
      LONG _cRef;
      WCHAR _szReaderName[256];
      WCHAR _szPin[256];
      WCHAR _szUsername[256];
      PCCERT_CONTEXT _pCertContext;
      CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR _rgFieldDescriptors[OFI_NUM_FIELDS];
      CREDENTIAL_PROVIDER_FIELD_STATE _rgFieldStates[OFI_NUM_FIELDS];
      ICredentialProviderCredentialEvents* _pEvents;
      DWORD _dwUserRid;

      HRESULT _GetUsernameFromCertificate();
      HRESULT _ValidatePinAndGetSerialization(
          CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* pcpgsr,
          CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs
      );
  };
  ```

  **Implementation tasks:**
  - [ ] Implement all ICredentialProviderCredential2 methods
  - [ ] Implement GetFieldState - define which fields are visible/editable
  - [ ] Implement GetStringValue - return reader name, labels
  - [ ] Implement SetStringValue - capture PIN input
  - [ ] Implement GetSerialization - **THIS IS CRITICAL**
    - Use `KerbInteractiveUnlockLogonPack()` helper (from Windows SDK sample)
    - Set serialization type to CPUS_SMART_CARD_PIN
    - Package: username, domain, PIN
  - [ ] Implement GetUserSid - return SID for found user
  - [ ] Implement _GetUsernameFromCertificate - call CStoredCredentialManager
  - [ ] Add smart card icon bitmap resource

#### Credential Provider - Registration

- [ ] **Create DLL registration (Dll.cpp)**
  ```cpp
  // Implement DllMain, DllGetClassObject, DllCanUnloadNow
  // GUID: Generate new GUID for OpenMFA provider
  // Example: {12345678-1234-1234-1234-123456789ABC}
  ```

- [ ] **Create registry scripts**

  **File: `Register.reg`**
  ```
  Windows Registry Editor Version 5.00

  [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{YOUR-GUID}]
  @="OpenMFA Smart Card Provider"

  [HKEY_CLASSES_ROOT\CLSID\{YOUR-GUID}]
  @="OpenMFA Smart Card Provider"

  [HKEY_CLASSES_ROOT\CLSID\{YOUR-GUID}\InprocServer32]
  @="OpenMFACredentialProvider.dll"
  "ThreadingModel"="Apartment"
  ```

  **File: `Unregister.reg`**
  ```
  Windows Registry Editor Version 5.00

  [-HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{YOUR-GUID}]
  [-HKEY_CLASSES_ROOT\CLSID\{YOUR-GUID}]
  ```

- [ ] **Build credential provider**
  ```cmd
  msbuild OpenMFACredentialProvider.vcxproj /p:Configuration=Release /p:Platform=x64
  ```

- [ ] **Test credential provider (Manual)**
  ```cmd
  # Copy DLL to System32
  copy bin\x64\Release\OpenMFACredentialProvider.dll C:\Windows\System32\

  # Register
  reg import Register.reg

  # Or use regsvr32 (if you implement DllRegisterServer)
  regsvr32 C:\Windows\System32\OpenMFACredentialProvider.dll

  # Lock Windows and test
  # Press Win+L

  # Check Event Viewer for errors
  eventvwr.msc
  # Application and Services Logs → Microsoft → Windows → CredentialProvider
  ```

---

### Phase 5: .NET Integration (Week 8-9)

#### .NET Wrapper Library

- [ ] **Create P/Invoke wrapper (NativeMethods.cs)**
  ```csharp
  using System;
  using System.Runtime.InteropServices;

  namespace OpenMFA.CredentialProvider
  {
      internal static class NativeMethods
      {
          const string CARD_LIBRARY = "OpenMFACardLibrary.dll";

          [DllImport(CARD_LIBRARY, CallingConvention = CallingConvention.StdCall)]
          public static extern bool StoreCredential(
              uint dwUserRid,
              IntPtr pCertContext);

          [DllImport(CARD_LIBRARY, CallingConvention = CallingConvention.StdCall)]
          public static extern bool RemoveCredential(uint dwUserRid);

          [DllImport(CARD_LIBRARY, CallingConvention = CallingConvention.StdCall)]
          public static extern bool HasCredential(uint dwUserRid);

          [DllImport(CARD_LIBRARY, CallingConvention = CallingConvention.StdCall)]
          public static extern bool GetUsernameFromCertificate(
              IntPtr pCertContext,
              out IntPtr ppszUsername,
              out uint pdwRid);

          // NetUserGetInfo to get RID
          [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
          public struct USER_INFO_23
          {
              public string usri23_name;
              // ... other fields
              public uint usri23_user_id; // This is the RID
          }

          [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
          public static extern int NetUserGetInfo(
              string servername,
              string username,
              int level,
              out IntPtr bufptr);

          [DllImport("netapi32.dll")]
          public static extern int NetApiBufferFree(IntPtr buffer);
      }
  }
  ```

- [ ] **Create high-level manager (CredentialProviderManager.cs)**
  ```csharp
  using System;
  using System.Security.Cryptography.X509Certificates;

  namespace OpenMFA.CredentialProvider
  {
      public class CredentialProviderManager
      {
          public bool RegisterUser(string username, X509Certificate2 certificate)
          {
              // 1. Get user RID
              uint rid = GetUserRid(username);
              if (rid == 0)
                  return false;

              // 2. Get certificate context
              IntPtr certContext = certificate.Handle;

              // 3. Store credential
              return NativeMethods.StoreCredential(rid, certContext);
          }

          public bool UnregisterUser(string username)
          {
              uint rid = GetUserRid(username);
              if (rid == 0)
                  return false;

              return NativeMethods.RemoveCredential(rid);
          }

          public bool IsUserRegistered(string username)
          {
              uint rid = GetUserRid(username);
              if (rid == 0)
                  return false;

              return NativeMethods.HasCredential(rid);
          }

          private uint GetUserRid(string username)
          {
              IntPtr buffer = IntPtr.Zero;
              try
              {
                  int result = NativeMethods.NetUserGetInfo(
                      null, username, 23, out buffer);

                  if (result != 0)
                      return 0;

                  var userInfo = Marshal.PtrToStructure<NativeMethods.USER_INFO_23>(buffer);
                  return userInfo.usri23_user_id;
              }
              finally
              {
                  if (buffer != IntPtr.Zero)
                      NativeMethods.NetApiBufferFree(buffer);
              }
          }
      }
  }
  ```

- [ ] **Build .NET wrapper**
  ```powershell
  cd src\OpenMFA.CredentialProvider
  dotnet build
  ```

#### GUI Integration

- [ ] **Add Windows Logon tab to OpenMFA.GUI**

  **File: `WindowsLogonForm.cs`**
  ```csharp
  public class WindowsLogonForm : Form
  {
      private Button btnRegisterUser;
      private Button btnUnregisterUser;
      private ListView lvRegisteredUsers;
      private CredentialProviderManager _manager;

      public WindowsLogonForm()
      {
          InitializeComponent();
          _manager = new CredentialProviderManager();
      }

      private async void BtnRegisterUser_Click(object sender, EventArgs e)
      {
          // 1. Check if card detected
          if (_card == null)
          {
              MessageBox.Show("Please detect card first");
              return;
          }

          // 2. Read certificate from card
          byte[] certData = await _card.ReadCertificateAsync();
          if (certData == null || certData.Length == 0)
          {
              MessageBox.Show("No certificate found on card");
              return;
          }

          // 3. Show user selection dialog
          using var dialog = new UserSelectionDialog();
          if (dialog.ShowDialog() != DialogResult.OK)
              return;

          string username = dialog.SelectedUsername;

          // 4. Register user
          var cert = new X509Certificate2(certData);
          bool success = _manager.RegisterUser(username, cert);

          if (success)
          {
              MessageBox.Show($"User {username} registered for smart card logon");
              RefreshRegisteredUsers();
          }
          else
          {
              MessageBox.Show("Registration failed");
          }
      }

      private void RefreshRegisteredUsers()
      {
          lvRegisteredUsers.Items.Clear();

          // Enumerate local users and check registration
          // Add to ListView
      }
  }
  ```

- [ ] **Create user selection dialog**
  ```csharp
  public class UserSelectionDialog : Form
  {
      private ComboBox cmbUsers;
      private Button btnOK;

      public string SelectedUsername { get; private set; }

      public UserSelectionDialog()
      {
          InitializeComponent();
          LoadLocalUsers();
      }

      private void LoadLocalUsers()
      {
          // Enumerate local users via NetUserEnum
          // Filter out system accounts
          // Add to combobox
      }
  }
  ```

---

### Phase 6: Testing & Debugging (Week 10-11)

#### Unit Tests

- [ ] **Create test project for native code**
  ```cmd
  # Add Google Test project
  # Test CStoredCredentialManager
  # Test CSmartCardHelper
  # Test CUserManager
  ```

- [ ] **Create test project for .NET**
  ```powershell
  cd tests
  dotnet new xunit -n OpenMFA.CredentialProvider.Tests
  # Add tests for CredentialProviderManager
  ```

#### Integration Testing

- [ ] **Test Scenario 1: Basic Registration and Login**
  ```
  Steps:
  1. Fresh Windows 10 VM
  2. Create local user "testuser"
  3. Initialize MyEID card with OpenMFA
  4. Register testuser with card
  5. Lock Windows
  6. Log in with smart card + PIN

  Expected: Login successful
  ```

- [ ] **Test Scenario 2: Multiple Users**
  ```
  Steps:
  1. Create 3 local users
  2. Register user1 and user2 with different cards
  3. Test login with user1 card
  4. Test login with user2 card
  5. Test user3 cannot login with any card

  Expected: Only registered users can login with their cards
  ```

- [ ] **Test Scenario 3: Wrong PIN**
  ```
  Steps:
  1. Registered user
  2. Enter wrong PIN 3 times
  3. Enter correct PIN

  Expected:
  - Wrong PIN: Error message
  - Correct PIN: Login successful
  ```

- [ ] **Test Scenario 4: Card Removal**
  ```
  Steps:
  1. Start login process
  2. Remove card before entering PIN
  3. Re-insert card
  4. Enter PIN

  Expected: Graceful handling, no crash
  ```

- [ ] **Test Scenario 5: Password Still Works**
  ```
  Steps:
  1. User registered with smart card
  2. Login with password (not smart card)

  Expected: Password login still works normally
  ```

#### Debugging

- [ ] **Enable detailed logging**
  ```cpp
  // In all DLLs, implement logging to:
  // C:\OpenMFA\Logs\CredentialProvider.log

  void Log(LPCWSTR format, ...)
  {
      FILE* f = _wfopen(L"C:\\OpenMFA\\Logs\\CredentialProvider.log", L"a");
      if (f)
      {
          // Write timestamp + message
          fclose(f);
      }
  }
  ```

- [ ] **Use DebugView for real-time logging**
  ```cpp
  OutputDebugString(L"OpenMFA: Credential provider initialized");
  ```

  Download DebugView: https://learn.microsoft.com/en-us/sysinternals/downloads/debugview

- [ ] **Test with Credential Provider Test Tool**
  ```cmd
  # Part of Windows SDK
  "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\CredentialProviderTest.exe"
  ```

---

### Phase 7: Installer & Deployment (Week 12)

#### WiX Installer

- [ ] **Create WiX project**
  ```xml
  <!-- OpenMFAInstaller.wxs -->
  <?xml version="1.0" encoding="UTF-8"?>
  <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <Product Id="*"
             Name="OpenMFA with Credential Provider"
             Language="1033"
             Version="1.0.0.0"
             Manufacturer="OpenMFA"
             UpgradeCode="PUT-GUID-HERE">

      <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />

      <!-- Prerequisites -->
      <PropertyRef Id="WIX_IS_NETFRAMEWORK_48_OR_LATER_INSTALLED"/>
      <Condition Message="This application requires .NET Framework 4.8 or later.">
        <![CDATA[Installed OR WIX_IS_NETFRAMEWORK_48_OR_LATER_INSTALLED]]>
      </Condition>

      <!-- Features -->
      <Feature Id="ProductFeature" Title="OpenMFA" Level="1">
        <ComponentGroupRef Id="OpenMFAFiles" />
        <ComponentGroupRef Id="CredentialProviderFiles" />
        <ComponentRef Id="CredentialProviderRegistry" />
      </Feature>

      <!-- Files -->
      <Directory Id="TARGETDIR" Name="SourceDir">
        <Directory Id="ProgramFiles64Folder">
          <Directory Id="INSTALLFOLDER" Name="OpenMFA">
            <!-- OpenMFA.GUI and .NET components -->
          </Directory>
        </Directory>

        <Directory Id="System64Folder">
          <!-- Native DLLs -->
          <Component Id="OpenMFACardLibrary" Guid="PUT-GUID-HERE">
            <File Id="OpenMFACardLibrary.dll"
                  Source="$(var.BuildDir)\OpenMFACardLibrary.dll"
                  KeyPath="yes" />
          </Component>

          <Component Id="OpenMFACredentialProvider" Guid="PUT-GUID-HERE">
            <File Id="OpenMFACredentialProvider.dll"
                  Source="$(var.BuildDir)\OpenMFACredentialProvider.dll"
                  KeyPath="yes" />
          </Component>
        </Directory>
      </Directory>

      <!-- Registry -->
      <Component Id="CredentialProviderRegistry" Guid="PUT-GUID-HERE">
        <RegistryKey Root="HKLM"
                     Key="SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{YOUR-CP-GUID}">
          <RegistryValue Type="string" Value="OpenMFA Smart Card Provider" />
        </RegistryKey>

        <RegistryKey Root="HKCR" Key="CLSID\{YOUR-CP-GUID}">
          <RegistryValue Type="string" Value="OpenMFA Smart Card Provider" />
          <RegistryKey Key="InprocServer32">
            <RegistryValue Type="string" Value="OpenMFACredentialProvider.dll" />
            <RegistryValue Name="ThreadingModel" Type="string" Value="Apartment" />
          </RegistryKey>
        </RegistryKey>
      </Component>

      <!-- Custom Actions -->
      <CustomAction Id="ScheduleReboot" Execute="immediate"
                    Return="check"
                    Script="vbscript">
        MsgBox "You must restart your computer to complete installation.", vbOKOnly + vbInformation, "Restart Required"
      </CustomAction>

      <InstallExecuteSequence>
        <Custom Action="ScheduleReboot" After="InstallFinalize">NOT Installed</Custom>
      </InstallExecuteSequence>

    </Product>
  </Wix>
  ```

- [ ] **Build installer**
  ```cmd
  candle OpenMFAInstaller.wxs
  light OpenMFAInstaller.wixobj -out OpenMFASetup.msi
  ```

- [ ] **Test installer**
  ```powershell
  # Install
  msiexec /i OpenMFASetup.msi /l*v install.log

  # Verify files copied
  # Verify registry entries
  # Restart
  # Test credential provider appears

  # Uninstall
  msiexec /x OpenMFASetup.msi /l*v uninstall.log

  # Verify clean removal
  ```

#### Code Signing

- [ ] **Acquire code signing certificate**
  ```
  Options:
  1. DigiCert (expensive, ~$500/year)
  2. Sectigo (mid-range, ~$200/year)
  3. SSL.com (budget, ~$100/year)

  For open source: Contact certificate authorities for discounts
  ```

- [ ] **Sign all DLLs and EXE**
  ```cmd
  # Sign each file
  signtool sign /f "your-certificate.pfx" /p password /t http://timestamp.digicert.com OpenMFACredentialProvider.dll
  signtool sign /f "your-certificate.pfx" /p password /t http://timestamp.digicert.com OpenMFACardLibrary.dll
  signtool sign /f "your-certificate.pfx" /p password /t http://timestamp.digicert.com OpenMFA.GUI.exe
  signtool sign /f "your-certificate.pfx" /p password /t http://timestamp.digicert.com OpenMFASetup.msi

  # Verify signature
  signtool verify /pa OpenMFACredentialProvider.dll
  ```

---

### Phase 8: Documentation (Week 13)

#### User Documentation

- [ ] **Create User Guide**
  ```markdown
  # OpenMFA Windows Logon User Guide

  ## Setup
  1. Install OpenMFA
  2. Insert smart card
  3. Initialize card
  4. Register user for Windows logon
  5. Restart computer
  6. Login with smart card

  ## Troubleshooting
  - Smart card not detected
  - Wrong PIN
  - Cannot login
  - Remove registration
  ```

- [ ] **Create video tutorial**
  - Screen recording of complete setup process
  - Upload to YouTube
  - Link from README

#### Developer Documentation

- [ ] **Architecture documentation**
  - Component diagram
  - Sequence diagrams for login flow
  - API reference

- [ ] **Build instructions**
  - Prerequisites
  - Build steps
  - Testing procedures

---

### Phase 9: Release (Week 14)

- [ ] **Create GitHub release**
  ```powershell
  # Tag version
  git tag -a v1.0.0 -m "Initial release with credential provider"
  git push origin v1.0.0

  # Upload to GitHub Releases:
  # - OpenMFASetup.msi (signed)
  # - README.md
  # - LICENSE
  # - User Guide PDF
  ```

- [ ] **Update README.md**
  - Add credential provider features
  - Add system requirements
  - Add installation instructions
  - Add screenshots

- [ ] **Announce release**
  - GitHub Discussions
  - Reddit (r/opensource, r/smartcard)
  - OpenSC mailing list
  - Social media

---

## Machine-Specific Instructions

### Development Machine Setup Script

```powershell
# Run as Administrator

# 1. Install Visual Studio 2022 (manual - download from Microsoft)
# Then install workloads via command line:
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\vs_installer.exe" modify `
  --installPath "C:\Program Files\Microsoft Visual Studio\2022\Community" `
  --add Microsoft.VisualStudio.Workload.NativeDesktop `
  --add Microsoft.VisualStudio.Workload.ManagedDesktop

# 2. Install WiX Toolset
winget install WiXToolset.WiX

# 3. Install Windows SDK (if not included with VS)
winget install Microsoft.WindowsSDK.10.0.22621

# 4. Install Git
winget install Git.Git

# 5. Create development directories
New-Item -ItemType Directory -Path "C:\OpenMFA\Logs" -Force
New-Item -ItemType Directory -Path "C:\OpenMFA\Test" -Force

# 6. Clone OpenMFA repository
cd C:\Projects
git clone https://github.com/YOUR-USERNAME/OpenMFA.git
cd OpenMFA

# 7. Initialize submodules (if any)
git submodule update --init --recursive

# 8. Install .NET SDK (if not already installed)
winget install Microsoft.DotNet.SDK.9

# 9. Restore .NET packages
cd src
dotnet restore

# 10. Set up test VM (Hyper-V)
# Create new VM
$VMName = "OpenMFA-Test-Win10"
New-VM -Name $VMName -MemoryStartupBytes 4GB -Generation 2 -NewVHDPath "C:\VMs\$VMName.vhdx" -NewVHDSizeBytes 60GB
Set-VMProcessor -VMName $VMName -Count 2
Add-VMDvdDrive -VMName $VMName -Path "C:\ISOs\Windows10.iso"

# Enable test signing on test VM (after Windows installed)
# Run this IN the test VM:
# bcdedit /set testsigning on
# Restart-Computer

Write-Host "Development environment setup complete!" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Open src\Native\OpenMFA.Native.sln in Visual Studio"
Write-Host "2. Build solution"
Write-Host "3. Run tests"
```

### Test VM Setup Script

```powershell
# Run in test VM as Administrator

# 1. Enable test signing
bcdedit /set testsigning on

# 2. Install OpenSC
$openscUrl = "https://github.com/OpenSC/OpenSC/releases/download/0.23.0/OpenSC-0.23.0_win64.msi"
Invoke-WebRequest -Uri $openscUrl -OutFile "C:\Temp\OpenSC.msi"
Start-Process msiexec.exe -ArgumentList "/i C:\Temp\OpenSC.msi /quiet" -Wait

# 3. Create test users
$password = ConvertTo-SecureString "TestPass123!" -AsPlainText -Force
New-LocalUser -Name "testuser1" -Password $password -FullName "Test User 1"
New-LocalUser -Name "testuser2" -Password $password -FullName "Test User 2"

# 4. Enable auto-login for testing (optional)
# Set registry for auto-login as your main test user

# 5. Install DebugView
$debugViewUrl = "https://live.sysinternals.com/Dbgview.exe"
Invoke-WebRequest -Uri $debugViewUrl -OutFile "C:\Tools\Dbgview.exe"

# 6. Create snapshot
Checkpoint-VM -Name "OpenMFA-Test-Win10" -SnapshotName "Clean with test users"

Write-Host "Test VM setup complete!" -ForegroundColor Green
```

---

## Critical Success Factors

1. **Use Windows Credential Provider V2 Framework**
   - ✅ Leverage ICredentialProviderV2 and ICredentialProviderCredentialV2
   - ✅ Use Windows SDK samples as foundation
   - ✅ Minimize custom authentication logic

2. **Follow Windows Best Practices**
   - ✅ Handle all credential provider usage scenarios (LOGON, UNLOCK, CHANGE_PASSWORD)
   - ✅ Implement proper COM reference counting
   - ✅ Use Windows serialization helpers
   - ✅ Respect Windows field state conventions

3. **Security First**
   - ✅ Code sign all binaries
   - ✅ Never log PINs
   - ✅ Use Windows DPAPI for encryption
   - ✅ Validate certificates properly

4. **Thorough Testing**
   - ✅ Test on clean VMs with snapshots
   - ✅ Test all error conditions
   - ✅ Test with multiple Windows versions
   - ✅ Security audit before release

5. **Good Documentation**
   - ✅ Clear installation instructions
   - ✅ Troubleshooting guide
   - ✅ Video tutorials
   - ✅ Developer documentation

---

## Timeline Summary

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| 1. Setup & Research | 2 weeks | Dev environment, understanding of Windows CP framework |
| 2. Solution Structure | 1 week | Projects created, builds working |
| 3. Core Library | 2 weeks | OpenMFACardLibrary functional |
| 4. Credential Provider | 3 weeks | OpenMFACredentialProvider working |
| 5. .NET Integration | 2 weeks | GUI integration complete |
| 6. Testing | 2 weeks | All tests passing |
| 7. Installer | 1 week | MSI installer working |
| 8. Documentation | 1 week | All docs complete |
| 9. Release | 1 week | v1.0.0 released |
| **Total** | **12-14 weeks** | **Production-ready credential provider** |

---

## Next Steps

Start with Phase 1:

```powershell
# 1. Set up development machine
# Run the development machine setup script above

# 2. Study Windows SDK samples
cd "C:\Program Files (x86)\Windows Kits\10\Samples\Security\CredentialProvider"
# Open and build SampleHardwareEventCredentialProvider

# 3. Study EIDAuthentication
cd C:\Users\charl\OpenMFA\NOUPLOAD\EIDAuthentication
# Read through StoredCredentialManagement.cpp

# 4. Create first project
cd C:\Users\charl\OpenMFA\src
mkdir Native
cd Native
# Open Visual Studio and create OpenMFA.Native solution
```

**Ready to begin! 🚀**
