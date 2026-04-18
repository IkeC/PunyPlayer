namespace PunyPlayer.Tests;

public class NativeMethodsTests
{
    private const long WS_EX_TOOLWINDOW = 0x80;
    private const long WS_EX_APPWINDOW = 0x40000;

    [Fact]
    public void PassesWindowFilter_NormalWindow_ReturnsTrue()
    {
        Assert.True(NativeMethods.PassesWindowFilter("Notepad", 0, isOwnRootOwner: true));
    }

    [Fact]
    public void PassesWindowFilter_EmptyTitle_ReturnsFalse()
    {
        Assert.False(NativeMethods.PassesWindowFilter("", 0, isOwnRootOwner: true));
    }

    [Fact]
    public void PassesWindowFilter_WhitespaceTitle_ReturnsFalse()
    {
        Assert.False(NativeMethods.PassesWindowFilter("   ", 0, isOwnRootOwner: true));
    }

    [Fact]
    public void PassesWindowFilter_OwnedWindow_ReturnsFalse()
    {
        Assert.False(NativeMethods.PassesWindowFilter("Dialog", 0, isOwnRootOwner: false));
    }

    [Fact]
    public void PassesWindowFilter_ToolWindowNoAppWindow_ReturnsFalse()
    {
        // Pure tool window (e.g. notification balloon) — no taskbar button
        Assert.False(NativeMethods.PassesWindowFilter("Tool", WS_EX_TOOLWINDOW, isOwnRootOwner: true));
    }

    [Fact]
    public void PassesWindowFilter_ToolWindowWithAppWindow_ReturnsTrue()
    {
        // Tool window explicitly forced onto taskbar via WS_EX_APPWINDOW
        long style = WS_EX_TOOLWINDOW | WS_EX_APPWINDOW;
        Assert.True(NativeMethods.PassesWindowFilter("Forced", style, isOwnRootOwner: true));
    }

    [Fact]
    public void PassesWindowFilter_AppWindowOnly_ReturnsTrue()
    {
        Assert.True(NativeMethods.PassesWindowFilter("App", WS_EX_APPWINDOW, isOwnRootOwner: true));
    }

    [Fact]
    public void PassesWindowFilter_OwnedToolWindow_ReturnsFalse()
    {
        Assert.False(NativeMethods.PassesWindowFilter("Tooltip", WS_EX_TOOLWINDOW, isOwnRootOwner: false));
    }

    [Fact]
    public void PassesWindowFilter_Einstellungen_ReturnsFalse()
    {
        Assert.False(NativeMethods.PassesWindowFilter("Einstellungen", 0, isOwnRootOwner: true));
    }

    [Fact]
    public void PassesWindowFilter_WindowsEingabeerfahrung_ReturnsFalse()
    {
        Assert.False(NativeMethods.PassesWindowFilter("Windows-Eingabeerfahrung", 0, isOwnRootOwner: true));
    }

    [Fact]
    public void PassesWindowFilter_PunyPlayer_ReturnsFalse()
    {
        Assert.False(NativeMethods.PassesWindowFilter("PunyPlayer", 0, isOwnRootOwner: true));
    }

    [Fact]
    public void PassesWindowFilter_PunyPlayerInTitle_ReturnsFalse()
    {
        Assert.False(NativeMethods.PassesWindowFilter("PunyPlayer - walkthrough.txt", 0, isOwnRootOwner: true));
    }
}
