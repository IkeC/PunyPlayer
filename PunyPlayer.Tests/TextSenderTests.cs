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
}
