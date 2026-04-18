namespace PunyPlayer.Tests;

public class TranscriptReaderTests
{
    [Fact]
    public void LineCount_EmptyReader_IsZero()
    {
        var reader = new TranscriptReader();
        Assert.Equal(0, reader.LineCount);
    }

    [Fact]
    public void LoadFromLines_SetsLineCount()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["north", "south", "east"]);
        Assert.Equal(3, reader.LineCount);
    }

    [Fact]
    public void GetRawLine_ValidLine_ReturnsContent()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["first", "second", "third"]);
        Assert.Equal("second", reader.GetRawLine(2));
    }

    [Fact]
    public void GetRawLine_BelowRange_ReturnsEmpty()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["only"]);
        Assert.Equal("", reader.GetRawLine(0));
    }

    [Fact]
    public void GetRawLine_AboveRange_ReturnsEmpty()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["only"]);
        Assert.Equal("", reader.GetRawLine(2));
    }

    [Fact]
    public void Parse_TextLine()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["north"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Text, line.Type);
        Assert.Equal("north", line.RawText);
    }

    [Fact]
    public void Parse_CommentLine()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["# this is a comment"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Comment, line.Type);
    }

    [Fact]
    public void Parse_CommentLine_WithLeadingSpaces()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["  # indented comment"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Comment, line.Type);
    }

    [Fact]
    public void Parse_EnterCommand()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! Enter"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Enter, line.Type);
    }

    [Fact]
    public void Parse_EnterCommand_CaseInsensitive()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! enter"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Enter, line.Type);
    }

    [Fact]
    public void Parse_ShortEnterCommand()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! E"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Enter, line.Type);
    }

    [Fact]
    public void Parse_ShortEnterCommand_NoSpaceAfterBang()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["!E"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Enter, line.Type);
    }

    [Fact]
    public void Parse_EnterCommand_WithSurroundingSpaces()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["  ! Enter  "]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Enter, line.Type);
    }

    [Fact]
    public void Parse_DelayCommand()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! Delay 200"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Delay, line.Type);
        Assert.Equal(200, line.DelayMs);
    }

    [Fact]
    public void Parse_DelayCommand_CaseInsensitive()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! delay 500"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Delay, line.Type);
        Assert.Equal(500, line.DelayMs);
    }

    [Fact]
    public void Parse_DelayCommand_LargeValue()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! Delay 5000"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Delay, line.Type);
        Assert.Equal(5000, line.DelayMs);
    }

    [Fact]
    public void Parse_DelayCommand_NoSpaceAfterBang()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["!Delay 250"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Delay, line.Type);
        Assert.Equal(250, line.DelayMs);
    }

    [Fact]
    public void Parse_DelayCommand_InvalidNumber_IsText()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! Delay abc"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Text, line.Type);
    }

    [Fact]
    public void Parse_DelayCommand_NegativeNumber_IsText()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! Delay -100"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Text, line.Type);
    }

    [Fact]
    public void Parse_EmptyLine()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines([""]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Empty, line.Type);
    }

    [Fact]
    public void Parse_WhitespaceLine_IsEmpty()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["   "]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Empty, line.Type);
    }

    [Fact]
    public void Parse_OutOfRange_IsEmpty()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["north"]);
        Assert.Equal(LineType.Empty, reader.Parse(0).Type);
        Assert.Equal(LineType.Empty, reader.Parse(5).Type);
    }

    [Fact]
    public void ClampLine_WithinRange_Unchanged()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["a", "b", "c"]);
        Assert.Equal(2, reader.ClampLine(2));
    }

    [Fact]
    public void ClampLine_BelowRange_ClampsToOne()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["a", "b"]);
        Assert.Equal(1, reader.ClampLine(0));
        Assert.Equal(1, reader.ClampLine(-5));
    }

    [Fact]
    public void ClampLine_AboveRange_ClampsToMax()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["a", "b"]);
        Assert.Equal(2, reader.ClampLine(10));
    }

    [Fact]
    public void ClampLine_EmptyReader_ReturnsOne()
    {
        var reader = new TranscriptReader();
        Assert.Equal(1, reader.ClampLine(5));
    }

    [Fact]
    public void Load_FromFile_ReadsLines()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tmp, ["line1", "line2"]);
            var reader = new TranscriptReader();
            reader.Load(tmp);
            Assert.Equal(2, reader.LineCount);
            Assert.Equal("line1", reader.GetRawLine(1));
            Assert.Equal("line2", reader.GetRawLine(2));
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Load_NonexistentFile_EmptyReader()
    {
        var reader = new TranscriptReader();
        reader.Load("nonexistent_file_12345.txt");
        Assert.Equal(0, reader.LineCount);
    }

    [Fact]
    public void Load_ClearsExistingLines()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["a", "b", "c"]);
        Assert.Equal(3, reader.LineCount);
        reader.LoadFromLines(["x"]);
        Assert.Equal(1, reader.LineCount);
        Assert.Equal("x", reader.GetRawLine(1));
    }

    [Fact]
    public void Parse_MixedContent_CorrectTypes()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines([
            "# comment",
            "look",
            "! Enter",
            "",
            "! Delay 300",
            "take lamp"
        ]);

        Assert.Equal(LineType.Comment, reader.Parse(1).Type);
        Assert.Equal(LineType.Text, reader.Parse(2).Type);
        Assert.Equal(LineType.Enter, reader.Parse(3).Type);
        Assert.Equal(LineType.Empty, reader.Parse(4).Type);
        Assert.Equal(LineType.Delay, reader.Parse(5).Type);
        Assert.Equal(LineType.Text, reader.Parse(6).Type);
    }

    // --- WIN= command ---

    [Fact]
    public void Parse_WinCommand_Basic()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! WIN=\"Agon\": exec playgame.txt"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Win, line.Type);
        Assert.Equal("Agon", line.WindowFilter);
        Assert.Equal("exec playgame.txt", line.CommandText);
    }

    [Fact]
    public void Parse_WinCommand_CaseInsensitive()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! win=\"Test\": hello world"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Win, line.Type);
        Assert.Equal("Test", line.WindowFilter);
        Assert.Equal("hello world", line.CommandText);
    }

    [Fact]
    public void Parse_WinCommand_EmptyFilter()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! WIN=\"\": some text"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Win, line.Type);
        Assert.Equal("", line.WindowFilter);
        Assert.Equal("some text", line.CommandText);
    }

    [Fact]
    public void Parse_WinCommand_ExtraSpacesAroundColon()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! WIN=\"filter\"  :  send this"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Win, line.Type);
        Assert.Equal("send this", line.CommandText);
    }

    [Fact]
    public void Parse_WinCommand_MissingColon_IsText()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! WIN=\"Agon\" send this"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Text, line.Type);
    }

    [Fact]
    public void Parse_WinCommand_RawText_Preserved()
    {
        var reader = new TranscriptReader();
        var raw = "! WIN=\"foo\": bar";
        reader.LoadFromLines([raw]);
        Assert.Equal(raw, reader.Parse(1).RawText);
    }

    // --- EXEC= command ---

    [Fact]
    public void Parse_ExecCommand_Basic()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! EXEC=\"C:\\Windows\\notepad.exe\""]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Exec, line.Type);
        Assert.Equal("C:\\Windows\\notepad.exe", line.ExecPath);
    }

    [Fact]
    public void Parse_ExecCommand_CaseInsensitive()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! exec=\"notepad.exe\""]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Exec, line.Type);
        Assert.Equal("notepad.exe", line.ExecPath);
    }

    [Fact]
    public void Parse_ExecCommand_EmptyPath()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! EXEC=\"\""]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Exec, line.Type);
        Assert.Equal("", line.ExecPath);
    }

    [Fact]
    public void Parse_ExecCommand_MissingQuotes_IsText()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! EXEC=notepad.exe"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Text, line.Type);
    }

    [Fact]
    public void Parse_ExecCommand_TrailingChars_IsText()
    {
        // Extra text after closing quote → falls through to Text
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! EXEC=\"notepad.exe\" extra"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Text, line.Type);
    }

    // --- Space command ---

    [Fact]
    public void Parse_SpaceCommand_LongForm()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! Space"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Space, line.Type);
    }

    [Fact]
    public void Parse_SpaceCommand_ShortForm()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! S"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Space, line.Type);
    }

    [Fact]
    public void Parse_SpaceCommand_CaseInsensitive()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["! space"]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Space, line.Type);
    }

    [Fact]
    public void Parse_SpaceCommand_WithSurroundingSpaces()
    {
        var reader = new TranscriptReader();
        reader.LoadFromLines(["  ! S  "]);
        var line = reader.Parse(1);
        Assert.Equal(LineType.Space, line.Type);
    }
}

