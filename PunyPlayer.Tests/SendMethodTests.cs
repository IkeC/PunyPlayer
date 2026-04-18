namespace PunyPlayer.Tests;

public class SendMethodTests
{
    [Fact]
    public void All_ContainsEveryEnumValue()
    {
        var enumValues = Enum.GetValues<SendMethod>();
        Assert.Equal(enumValues.Length, SendMethodExtensions.All.Count);
        foreach (var v in enumValues)
            Assert.Contains(v, SendMethodExtensions.All);
    }

    [Theory]
    [InlineData(SendMethod.PostMessage, false)]
    [InlineData(SendMethod.SendMessage, false)]
    [InlineData(SendMethod.PostCharOnly, false)]
    [InlineData(SendMethod.SendCharOnly, false)]
    [InlineData(SendMethod.SendKeys, true)]
    [InlineData(SendMethod.InputUnicode, true)]
    [InlineData(SendMethod.InputVK, true)]
    [InlineData(SendMethod.InputScancode, true)]
    [InlineData(SendMethod.KeybdEvent, true)]
    public void RequiresFocus_CorrectForEachMethod(SendMethod method, bool expected)
    {
        Assert.Equal(expected, method.RequiresFocus());
    }

    [Fact]
    public void DisplayName_NeverNullOrEmpty()
    {
        foreach (var m in SendMethodExtensions.All)
            Assert.False(string.IsNullOrWhiteSpace(m.DisplayName()));
    }

    [Fact]
    public void DisplayName_AllUnique()
    {
        var names = SendMethodExtensions.All.Select(m => m.DisplayName()).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void EnumNames_RoundTrip()
    {
        foreach (var m in SendMethodExtensions.All)
        {
            var name = m.ToString();
            Assert.True(Enum.TryParse<SendMethod>(name, out var parsed));
            Assert.Equal(m, parsed);
        }
    }

    [Fact]
    public void BackgroundMethods_Count()
    {
        var bg = SendMethodExtensions.All.Where(m => !m.RequiresFocus()).ToList();
        Assert.Equal(4, bg.Count);
    }

    [Fact]
    public void FocusMethods_Count()
    {
        var fg = SendMethodExtensions.All.Where(m => m.RequiresFocus()).ToList();
        Assert.Equal(5, fg.Count);
    }

    [Fact]
    public void TotalMethods_IsNine()
    {
        Assert.Equal(9, SendMethodExtensions.All.Count);
    }

    [Theory]
    [InlineData(SendMethod.PostMessage, "PostMessage")]
    [InlineData(SendMethod.SendKeys, "SendKeys (focus)")]
    [InlineData(SendMethod.InputVK, "SendInput VK (focus)")]
    [InlineData(SendMethod.KeybdEvent, "keybd_event (focus)")]
    public void DisplayName_MatchesExpected(SendMethod method, string expected)
    {
        Assert.Equal(expected, method.DisplayName());
    }
}
