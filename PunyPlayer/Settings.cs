using System.Text.Json;

namespace PunyPlayer;

public class AppSettings
{
    public string SelectedWindow1 { get; set; } = "";
    public string SelectedWindow2 { get; set; } = "";
    public string SelectedWindow3 { get; set; } = "";
    public string FilePath { get; set; } = "example.txt";
    public int Delay { get; set; } = 1500;
    public int KeyDelay { get; set; } = 30;
    public int CurrentLine { get; set; } = 1;
    public bool FocusMode { get; set; } = true;
    public double WindowLeft { get; set; } = 0;
    public double WindowTop { get; set; } = 0;
    public double WindowWidth { get; set; } = 0;
    public double WindowHeight { get; set; } = 0;

    // Keep for backwards compat when loading old files
    public string SelectedWindow { set { SelectedWindow1 = value; } }

    public static string DefaultPath =>
        Path.Combine(AppContext.BaseDirectory, "PunyPlayer.jsonc");

    public static AppSettings Load(string? path = null)
    {
        path ??= DefaultPath;
        if (!File.Exists(path)) return new AppSettings();
        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<AppSettings>(json, options) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        var json = $$"""
            {
              // The title of the first selected target window
              "selectedWindow1": {{JsonSerializer.Serialize(SelectedWindow1)}},
              // The title of the second selected target window (optional)
              "selectedWindow2": {{JsonSerializer.Serialize(SelectedWindow2)}},
              // The title of the third selected target window (optional)
              "selectedWindow3": {{JsonSerializer.Serialize(SelectedWindow3)}},
              // Path to the walkthrough/transcript file
              "filePath": {{JsonSerializer.Serialize(FilePath)}},
              // Delay in milliseconds between sending commands
              "delay": {{Delay}},
              // Delay in milliseconds between each keypress (Focus Mode)
              "keyDelay": {{KeyDelay}},
              // The next line number to play (1-based)
              "currentLine": {{CurrentLine}},
              // Use SetForegroundWindow + SendKeys instead of PostMessage
              "focusMode": {{JsonSerializer.Serialize(FocusMode)}},
              // Window position and size (0 = use default placement)
              "windowLeft": {{WindowLeft}},
              "windowTop": {{WindowTop}},
              "windowWidth": {{WindowWidth}},
              "windowHeight": {{WindowHeight}}
            }
            """;
        File.WriteAllText(path, json);
    }
}
