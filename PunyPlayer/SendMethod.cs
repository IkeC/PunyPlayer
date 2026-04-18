namespace PunyPlayer;

/// <summary>
/// All available keystroke delivery methods.
/// Background methods send messages directly to the target window handle without focus.
/// Focus methods bring the target window to the foreground first.
/// </summary>
public enum SendMethod
{
    /// <summary>Background: WM_KEYDOWN + WM_CHAR + WM_KEYUP via PostMessage (async).</summary>
    PostMessage,

    /// <summary>Background: WM_KEYDOWN + WM_CHAR + WM_KEYUP via SendMessage (sync).</summary>
    SendMessage,

    /// <summary>Background: WM_CHAR only via PostMessage (async, no key-down/up).</summary>
    PostCharOnly,

    /// <summary>Background: WM_CHAR only via SendMessage (sync, no key-down/up).</summary>
    SendCharOnly,

    /// <summary>Focus: System.Windows.Forms.SendKeys.SendWait — journal-hook based.</summary>
    SendKeys,

    /// <summary>Focus: SendInput with KEYEVENTF_UNICODE — hardware-level Unicode chars.</summary>
    InputUnicode,

    /// <summary>Focus: SendInput with virtual-key codes + scan codes.</summary>
    InputVK,

    /// <summary>Focus: SendInput with KEYEVENTF_SCANCODE — raw hardware scan codes only.</summary>
    InputScancode,

    /// <summary>Focus: Legacy keybd_event API — sometimes bypasses UIPI restrictions.</summary>
    KeybdEvent,
}

public static class SendMethodExtensions
{
    private static readonly Dictionary<SendMethod, string> DisplayNames = new()
    {
        [SendMethod.PostMessage]   = "PostMessage",
        [SendMethod.SendMessage]   = "SendMessage",
        [SendMethod.PostCharOnly]  = "Post WM_CHAR only",
        [SendMethod.SendCharOnly]  = "Send WM_CHAR only",
        [SendMethod.SendKeys]      = "SendKeys (focus)",
        [SendMethod.InputUnicode]  = "SendInput Unicode (focus)",
        [SendMethod.InputVK]       = "SendInput VK (focus)",
        [SendMethod.InputScancode] = "SendInput Scan (focus)",
        [SendMethod.KeybdEvent]    = "keybd_event (focus)",
    };

    public static string DisplayName(this SendMethod m) =>
        DisplayNames.TryGetValue(m, out var name) ? name : m.ToString();

    /// <summary>True if this method requires the target window to be in the foreground.</summary>
    public static bool RequiresFocus(this SendMethod m) =>
        m >= SendMethod.SendKeys;

    /// <summary>All defined send methods in declaration order.</summary>
    public static IReadOnlyList<SendMethod> All { get; } =
        Enum.GetValues<SendMethod>();
}
