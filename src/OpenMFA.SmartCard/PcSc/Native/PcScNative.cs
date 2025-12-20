using System.Runtime.InteropServices;

namespace OpenMFA.SmartCard.PcSc.Native;

/// <summary>
/// Native PC/SC API wrapper using P/Invoke
/// Supports both Windows (winscard.dll) and Linux (libpcsclite.so.1)
/// </summary>
internal static class PcScNative
{
    private const string WinscardDll = "winscard.dll";
    private const string PcscLiteSo = "libpcsclite.so.1";

    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly string LibraryName = IsWindows ? WinscardDll : PcscLiteSo;

    // Return codes
    public const uint SCARD_S_SUCCESS = 0x00000000;
    public const uint SCARD_E_NO_READERS_AVAILABLE = 0x8010002E;
    public const uint SCARD_E_NO_SMARTCARD = 0x8010000C;
    public const uint SCARD_W_REMOVED_CARD = 0x80100069;

    // Scope
    public const uint SCARD_SCOPE_USER = 0;
    public const uint SCARD_SCOPE_SYSTEM = 2;

    // Share mode
    public const uint SCARD_SHARE_SHARED = 2;
    public const uint SCARD_SHARE_EXCLUSIVE = 1;
    public const uint SCARD_SHARE_DIRECT = 3;

    // Protocol
    public const uint SCARD_PROTOCOL_T0 = 1;
    public const uint SCARD_PROTOCOL_T1 = 2;
    public const uint SCARD_PROTOCOL_ANY = SCARD_PROTOCOL_T0 | SCARD_PROTOCOL_T1;

    // Disposition
    public const uint SCARD_LEAVE_CARD = 0;
    public const uint SCARD_RESET_CARD = 1;
    public const uint SCARD_UNPOWER_CARD = 2;
    public const uint SCARD_EJECT_CARD = 3;

    // State
    public const uint SCARD_STATE_UNAWARE = 0x00000000;
    public const uint SCARD_STATE_PRESENT = 0x00000020;

    // Auto allocation
    public const uint SCARD_AUTOALLOCATE = unchecked((uint)-1);

    // ATR length
    public const int MAX_ATR_SIZE = 33;

    [StructLayout(LayoutKind.Sequential)]
    public struct SCARD_IO_REQUEST
    {
        public uint dwProtocol;
        public uint cbPciLength;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SCARD_READERSTATE
    {
        public string szReader;
        public IntPtr pvUserData;
        public uint dwCurrentState;
        public uint dwEventState;
        public uint cbAtr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ATR_SIZE)]
        public byte[] rgbAtr;
    }

    // Windows API
    [DllImport(WinscardDll, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SCardEstablishContext")]
    private static extern uint SCardEstablishContext_Win(uint dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext);

    [DllImport(WinscardDll, SetLastError = true, EntryPoint = "SCardReleaseContext")]
    private static extern uint SCardReleaseContext_Win(IntPtr hContext);

    [DllImport(WinscardDll, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SCardListReadersW")]
    private static extern uint SCardListReaders_Win(IntPtr hContext, byte[]? mszGroups, byte[]? mszReaders, ref uint pcchReaders);

    [DllImport(WinscardDll, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SCardConnectW")]
    private static extern uint SCardConnect_Win(IntPtr hContext, string szReader, uint dwShareMode, uint dwPreferredProtocols, out IntPtr phCard, out uint pdwActiveProtocol);

    [DllImport(WinscardDll, SetLastError = true, EntryPoint = "SCardDisconnect")]
    private static extern uint SCardDisconnect_Win(IntPtr hCard, uint dwDisposition);

    [DllImport(WinscardDll, SetLastError = true, EntryPoint = "SCardTransmit")]
    private static extern uint SCardTransmit_Win(IntPtr hCard, IntPtr pioSendPci, byte[] pbSendBuffer, uint cbSendLength, IntPtr pioRecvPci, byte[] pbRecvBuffer, ref uint pcbRecvLength);

    // Linux API (same signatures, different DLL)
    [DllImport(PcscLiteSo, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SCardEstablishContext")]
    private static extern uint SCardEstablishContext_Linux(uint dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext);

    [DllImport(PcscLiteSo, SetLastError = true, EntryPoint = "SCardReleaseContext")]
    private static extern uint SCardReleaseContext_Linux(IntPtr hContext);

    [DllImport(PcscLiteSo, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SCardListReaders")]
    private static extern uint SCardListReaders_Linux(IntPtr hContext, byte[]? mszGroups, byte[]? mszReaders, ref uint pcchReaders);

    [DllImport(PcscLiteSo, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SCardConnect")]
    private static extern uint SCardConnect_Linux(IntPtr hContext, string szReader, uint dwShareMode, uint dwPreferredProtocols, out IntPtr phCard, out uint pdwActiveProtocol);

    [DllImport(PcscLiteSo, SetLastError = true, EntryPoint = "SCardDisconnect")]
    private static extern uint SCardDisconnect_Linux(IntPtr hCard, uint dwDisposition);

    [DllImport(PcscLiteSo, SetLastError = true, EntryPoint = "SCardTransmit")]
    private static extern uint SCardTransmit_Linux(IntPtr hCard, IntPtr pioSendPci, byte[] pbSendBuffer, uint cbSendLength, IntPtr pioRecvPci, byte[] pbRecvBuffer, ref uint pcbRecvLength);

    // Cross-platform wrappers
    public static uint SCardEstablishContext(uint dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext)
    {
        return IsWindows
            ? SCardEstablishContext_Win(dwScope, pvReserved1, pvReserved2, out phContext)
            : SCardEstablishContext_Linux(dwScope, pvReserved1, pvReserved2, out phContext);
    }

    public static uint SCardReleaseContext(IntPtr hContext)
    {
        return IsWindows
            ? SCardReleaseContext_Win(hContext)
            : SCardReleaseContext_Linux(hContext);
    }

    public static uint SCardListReaders(IntPtr hContext, byte[]? mszGroups, byte[]? mszReaders, ref uint pcchReaders)
    {
        return IsWindows
            ? SCardListReaders_Win(hContext, mszGroups, mszReaders, ref pcchReaders)
            : SCardListReaders_Linux(hContext, mszGroups, mszReaders, ref pcchReaders);
    }

    public static uint SCardConnect(IntPtr hContext, string szReader, uint dwShareMode, uint dwPreferredProtocols, out IntPtr phCard, out uint pdwActiveProtocol)
    {
        return IsWindows
            ? SCardConnect_Win(hContext, szReader, dwShareMode, dwPreferredProtocols, out phCard, out pdwActiveProtocol)
            : SCardConnect_Linux(hContext, szReader, dwShareMode, dwPreferredProtocols, out phCard, out pdwActiveProtocol);
    }

    public static uint SCardDisconnect(IntPtr hCard, uint dwDisposition)
    {
        return IsWindows
            ? SCardDisconnect_Win(hCard, dwDisposition)
            : SCardDisconnect_Linux(hCard, dwDisposition);
    }

    public static uint SCardTransmit(IntPtr hCard, IntPtr pioSendPci, byte[] pbSendBuffer, uint cbSendLength, IntPtr pioRecvPci, byte[] pbRecvBuffer, ref uint pcbRecvLength)
    {
        return IsWindows
            ? SCardTransmit_Win(hCard, pioSendPci, pbSendBuffer, cbSendLength, pioRecvPci, pbRecvBuffer, ref pcbRecvLength)
            : SCardTransmit_Linux(hCard, pioSendPci, pbSendBuffer, cbSendLength, pioRecvPci, pbRecvBuffer, ref pcbRecvLength);
    }

    /// <summary>
    /// Get error message for PC/SC error code
    /// </summary>
    public static string GetErrorMessage(uint errorCode) => errorCode switch
    {
        SCARD_S_SUCCESS => "Success",
        SCARD_E_NO_READERS_AVAILABLE => "No smart card readers available",
        SCARD_E_NO_SMARTCARD => "No smart card present",
        SCARD_W_REMOVED_CARD => "Smart card was removed",
        _ => $"PC/SC error 0x{errorCode:X8}"
    };
}
