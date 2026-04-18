using System.Runtime.InteropServices;
using System.Text;

namespace PunyPlayer;

internal static class NativeMethods
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateFile(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool WriteConsoleInput(IntPtr hConsoleInput, INPUT_RECORD[] lpBuffer, uint nLength, out uint lpNumberOfEventsWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    private const int GWL_EXSTYLE = -20;
    private const uint GA_ROOTOWNER = 3;
    internal const uint WM_KEYDOWN = 0x0100;
    internal const uint WM_KEYUP   = 0x0101;
    internal const uint WM_CHAR    = 0x0102;
    internal const int    STD_INPUT_HANDLE      = -10;
    internal const uint   GENERIC_READ_WRITE    = 0xC0000000;
    internal const uint   FILE_SHARE_READ_WRITE = 0x00000003;
    internal const uint   OPEN_EXISTING         = 3;
    internal const ushort KEY_EVENT = 0x0001;
    internal const ushort VK_RETURN = 0x000D;
    internal const ushort VK_SPACE  = 0x0020;
    private const long WS_EX_TOOLWINDOW = 0x80;
    private const long WS_EX_APPWINDOW = 0x40000;
    private const uint GW_OWNER = 4;

    // Mirrors BOOL bKeyDown field (4 bytes), then WORD fields at explicit offsets.
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    internal struct KEY_EVENT_RECORD
    {
        [FieldOffset(0)]  public int    bKeyDown;
        [FieldOffset(4)]  public ushort wRepeatCount;
        [FieldOffset(6)]  public ushort wVirtualKeyCode;
        [FieldOffset(8)]  public ushort wVirtualScanCode;
        [FieldOffset(10)] public char   UnicodeChar;
        [FieldOffset(12)] public uint   dwControlKeyState;
    }

    // EventType (WORD) at 0, 2 bytes padding, then union at offset 4.
    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUT_RECORD
    {
        [FieldOffset(0)] public ushort          EventType;
        [FieldOffset(4)] public KEY_EVENT_RECORD KeyEvent;
    }

    internal const uint TH32CS_SNAPPROCESS = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int  pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    /// <summary>Returns the parent PID of <paramref name="pid"/>, or 0 on failure.</summary>
    internal static uint GetParentProcessId(uint pid)
    {
        var snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snap == IntPtr.Zero || snap == new IntPtr(-1)) return 0;
        try
        {
            var e = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
            if (!Process32First(snap, ref e)) return 0;
            do
            {
                if (e.th32ProcessID == pid) return e.th32ParentProcessID;
            }
            while (Process32Next(snap, ref e));
            return 0;
        }
        finally { CloseHandle(snap); }
    }

    /// <summary>
    /// Returns true if the window is a suitable target (taskbar-visible app window).
    /// Extracted for testability — all Win32 values are passed in as plain values.
    /// </summary>
    // Titles of phantom/system windows that should never appear in the target list.
    private static readonly string[] ExcludedTitles = [
        "Einstellungen",
        "Windows-Eingabeerfahrung",
    ];

    internal static bool PassesWindowFilter(string title, long exStyleFlags, bool isOwnRootOwner)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;
        if (!isOwnRootOwner) return false;
        // Exclude pure tool windows (no taskbar button) unless forced visible via APPWINDOW
        if ((exStyleFlags & WS_EX_TOOLWINDOW) != 0 && (exStyleFlags & WS_EX_APPWINDOW) == 0)
            return false;
        // Exclude our own window
        if (title.Contains("PunyPlayer", StringComparison.OrdinalIgnoreCase))
            return false;
        // Exclude known phantom/system windows
        foreach (var excluded in ExcludedTitles)
            if (string.Equals(title, excluded, StringComparison.OrdinalIgnoreCase))
                return false;
        return true;
    }

    public static List<(IntPtr Handle, string Title)> GetVisibleWindows()
    {
        var windows = new List<(IntPtr, string)>();
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;

            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();

            // Skip the desktop shell window
            var classSb = new StringBuilder(64);
            GetClassName(hWnd, classSb, classSb.Capacity);
            if (classSb.ToString() == "Progman") return true;

            var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            // Use direct-owner check: GA_ROOTOWNER walks the full owner chain and
            // incorrectly excludes apps (e.g. Chrome) that own a hidden host window.
            var hasNoOwner = GetWindow(hWnd, GW_OWNER) == IntPtr.Zero;

            if (PassesWindowFilter(title, exStyle, hasNoOwner))
                windows.Add((hWnd, title));

            return true;
        }, IntPtr.Zero);
        return windows;
    }
}
