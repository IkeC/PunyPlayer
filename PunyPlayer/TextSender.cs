using System.Runtime.InteropServices;

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

    /// <summary>Send text + Enter to the target window using the chosen method.</summary>
    public static void SendCommand(IntPtr hwnd, string text, SendMethod method, int keyDelayMs = 0)
    {
        var all = BuildTextInput(text).Concat(BuildEnterInput()).ToArray();
        if (method.RequiresFocus())
        {
            ForceForeground(hwnd);
            DispatchFocused(all, method, keyDelayMs);
        }
        else
        {
            if (!TrySendViaConsole(hwnd, all))
                DispatchBackground(hwnd, all, method, keyDelayMs);
        }
    }

    /// <summary>Send only Enter to the target window using the chosen method.</summary>
    public static void SendEnter(IntPtr hwnd, SendMethod method)
    {
        char[] chars = ['\r'];
        if (method.RequiresFocus())
        {
            ForceForeground(hwnd);
            DispatchFocused(chars, method, 0);
        }
        else
        {
            if (!TrySendViaConsole(hwnd, chars))
                DispatchBackground(hwnd, chars, method, 0);
        }
    }

    /// <summary>Send only Space to the target window using the chosen method.</summary>
    public static void SendSpace(IntPtr hwnd, SendMethod method)
    {
        char[] chars = [' '];
        if (method.RequiresFocus())
        {
            ForceForeground(hwnd);
            DispatchFocused(chars, method, 0);
        }
        else
        {
            if (!TrySendViaConsole(hwnd, chars))
                DispatchBackground(hwnd, chars, method, 0);
        }
    }

    // ── Focus management ─────────────────────────────────────────────────────
    // Attach → BringWindowToTop + SetForegroundWindow → Detach → settle.
    // Detaching before sending avoids queue-sharing locks that block SDL event loops.

    private static void ForceForeground(IntPtr hwnd)
    {
        uint targetThread = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        uint ownThread = NativeMethods.GetCurrentThreadId();
        bool attached = (ownThread != targetThread)
                     && NativeMethods.AttachThreadInput(ownThread, targetThread, true);
        NativeMethods.BringWindowToTop(hwnd);
        NativeMethods.SetForegroundWindow(hwnd);
        if (attached) NativeMethods.AttachThreadInput(ownThread, targetThread, false);
        Thread.Sleep(50);
    }

    // ── Focused dispatch (requires foreground) ───────────────────────────────

    private static void DispatchFocused(char[] chars, SendMethod method, int keyDelayMs)
    {
        foreach (char c in chars)
        {
            switch (method)
            {
                case SendMethod.SendKeys:
                    if (c == '\r')
                        System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                    else if (c == ' ')
                        System.Windows.Forms.SendKeys.SendWait(" ");
                    else
                    {
                        string k = "+^%~(){}[]".Contains(c) ? "{" + c + "}" : c.ToString();
                        System.Windows.Forms.SendKeys.SendWait(k);
                    }
                    break;

                case SendMethod.InputUnicode:
                    SendInputUnicodeChar(c);
                    break;

                case SendMethod.InputVK:
                    SendInputVKChar(c);
                    break;

                case SendMethod.InputScancode:
                    SendInputScancodeChar(c);
                    break;

                case SendMethod.KeybdEvent:
                    SendKeybdEventChar(c);
                    break;
            }
            if (keyDelayMs > 0) Thread.Sleep(keyDelayMs);
        }
    }

    // ── Background dispatch (no focus needed) ────────────────────────────────

    private static void DispatchBackground(IntPtr hwnd, IEnumerable<char> chars, SendMethod method, int keyDelayMs)
    {
        switch (method)
        {
            case SendMethod.PostMessage:
                PostFullKeyEvents(hwnd, chars, keyDelayMs);
                break;
            case SendMethod.SendMessage:
                SendFullKeyEvents(hwnd, chars, keyDelayMs);
                break;
            case SendMethod.PostCharOnly:
                PostCharOnlyEvents(hwnd, chars, keyDelayMs);
                break;
            case SendMethod.SendCharOnly:
                SendCharOnlyEvents(hwnd, chars, keyDelayMs);
                break;
        }
    }

    // ── PostMessage: WM_KEYDOWN + WM_CHAR + WM_KEYUP (async) ────────────────

    private static void PostFullKeyEvents(IntPtr hwnd, IEnumerable<char> chars, int keyDelayMs)
    {
        foreach (var c in chars)
        {
            ushort vk = CharToVk(c);
            if (vk != 0)
                NativeMethods.PostMessage(hwnd, NativeMethods.WM_KEYDOWN, (IntPtr)vk, BuildKeyLParam(vk, false));
            NativeMethods.PostMessage(hwnd, NativeMethods.WM_CHAR, (IntPtr)c, BuildCharLParam(vk));
            if (vk != 0)
                NativeMethods.PostMessage(hwnd, NativeMethods.WM_KEYUP, (IntPtr)vk, BuildKeyLParam(vk, true));
            if (keyDelayMs > 0) Thread.Sleep(keyDelayMs);
        }
    }

    // ── SendMessage: WM_KEYDOWN + WM_CHAR + WM_KEYUP (sync) ─────────────────

    private static void SendFullKeyEvents(IntPtr hwnd, IEnumerable<char> chars, int keyDelayMs)
    {
        foreach (var c in chars)
        {
            ushort vk = CharToVk(c);
            if (vk != 0)
                SendMsg(hwnd, NativeMethods.WM_KEYDOWN, (IntPtr)vk, BuildKeyLParam(vk, false));
            SendMsg(hwnd, NativeMethods.WM_CHAR, (IntPtr)c, BuildCharLParam(vk));
            if (vk != 0)
                SendMsg(hwnd, NativeMethods.WM_KEYUP, (IntPtr)vk, BuildKeyLParam(vk, true));
            if (keyDelayMs > 0) Thread.Sleep(keyDelayMs);
        }
    }

    // ── PostMessage: WM_CHAR only (async) ────────────────────────────────────

    private static void PostCharOnlyEvents(IntPtr hwnd, IEnumerable<char> chars, int keyDelayMs)
    {
        foreach (var c in chars)
        {
            NativeMethods.PostMessage(hwnd, NativeMethods.WM_CHAR, (IntPtr)c, BuildCharLParam(CharToVk(c)));
            if (keyDelayMs > 0) Thread.Sleep(keyDelayMs);
        }
    }

    // ── SendMessage: WM_CHAR only (sync) ─────────────────────────────────────

    private static void SendCharOnlyEvents(IntPtr hwnd, IEnumerable<char> chars, int keyDelayMs)
    {
        foreach (var c in chars)
        {
            SendMsg(hwnd, NativeMethods.WM_CHAR, (IntPtr)c, BuildCharLParam(CharToVk(c)));
            if (keyDelayMs > 0) Thread.Sleep(keyDelayMs);
        }
    }

    // ── SendInput: Unicode (KEYEVENTF_UNICODE, no VK codes) ─────────────────

    private static void SendInputUnicodeChar(char c)
    {
        var inputs = new NativeMethods.INPUT[]
        {
            MakeUnicodeInput(c, false),
            MakeUnicodeInput(c, true),
        };
        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT MakeUnicodeInput(char c, bool keyUp) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new() { ki = new()
        {
            wVk = 0,
            wScan = c,
            dwFlags = NativeMethods.KEYEVENTF_UNICODE | (keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0),
        }}
    };

    // ── SendInput: VK codes + scan codes ─────────────────────────────────────

    private static void SendInputVKChar(char c)
    {
        var (vk, shift) = CharToVkFull(c);
        if (vk == 0) return;
        var scan = (ushort)(NativeMethods.MapVirtualKey(vk, 0) & 0xFF);

        if (shift) SendInputKey(NativeMethods.VK_SHIFT, false);

        var inputs = new NativeMethods.INPUT[]
        {
            MakeVKInput(vk, scan, false),
            MakeVKInput(vk, scan, true),
        };
        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());

        if (shift) SendInputKey(NativeMethods.VK_SHIFT, true);
    }

    private static NativeMethods.INPUT MakeVKInput(ushort vk, ushort scan, bool keyUp) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new() { ki = new()
        {
            wVk = vk,
            wScan = scan,
            dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
        }}
    };

    private static void SendInputKey(ushort vk, bool keyUp)
    {
        var scan = (ushort)(NativeMethods.MapVirtualKey(vk, 0) & 0xFF);
        var inputs = new NativeMethods.INPUT[]
        {
            MakeVKInput(vk, scan, keyUp),
        };
        NativeMethods.SendInput(1, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    // ── SendInput: raw scan codes (KEYEVENTF_SCANCODE, no VK) ───────────────

    private static void SendInputScancodeChar(char c)
    {
        var (vk, shift) = CharToVkFull(c);
        if (vk == 0) return;
        var scan = (ushort)(NativeMethods.MapVirtualKey(vk, 0) & 0xFF);

        if (shift)
        {
            var shiftScan = (ushort)(NativeMethods.MapVirtualKey(NativeMethods.VK_SHIFT, 0) & 0xFF);
            SendInputRawScan(shiftScan, false);
        }

        SendInputRawScan(scan, false);
        SendInputRawScan(scan, true);

        if (shift)
        {
            var shiftScan = (ushort)(NativeMethods.MapVirtualKey(NativeMethods.VK_SHIFT, 0) & 0xFF);
            SendInputRawScan(shiftScan, true);
        }
    }

    private static void SendInputRawScan(ushort scanCode, bool keyUp)
    {
        uint flags = NativeMethods.KEYEVENTF_SCANCODE;
        if (keyUp) flags |= NativeMethods.KEYEVENTF_KEYUP;
        var inputs = new NativeMethods.INPUT[]
        {
            new()
            {
                type = NativeMethods.INPUT_KEYBOARD,
                U = new() { ki = new() { wVk = 0, wScan = scanCode, dwFlags = flags } }
            },
        };
        NativeMethods.SendInput(1, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    // ── keybd_event: legacy hardware injection ───────────────────────────────

    private static void SendKeybdEventChar(char c)
    {
        var (vk, shift) = CharToVkFull(c);
        if (vk == 0) return;
        byte scan = (byte)(NativeMethods.MapVirtualKey(vk, 0) & 0xFF);
        byte shiftScan = (byte)(NativeMethods.MapVirtualKey(NativeMethods.VK_SHIFT, 0) & 0xFF);

        if (shift) NativeMethods.keybd_event((byte)NativeMethods.VK_SHIFT, shiftScan, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(vk, scan, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(vk, scan, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        if (shift) NativeMethods.keybd_event((byte)NativeMethods.VK_SHIFT, shiftScan, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
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

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static void SendMsg(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        NativeMethods.SendMessageTimeout(hwnd, msg, wParam, lParam,
            NativeMethods.SMTO_ABORTIFHUNG, 200, out _);
    }

    internal static IntPtr BuildKeyLParam(ushort vk, bool keyUp)
    {
        var scanCode = NativeMethods.MapVirtualKey(vk, 0) & 0xFF;
        long value = 1 | (scanCode << 16);
        if (keyUp) value |= 1L << 30 | 1L << 31;
        return (IntPtr)value;
    }

    internal static IntPtr BuildCharLParam(ushort vk)
    {
        if (vk == 0) return (IntPtr)1;
        var scanCode = NativeMethods.MapVirtualKey(vk, 0) & 0xFF;
        long value = 1 | (scanCode << 16);
        return (IntPtr)value;
    }

    /// <summary>Maps a character to its Windows virtual-key code (simple mapping).</summary>
    internal static ushort CharToVk(char c) =>
        c is >= 'a' and <= 'z' ? (ushort)(c - 32) :
        c is >= 'A' and <= 'Z' ? (ushort)c :
        c is >= '0' and <= '9' ? (ushort)c :
        c == ' '  ? NativeMethods.VK_SPACE :
        c == '\r' ? NativeMethods.VK_RETURN :
        (ushort)0;

    /// <summary>
    /// Maps a character to its VK code and shift state.
    /// Uses deterministic rules for common characters and falls back to VkKeyScanW
    /// for everything else (punctuation, locale-specific characters).
    /// </summary>
    internal static (byte vk, bool shift) CharToVkFull(char c)
    {
        if (c == '\r') return ((byte)NativeMethods.VK_RETURN, false);
        if (c == ' ')  return ((byte)NativeMethods.VK_SPACE, false);
        if (c is >= 'a' and <= 'z') return ((byte)(c - 32), false);
        if (c is >= 'A' and <= 'Z') return ((byte)c, true);
        if (c is >= '0' and <= '9') return ((byte)c, false);

        // Fall back to the system keyboard layout for other characters
        short result = NativeMethods.VkKeyScanW(c);
        if (result == -1) return (0, false);
        return ((byte)(result & 0xFF), (result & 0x100) != 0);
    }
}
