namespace OpenMFA.SmartCard.Piv;

/// <summary>
/// PIV key reference values per NIST SP 800-73-4 Table 4b
/// </summary>
public enum PivSlot : byte
{
    /// <summary>
    /// PIV Authentication (9A) - Used for login/authentication
    /// </summary>
    Authentication = 0x9A,

    /// <summary>
    /// Digital Signature (9C) - Used for digital signatures
    /// </summary>
    Signature = 0x9C,

    /// <summary>
    /// Key Management (9D) - Used for encryption
    /// </summary>
    KeyManagement = 0x9D,

    /// <summary>
    /// Card Authentication (9E) - Used for physical access
    /// </summary>
    CardAuthentication = 0x9E,

    /// <summary>
    /// Retired Key 1 (82)
    /// </summary>
    Retired1 = 0x82,

    /// <summary>
    /// Retired Key 2 (83)
    /// </summary>
    Retired2 = 0x83
}

/// <summary>
/// PIV algorithm identifiers
/// </summary>
public enum PivAlgorithm : byte
{
    /// <summary>
    /// RSA 1024-bit key
    /// </summary>
    Rsa1024 = 0x06,

    /// <summary>
    /// RSA 2048-bit key (recommended)
    /// </summary>
    Rsa2048 = 0x07,

    /// <summary>
    /// ECC P-256
    /// </summary>
    EccP256 = 0x11,

    /// <summary>
    /// ECC P-384
    /// </summary>
    EccP384 = 0x14
}

/// <summary>
/// PIV data object identifiers per NIST SP 800-73-4 Table 3
/// </summary>
public static class PivDataObject
{
    // Certificates (5FC1xx range)
    public static readonly byte[] CardAuthenticationCertificate = { 0x5F, 0xC1, 0x01 };
    public static readonly byte[] AuthenticationCertificate = { 0x5F, 0xC1, 0x05 };
    public static readonly byte[] SignatureCertificate = { 0x5F, 0xC1, 0x0A };
    public static readonly byte[] KeyManagementCertificate = { 0x5F, 0xC1, 0x0B };
    public static readonly byte[] RetiredCertificate1 = { 0x5F, 0xC1, 0x0D };
    public static readonly byte[] RetiredCertificate2 = { 0x5F, 0xC1, 0x0E };

    // Card metadata
    public static readonly byte[] CardCapabilityContainer = { 0x5F, 0xC1, 0x07 };
    public static readonly byte[] CardHolderUniqueId = { 0x5F, 0xC1, 0x02 };
    public static readonly byte[] CardholderFingerprints = { 0x5F, 0xC1, 0x03 };
    public static readonly byte[] SecurityObject = { 0x5F, 0xC1, 0x06 };
    public static readonly byte[] FacialImage = { 0x5F, 0xC1, 0x08 };
    public static readonly byte[] PrintedInformation = { 0x5F, 0xC1, 0x09 };

    // Discovery object
    public static readonly byte[] DiscoveryObject = { 0x7E };

    /// <summary>
    /// Get the data object ID for a certificate in a specific slot
    /// </summary>
    public static byte[] GetCertificateObject(PivSlot slot) => slot switch
    {
        PivSlot.Authentication => AuthenticationCertificate,
        PivSlot.Signature => SignatureCertificate,
        PivSlot.KeyManagement => KeyManagementCertificate,
        PivSlot.CardAuthentication => CardAuthenticationCertificate,
        PivSlot.Retired1 => RetiredCertificate1,
        PivSlot.Retired2 => RetiredCertificate2,
        _ => throw new ArgumentException($"No certificate object for slot {slot}")
    };

    /// <summary>
    /// Get OpenSC object ID (hex string) for a PIV slot
    /// </summary>
    public static string GetOpenScObjectId(PivSlot slot) => slot switch
    {
        PivSlot.Authentication => "01",          // PIV Authentication
        PivSlot.Signature => "02",               // Digital Signature
        PivSlot.KeyManagement => "03",           // Key Management
        PivSlot.CardAuthentication => "04",      // Card Authentication
        PivSlot.Retired1 => "05",                // Retired Key 1
        PivSlot.Retired2 => "06",                // Retired Key 2
        _ => throw new ArgumentException($"Unknown slot {slot}")
    };
}

/// <summary>
/// Helpers for converting PIV algorithms to OpenSC key types
/// </summary>
public static class PivAlgorithmHelper
{
    public static string ToOpenScKeyType(PivAlgorithm algorithm) => algorithm switch
    {
        PivAlgorithm.Rsa1024 => "RSA:1024",
        PivAlgorithm.Rsa2048 => "RSA:2048",
        PivAlgorithm.EccP256 => "EC:prime256v1",
        PivAlgorithm.EccP384 => "EC:secp384r1",
        _ => throw new ArgumentException($"Unknown algorithm {algorithm}")
    };
}
