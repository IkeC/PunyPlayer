namespace PunyPlayer;

public static class TextSender
{
    internal static IReadOnlyList<char> BuildTextInput(string text) => text.ToCharArray();

    internal static IReadOnlyList<char> BuildEnterInput() => ['\r'];

    internal static string EscapeSendKeys(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length + 8);
        foreach (var c in text)
        {
            if ("+^%~(){}[]".Contains(c))
                sb.Append('{').Append(c).Append('}');
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Send text + Enter without focusing the window.</summary>
    public static void SendCommand(IntPtr hwnd, string text)
    {
        var chars = BuildTextInput(text);
        var enter = BuildEnterInput();
        // Combine into one WriteConsoleInput call for atomicity
        var all   = chars.Concat(enter).ToArray();
        if (!TrySendViaConsole(hwnd, all))
            PostKeyEvents(hwnd, all);
    }

    /// <summary>Send only Enter without focusing the window.</summary>
    public static void SendEnter(IntPtr hwnd)
    {
        if (!TrySendViaConsole(hwnd, BuildEnterInput()))
            PostKeyEvents(hwnd, BuildEnterInput());
    }

    /// <summary>Send only Space without focusing the window.</summary>
    public static void SendSpace(IntPtr hwnd)
    {
        if (!TrySendViaConsole(hwnd, [' ']))
            PostKeyEvents(hwnd, [' ']);
    }

    /// <summary>
    /// Focus the window, then send text + Enter via SendKeys with optional per-key delay.
    /// Uses AttachThreadInput to reliably force the target to foreground even when
    /// the calling process is not itself the current foreground process.
    /// Keeps the thread attached throughout the send so focus does not escape before
    /// all keystrokes are delivered.
    /// </summary>
    public static void SendCommandFocused(IntPtr hwnd, string text, int keyDelayMs)
    {
        bool attached = BeginForceForeground(hwnd, out uint targetThread);
        try
        {
            foreach (char c in text)
            {
                string k = "+^%~(){}[]".Contains(c) ? "{" + c + "}" : c.ToString();
                System.Windows.Forms.SendKeys.SendWait(k);
                if (keyDelayMs > 0) Thread.Sleep(keyDelayMs);
            }
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        }
        finally
        {
            if (attached) NativeMethods.AttachThreadInput(NativeMethods.GetCurrentThreadId(), targetThread, false);
        }
    }

    /// <summary>Focus the window and send Enter via SendKeys.</summary>
    public static void SendEnterFocused(IntPtr hwnd)
    {
        bool attached = BeginForceForeground(hwnd, out uint targetThread);
        try   { System.Windows.Forms.SendKeys.SendWait("{ENTER}"); }
        finally { if (attached) NativeMethods.AttachThreadInput(NativeMethods.GetCurrentThreadId(), targetThread, false); }
    }

    /// <summary>Focus the window and send Space via SendKeys.</summary>
    public static void SendSpaceFocused(IntPtr hwnd)
    {
        bool attached = BeginForceForeground(hwnd, out uint targetThread);
        try   { System.Windows.Forms.SendKeys.SendWait(" "); }
        finally { if (attached) NativeMethods.AttachThreadInput(NativeMethods.GetCurrentThreadId(), targetThread, false); }
    }

    /// <summary>
    /// Reliably bring hwnd to the foreground using AttachThreadInput.
    /// Returns the target thread ID and whether AttachThreadInput was called;
    /// the <b>caller</b> must detach after sending is done.
    /// </summary>
    private static bool BeginForceForeground(IntPtr hwnd, out uint targetThread)
    {
        targetThread = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        uint ownThread = NativeMethods.GetCurrentThreadId();
        bool attached  = (ownThread != targetThread)
                      && NativeMethods.AttachThreadInput(ownThread, targetThread, true);
        NativeMethods.BringWindowToTop(hwnd);
        NativeMethods.SetForegroundWindow(hwnd);
        Thread.Sleep(50);  // let focus settle while still attached
        return attached;
    }

    // ── Console path (WriteConsoleInput) ─────────────────────────────────────

    private static bool TrySendViaConsole(IntPtr hwnd, IReadOnlyList<char> chars)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        return TryAttachAndWrite(pid, chars)
            || TryAttachAndWrite(NativeMethods.GetParentProcessId(pid), chars);
    }

    private static bool TryAttachAndWrite(uint pid, IReadOnlyList<char> chars)
    {
        if (pid == 0) return false;
        if (!NativeMethods.AttachConsole(pid)) return false;
        try
        {
            // GetStdHandle returns the inherited handle, which is INVALID for a GUI app
            // even after AttachConsole. Open CONIN$ directly to get the active console input.
            var hConIn = NativeMethods.CreateFile(
                "CONIN$",
                NativeMethods.GENERIC_READ_WRITE,
                NativeMethods.FILE_SHARE_READ_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                0, IntPtr.Zero);
            if (hConIn == IntPtr.Zero || hConIn == new IntPtr(-1)) return false;
            try
            {
                var records = new NativeMethods.INPUT_RECORD[chars.Count * 2];
                for (int i = 0; i < chars.Count; i++)
                {
                    char c  = chars[i];
                    ushort vk = c == '\r' ? NativeMethods.VK_RETURN
                              : c == ' '  ? NativeMethods.VK_SPACE
                              : (ushort)0;
                    records[i * 2]     = MakeKeyRecord(c, vk, keyDown: true);
                    records[i * 2 + 1] = MakeKeyRecord(c, vk, keyDown: false);
                }
                return NativeMethods.WriteConsoleInput(hConIn, records, (uint)records.Length, out _);
            }
            finally { NativeMethods.CloseHandle(hConIn); }
        }
        finally { NativeMethods.FreeConsole(); }
    }

    private static NativeMethods.INPUT_RECORD MakeKeyRecord(char c, ushort vk, bool keyDown) =>
        new()
        {
            EventType = NativeMethods.KEY_EVENT,
            KeyEvent  = new NativeMethods.KEY_EVENT_RECORD
            {
                bKeyDown        = keyDown ? 1 : 0,
                wRepeatCount    = 1,
                wVirtualKeyCode = vk,
                UnicodeChar     = c
            }
        };

    // ── GUI fallback path (WM_KEYDOWN + WM_CHAR + WM_KEYUP) ─────────────────
    // Sending all three messages covers both Win32 apps (which use WM_CHAR from
    // TranslateMessage) and SDL-based apps such as VICE (which process WM_KEYDOWN).

    private static void PostKeyEvents(IntPtr hwnd, IEnumerable<char> chars)
    {
        const long lpKeyDown = 1L;              // repeat=1, all other bits 0
        const long lpKeyUp   = 0xC0000001L;    // repeat=1, prev-state=1, transition=1
        foreach (var c in chars)
        {
            ushort vk = CharToVk(c);
            if (vk != 0)
                NativeMethods.PostMessage(hwnd, NativeMethods.WM_KEYDOWN, (IntPtr)vk,  (IntPtr)lpKeyDown);
            NativeMethods.PostMessage(hwnd, NativeMethods.WM_CHAR, (IntPtr)c, IntPtr.Zero);
            if (vk != 0)
                NativeMethods.PostMessage(hwnd, NativeMethods.WM_KEYUP,   (IntPtr)vk,  (IntPtr)lpKeyUp);
        }
    }

    /// <summary>Maps a character to its Windows virtual-key code, or 0 if unknown.</summary>
    private static ushort CharToVk(char c) =>
        c is >= 'a' and <= 'z' ? (ushort)(c - 32) :
        c is >= 'A' and <= 'Z' ? (ushort)c :
        c is >= '0' and <= '9' ? (ushort)c :
        c == ' '  ? NativeMethods.VK_SPACE :
        c == '\r' ? NativeMethods.VK_RETURN :
        (ushort)0;
}
