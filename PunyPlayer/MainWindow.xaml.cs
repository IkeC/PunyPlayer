using System.Text.RegularExpressions;
using System.Windows;
using Key = System.Windows.Input.Key;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using TextCompositionEventArgs = System.Windows.Input.TextCompositionEventArgs;

namespace PunyPlayer;

public partial class MainWindow : Window
{
    private readonly TranscriptReader _reader = new();
    private readonly List<(IntPtr Handle, string Title)> _windows = [];
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private AppSettings _loadedSettings = new();

    public MainWindow()
    {
        InitializeComponent();
        // Restore window geometry before the window is shown
        _loadedSettings = AppSettings.Load();
        if (_loadedSettings.WindowWidth > 0) Width = _loadedSettings.WindowWidth;
        if (_loadedSettings.WindowHeight > 0) Height = _loadedSettings.WindowHeight;
        if (_loadedSettings.WindowLeft != 0 || _loadedSettings.WindowTop != 0)
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
            Left = _loadedSettings.WindowLeft;
            Top = _loadedSettings.WindowTop;
        }
        Loaded += OnLoaded;
    }

    private class SendMethodItem
    {
        public SendMethod Method { get; init; }
        public override string ToString() => Method.DisplayName();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var s = _loadedSettings;
        FilePathBox.Text = s.FilePath;
        DelayBox.Text = s.Delay.ToString();
        KeyDelayBox.Text = s.KeyDelay.ToString();
        LineBox.Text = "1";

        // Populate send-method combobox
        SendMethodCombo.ItemsSource = SendMethodExtensions.All
            .Select(m => new SendMethodItem { Method = m }).ToList();
        var saved = Enum.TryParse<SendMethod>(s.SendMethod, true, out var parsed)
            ? parsed : SendMethod.PostMessage;
        SendMethodCombo.SelectedIndex = SendMethodExtensions.All
            .Select((m, i) => (m, i)).FirstOrDefault(x => x.m == saved).i;

        RefreshWindows(s);
        ReloadTranscript();
        UpdateLinePreview();
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => RefreshWindows();

    private void RefreshWindows(AppSettings? settings = null)
    {
        var prev1 = WindowCombo1.SelectedItem?.ToString();
        var prev2 = WindowCombo2.SelectedItem?.ToString();

        _windows.Clear();
        _windows.AddRange(NativeMethods.GetVisibleWindows()
            .OrderBy(w => w.Title, StringComparer.OrdinalIgnoreCase));

        var titles = new List<string> { "" };
        titles.AddRange(_windows.Select(w => w.Title));
        foreach (var combo in new[] { WindowCombo1, WindowCombo2 })
        {
            combo.ItemsSource = null;
            combo.ItemsSource = titles;
        }

        RestoreSelection(WindowCombo1, prev1 ?? settings?.SelectedWindow1);
        RestoreSelection(WindowCombo2, prev2 ?? settings?.SelectedWindow2);
    }

    private static void RestoreSelection(System.Windows.Controls.ComboBox combo, string? title)
    {
        title ??= "";
        var idx = combo.Items.IndexOf(title);
        combo.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Select walkthrough file"
        };
        if (dlg.ShowDialog() == true)
        {
            FilePathBox.Text = dlg.FileName;
            ReloadTranscript();
        }
    }

    private void OnFilePathChanged(object sender, RoutedEventArgs e) => ReloadTranscript();

    private void ReloadTranscript()
    {
        _reader.Load(ResolveFilePath(FilePathBox.Text));
        ClampLineInput();
        UpdateLinePreview();
        LineCountBox.Text = _reader.LineCount > 0 ? _reader.LineCount.ToString() : "0";
    }

    private static string ResolveFilePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    // --- Line spinner ---

    private void OnLineTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateLinePreview();
    }

    private void OnLineKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)   { AdjustLine(+1); e.Handled = true; }
        else if (e.Key == Key.Down) { AdjustLine(-1); e.Handled = true; }
    }

    private void OnLineMouseWheel(object sender, MouseWheelEventArgs e)
    {
        AdjustLine(e.Delta > 0 ? 1 : -1);
        e.Handled = true;
    }

    private void OnDelayMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!int.TryParse(DelayBox.Text, out var val)) val = 200;
        val = Math.Max(0, val + (e.Delta > 0 ? 100 : -100));
        DelayBox.Text = val.ToString();
        e.Handled = true;
    }

    private void OnKeyDelayMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!int.TryParse(KeyDelayBox.Text, out var val)) val = 30;
        val = Math.Max(0, val + (e.Delta > 0 ? 5 : -5));
        KeyDelayBox.Text = val.ToString();
        e.Handled = true;
    }

    private void AdjustLine(int delta)
    {
        if (!int.TryParse(LineBox.Text, out var line)) line = 1;
        line = _reader.ClampLine(line + delta);
        LineBox.Text = line.ToString();
        LineBox.SelectAll();
    }

    private void ClampLineInput()
    {
        if (!int.TryParse(LineBox.Text, out var line)) line = 1;
        line = _reader.ClampLine(line);
        LineBox.Text = line.ToString();
    }

    private void UpdateLinePreview()
    {
        if (!int.TryParse(LineBox.Text, out var line)) line = 1;
        LinePreview.Text = _reader.GetRawLine(line);
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        LineBox.Text = "1";
        LineBox.SelectAll();
    }

    private void OnNumericInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }

    private void OnNumericPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!Regex.IsMatch(text, @"^\d+$")) e.CancelCommand();
        }
        else e.CancelCommand();
    }

    // --- Playback ---

    private List<IntPtr> GetSelectedHandles()
    {
        var handles = new List<IntPtr>();
        foreach (var combo in new[] { WindowCombo1, WindowCombo2 })
        {
            if (combo.SelectedIndex > 0 && combo.SelectedIndex <= _windows.Count)
                handles.Add(_windows[combo.SelectedIndex - 1].Handle);
        }
        return handles;
    }

    private async void OnRunToggle(object sender, RoutedEventArgs e)
    {
        if (_isRunning) { _cts?.Cancel(); return; }

        var handles = GetSelectedHandles();
        if (handles.Count == 0)
        {
            System.Windows.MessageBox.Show("Please select at least one target window.", "PunyPlayer",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isRunning = true;
        RunButton.Content = "Stop";
        RunButton.Style = (Style)FindResource("StopButton");
        SetControlsEnabled(false);
        var method     = (SendMethodCombo.SelectedItem as SendMethodItem)?.Method ?? SendMethod.PostMessage;
        int keyDelayMs = int.TryParse(KeyDelayBox.Text, out var kd) ? kd : 30;

        using var cts = new CancellationTokenSource();
        _cts = cts;

        try { await PlaybackLoop(handles, method, keyDelayMs, cts.Token); }
        catch (OperationCanceledException) { }
        finally
        {
            _cts = null;
            _isRunning = false;
            RunButton.Content = "Run";
            RunButton.Style = (Style)FindResource("AccentButton");
            SetControlsEnabled(true);
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        WindowCombo1.IsEnabled = enabled;
        WindowCombo2.IsEnabled = enabled;
        RefreshButton.IsEnabled = enabled;
        FilePathBox.IsEnabled = enabled;
        BrowseButton.IsEnabled = enabled;
        KeyDelayBox.IsEnabled = enabled;
        SendMethodCombo.IsEnabled = enabled;
        DelayBox.IsEnabled = enabled;
        LineBox.IsEnabled = enabled;
        ResetButton.IsEnabled = enabled;
    }

    private async Task PlaybackLoop(List<IntPtr> handles, SendMethod method, int keyDelayMs, CancellationToken ct)
    {
        int delay = int.TryParse(DelayBox.Text, out var d) ? d : 1000;
        var activeSwaps = new List<(char From, char To)>();

        while (!ct.IsCancellationRequested)
        {
            if (!int.TryParse(LineBox.Text, out var line)) break;
            if (line > _reader.LineCount) break;

            var parsed = _reader.Parse(line);
            UpdateLinePreview();

            switch (parsed.Type)
            {
                case LineType.Comment:
                case LineType.Empty:
                    break;

                case LineType.Enter:
                    foreach (var h in handles)
                        TextSender.SendEnter(h, method);
                    await Task.Delay(delay, ct);
                    break;

                case LineType.Space:
                    foreach (var h in handles)
                        TextSender.SendSpace(h, method);
                    await Task.Delay(delay, ct);
                    break;

                case LineType.Delay:
                    await Task.Delay(parsed.DelayMs, ct);
                    break;

                case LineType.Text:
                    foreach (var h in handles)
                        TextSender.SendCommand(h, TranscriptReader.ApplySwaps(parsed.RawText, activeSwaps), method, keyDelayMs);
                    await Task.Delay(delay, ct);
                    break;

                case LineType.Win:
                    var matchingHandles = NativeMethods.GetVisibleWindows()
                        .Where(w => w.Title.Contains(parsed.WindowFilter, StringComparison.OrdinalIgnoreCase))
                        .Select(w => w.Handle)
                        .ToList();
                    foreach (var h in matchingHandles)
                        TextSender.SendCommand(h, TranscriptReader.ApplySwaps(parsed.CommandText, activeSwaps), method, keyDelayMs);
                    await Task.Delay(delay, ct);
                    break;

                case LineType.Swap:
                    activeSwaps.Add((parsed.SwapFrom, parsed.SwapTo));
                    break;

                case LineType.Exec:
                    try
                    {
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo(parsed.ExecPath)
                            { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        // EXEC failed, continue playback
                    }
                    await Task.Delay(delay, ct);
                    break;
            }

            if (parsed.Type is LineType.Comment or LineType.Empty or LineType.Swap)
            {
                if (line >= _reader.LineCount) break;
                LineBox.Text = (line + 1).ToString();
                continue;
            }

            if (line >= _reader.LineCount) break;
            LineBox.Text = (line + 1).ToString();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        int.TryParse(DelayBox.Text, out var delay);
        int.TryParse(KeyDelayBox.Text, out var keyDelay);
        int.TryParse(LineBox.Text, out var currentLine);
        new AppSettings
        {
            SelectedWindow1 = WindowCombo1.SelectedItem?.ToString() ?? "",
            SelectedWindow2 = WindowCombo2.SelectedItem?.ToString() ?? "",
            SelectedWindow3 = "",
            FilePath = FilePathBox.Text,
            Delay = delay > 0 ? delay : 1500,
            KeyDelay = keyDelay >= 0 ? keyDelay : 30,
            CurrentLine = currentLine > 0 ? currentLine : 1,
            SendMethod = ((SendMethodCombo.SelectedItem as SendMethodItem)?.Method ?? SendMethod.PostMessage).ToString(),
            WindowLeft = Left,
            WindowTop = Top,
            WindowWidth = Width,
            WindowHeight = Height
        }.Save();
        base.OnClosing(e);
    }
}
