namespace PunyPlayer;

public enum LineType
{
    Text,
    Comment,
    Enter,
    Space,
    Delay,
    Empty,
    /// <summary>! WIN="filter": text — send text to windows whose title contains filter</summary>
    Win,
    /// <summary>! EXEC="path" — start executable at path</summary>
    Exec,
    /// <summary>! SWAP(y,z) — swap two characters in all following text lines</summary>
    Swap,
}

public record TranscriptLine(
    int LineNumber,
    string RawText,
    LineType Type,
    int DelayMs = 0,
    string WindowFilter = "",
    string CommandText = "",
    string ExecPath = "",
    char SwapFrom = '\0',
    char SwapTo = '\0');

public class TranscriptReader
{
    private readonly List<string> _lines = [];

    public int LineCount => _lines.Count;

    public void Load(string filePath)
    {
        _lines.Clear();
        if (File.Exists(filePath))
            _lines.AddRange(File.ReadAllLines(filePath));
    }

    public void LoadFromLines(IEnumerable<string> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
    }

    public string GetRawLine(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > _lines.Count) return "";
        return _lines[lineNumber - 1];
    }

    public TranscriptLine Parse(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > _lines.Count)
            return new TranscriptLine(lineNumber, "", LineType.Empty);

        var raw = _lines[lineNumber - 1];

        if (string.IsNullOrWhiteSpace(raw))
            return new TranscriptLine(lineNumber, raw, LineType.Empty);

        if (raw.TrimStart().StartsWith('#'))
            return new TranscriptLine(lineNumber, raw, LineType.Comment);

        var trimmed = raw.Trim();

        var enterMatch = System.Text.RegularExpressions.Regex.Match(
            trimmed, @"^!\s*(E|Enter)\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (enterMatch.Success)
            return new TranscriptLine(lineNumber, raw, LineType.Enter);

        var spaceMatch = System.Text.RegularExpressions.Regex.Match(
            trimmed, @"^!\s*(S|Space)\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (spaceMatch.Success)
            return new TranscriptLine(lineNumber, raw, LineType.Space);

        var delayMatch = System.Text.RegularExpressions.Regex.Match(
            trimmed, @"^!\s*Delay\s+(\d+)\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (delayMatch.Success && int.TryParse(delayMatch.Groups[1].Value, out var ms) && ms >= 0)
            return new TranscriptLine(lineNumber, raw, LineType.Delay, ms);

        // ! WIN="filter": text to send
        var winMatch = System.Text.RegularExpressions.Regex.Match(
            trimmed, @"^! WIN=""([^""]*)""\s*:\s*(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (winMatch.Success)
            return new TranscriptLine(lineNumber, raw, LineType.Win,
                WindowFilter: winMatch.Groups[1].Value,
                CommandText: winMatch.Groups[2].Value.TrimEnd());

        // ! EXEC="path"
        var execMatch = System.Text.RegularExpressions.Regex.Match(
            trimmed, @"^! EXEC=""([^""]*)""$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (execMatch.Success)
            return new TranscriptLine(lineNumber, raw, LineType.Exec,
                ExecPath: execMatch.Groups[1].Value);

        // ! SWAP(a,b) — case-insensitive single-character swap for following lines
        var swapMatch = System.Text.RegularExpressions.Regex.Match(
            trimmed, @"^!\s*SWAP\((.),(.)?\)\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (swapMatch.Success)
        {
            char from = swapMatch.Groups[1].Value[0];
            char to   = swapMatch.Groups[2].Success ? swapMatch.Groups[2].Value[0] : '\0';
            if (to != '\0' && from != to)
                return new TranscriptLine(lineNumber, raw, LineType.Swap, SwapFrom: from, SwapTo: to);
        }

        return new TranscriptLine(lineNumber, raw, LineType.Text);
    }

    /// <summary>
    /// Applies all active character swaps to the given text.
    /// Each entry swaps from ↔ to (case-insensitive, preserving original case).
    /// </summary>
    internal static string ApplySwaps(string text, IReadOnlyList<(char From, char To)> swaps)
    {
        if (swaps.Count == 0) return text;
        var result = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            char lower = char.ToLowerInvariant(c);
            bool replaced = false;
            foreach (var (from, to) in swaps)
            {
                if (lower == char.ToLowerInvariant(from))
                {
                    result.Append(char.IsUpper(c) ? char.ToUpperInvariant(to) : char.ToLowerInvariant(to));
                    replaced = true;
                    break;
                }
                if (lower == char.ToLowerInvariant(to))
                {
                    result.Append(char.IsUpper(c) ? char.ToUpperInvariant(from) : char.ToLowerInvariant(from));
                    replaced = true;
                    break;
                }
            }
            if (!replaced) result.Append(c);
        }
        return result.ToString();
    }

    public int ClampLine(int lineNumber)
    {
        if (_lines.Count == 0) return 1;
        return Math.Clamp(lineNumber, 1, _lines.Count);
    }
}
