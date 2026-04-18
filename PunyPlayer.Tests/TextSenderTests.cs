namespace PunyPlayer.Tests;

public class TextSenderTests
{
    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("", "")]
    [InlineData("normal text", "normal text")]
    public void BuildTextInput_ReturnsCharacters(string input, string expected)
    {
        Assert.Equal(expected.ToCharArray(), TextSender.BuildTextInput(input));
    }

    [Fact]
    public void BuildEnterInput_ReturnsCarriageReturn()
    {
        Assert.Equal(['\r'], TextSender.BuildEnterInput());
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("look +north", "look {+}north")]
    [InlineData("100%", "100{%}")]
    [InlineData("{key}", "{{}key{}}")]
    [InlineData("a~b^c", "a{~}b{^}c")]
    public void EscapeSendKeys_EscapesSpecialCharacters(string input, string expected)
    {
        Assert.Equal(expected, TextSender.EscapeSendKeys(input));
    }

    // --- CharToVk (simple mapping) ---

    [Theory]
    [InlineData('a', 0x41)]
    [InlineData('z', 0x5A)]
    [InlineData('A', 0x41)]
    [InlineData('Z', 0x5A)]
    [InlineData('0', 0x30)]
    [InlineData('9', 0x39)]
    [InlineData(' ', 0x20)]
    [InlineData('\r', 0x0D)]
    [InlineData('!', 0)]  // unmapped
    public void CharToVk_ReturnsExpectedVK(char c, ushort expected)
    {
        Assert.Equal(expected, TextSender.CharToVk(c));
    }

    // --- CharToVkFull (full mapping with shift state) ---

    [Theory]
    [InlineData('a', 0x41, false)]
    [InlineData('z', 0x5A, false)]
    [InlineData('A', 0x41, true)]
    [InlineData('Z', 0x5A, true)]
    [InlineData('0', 0x30, false)]
    [InlineData('9', 0x39, false)]
    [InlineData(' ', 0x20, false)]
    [InlineData('\r', 0x0D, false)]
    public void CharToVkFull_CommonChars_ReturnsExpected(char c, byte expectedVk, bool expectedShift)
    {
        var (vk, shift) = TextSender.CharToVkFull(c);
        Assert.Equal(expectedVk, vk);
        Assert.Equal(expectedShift, shift);
    }

    [Fact]
    public void CharToVkFull_UnmappableChar_ReturnsZero()
    {
        // A private-use Unicode character with no keyboard mapping
        var (vk, _) = TextSender.CharToVkFull('\uE000');
        Assert.Equal(0, vk);
    }

    // --- BuildKeyLParam / BuildCharLParam ---

    [Fact]
    public void BuildKeyLParam_KeyDown_HasRepeatOneAndScanCode()
    {
        // VK_RETURN (0x0D) → scan code via MapVirtualKey
        var lParam = TextSender.BuildKeyLParam(0x0D, keyUp: false);
        long val = (long)lParam;
        Assert.Equal(1, val & 0xFFFF); // repeat count = 1
        Assert.True((val >> 16 & 0xFF) != 0); // scan code present
        Assert.Equal(0, val >> 30 & 1); // previous state = 0
        Assert.Equal(0, val >> 31 & 1); // transition state = 0
    }

    [Fact]
    public void BuildKeyLParam_KeyUp_HasTransitionBits()
    {
        var lParam = TextSender.BuildKeyLParam(0x0D, keyUp: true);
        long val = (long)lParam;
        Assert.Equal(1, val >> 30 & 1); // previous state = 1
        Assert.Equal(1, val >> 31 & 1); // transition state = 1
    }

    [Fact]
    public void BuildCharLParam_KnownVK_HasScanCode()
    {
        var lParam = TextSender.BuildCharLParam(0x41); // VK_A
        long val = (long)lParam;
        Assert.Equal(1, val & 0xFFFF); // repeat count = 1
        Assert.True((val >> 16 & 0xFF) != 0); // scan code present
    }

    [Fact]
    public void BuildCharLParam_UnknownVK_ReturnsOne()
    {
        var lParam = TextSender.BuildCharLParam(0);
        Assert.Equal((IntPtr)1, lParam);
    }
}
