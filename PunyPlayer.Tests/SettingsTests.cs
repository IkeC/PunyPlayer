namespace PunyPlayer.Tests;

public class SettingsTests
{
    [Fact]
    public void DefaultValues()
    {
        var s = new AppSettings();
        Assert.Equal("", s.SelectedWindow1);
        Assert.Equal("", s.SelectedWindow2);
        Assert.Equal("", s.SelectedWindow3);
        Assert.Equal("example.txt", s.FilePath);
        Assert.Equal(1500, s.Delay);
        Assert.Equal(30, s.KeyDelay);
        Assert.Equal(1, s.CurrentLine);
        Assert.Equal("SendKeys", s.SendMethod);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            var original = new AppSettings
            {
                SelectedWindow1 = "Test Window",
                SelectedWindow2 = "Second Window",
                SelectedWindow3 = "",
                FilePath = @"C:\test\walkthrough.txt",
                Delay = 500,
                CurrentLine = 42
            };
            original.Save(tmp);

            var loaded = AppSettings.Load(tmp);
            Assert.Equal(original.SelectedWindow1, loaded.SelectedWindow1);
            Assert.Equal(original.SelectedWindow2, loaded.SelectedWindow2);
            Assert.Equal(original.SelectedWindow3, loaded.SelectedWindow3);
            Assert.Equal(original.FilePath, loaded.FilePath);
            Assert.Equal(original.Delay, loaded.Delay);
            Assert.Equal(original.CurrentLine, loaded.CurrentLine);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var s = AppSettings.Load("nonexistent_settings_12345.jsonc");
        Assert.Equal("example.txt", s.FilePath);
        Assert.Equal(1500, s.Delay);
        Assert.Equal(1, s.CurrentLine);
    }

    [Fact]
    public void Save_CreatesFileWithComments()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            new AppSettings().Save(tmp);
            var content = File.ReadAllText(tmp);
            Assert.Contains("//", content);
            Assert.Contains("selectedWindow1", content);
            Assert.Contains("selectedWindow2", content);
            Assert.Contains("selectedWindow3", content);
            Assert.Contains("filePath", content);
            Assert.Contains("delay", content);
            Assert.Contains("keyDelay", content);
            Assert.Contains("currentLine", content);
            Assert.Contains("sendMethod", content);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Save_SpecialCharactersInWindow_RoundTrip()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            var s = new AppSettings { SelectedWindow1 = "Window \"with\" quotes" };
            s.Save(tmp);
            var loaded = AppSettings.Load(tmp);
            Assert.Equal("Window \"with\" quotes", loaded.SelectedWindow1);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Save_BackslashesInPath_RoundTrip()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            var s = new AppSettings { FilePath = @"C:\Users\test\walkthrough.txt" };
            s.Save(tmp);
            var loaded = AppSettings.Load(tmp);
            Assert.Equal(@"C:\Users\test\walkthrough.txt", loaded.FilePath);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaults()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, "not valid json{{{");
            var s = AppSettings.Load(tmp);
            Assert.Equal("example.txt", s.FilePath);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Load_JsonWithComments_Succeeds()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, """
                {
                  // A comment
                  "filePath": "custom.txt",
                  "delay": 250
                }
                """);
            var s = AppSettings.Load(tmp);
            Assert.Equal("custom.txt", s.FilePath);
            Assert.Equal(250, s.Delay);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void ThreeWindows_AllSavedAndRestored()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            var s = new AppSettings
            {
                SelectedWindow1 = "Win A",
                SelectedWindow2 = "Win B",
                SelectedWindow3 = "Win C"
            };
            s.Save(tmp);
            var loaded = AppSettings.Load(tmp);
            Assert.Equal("Win A", loaded.SelectedWindow1);
            Assert.Equal("Win B", loaded.SelectedWindow2);
            Assert.Equal("Win C", loaded.SelectedWindow3);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void BackwardsCompat_OldSelectedWindow_MapsToWindow1()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, """{ "selectedWindow": "OldWin" }""");
            var loaded = AppSettings.Load(tmp);
            Assert.Equal("OldWin", loaded.SelectedWindow1);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void BackwardsCompat_FocusModeTrue_MapsSendKeys()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, """{ "focusMode": true }""");
            var loaded = AppSettings.Load(tmp);
            Assert.Equal("SendKeys", loaded.SendMethod);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void BackwardsCompat_FocusModeFalse_MapsPostMessage()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, """{ "focusMode": false }""");
            var loaded = AppSettings.Load(tmp);
            Assert.Equal("PostMessage", loaded.SendMethod);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void SendMethod_SaveAndLoad_RoundTrip()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            var s = new AppSettings { SendMethod = "InputVK" };
            s.Save(tmp);
            var loaded = AppSettings.Load(tmp);
            Assert.Equal("InputVK", loaded.SendMethod);
        }
        finally { File.Delete(tmp); }
    }

    // --- Window geometry ---

    [Fact]
    public void WindowGeometry_DefaultsToZero()
    {
        var s = new AppSettings();
        Assert.Equal(0, s.WindowLeft);
        Assert.Equal(0, s.WindowTop);
        Assert.Equal(0, s.WindowWidth);
        Assert.Equal(0, s.WindowHeight);
    }

    [Fact]
    public void WindowGeometry_SaveAndLoad_RoundTrip()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            var s = new AppSettings { WindowLeft = 120, WindowTop = 80, WindowWidth = 700, WindowHeight = 420 };
            s.Save(tmp);
            var loaded = AppSettings.Load(tmp);
            Assert.Equal(120, loaded.WindowLeft);
            Assert.Equal(80, loaded.WindowTop);
            Assert.Equal(700, loaded.WindowWidth);
            Assert.Equal(420, loaded.WindowHeight);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Save_ContainsWindowGeometryKeys()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            new AppSettings().Save(tmp);
            var content = File.ReadAllText(tmp);
            Assert.Contains("windowLeft", content);
            Assert.Contains("windowTop", content);
            Assert.Contains("windowWidth", content);
            Assert.Contains("windowHeight", content);
        }
        finally { File.Delete(tmp); }
    }
}
