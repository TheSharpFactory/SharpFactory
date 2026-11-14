#region Usings
using SF.Utils;
using System;
using System.Text;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// IOUtils.ToCrlf, which every generated file goes through before it is written: every line break becomes CRLF, text
/// that is CRLF already comes back as the same string (no copy), and the result is the same as the old conversion
/// (three Replace calls) for any text.
/// </summary>
public sealed class CrlfTests
{
    [Fact]
    public void TextThatIsCrlfAlreadyComesBackAsTheSameString()
    {
        var text = "namespace A;\r\npublic class B\r\n{\r\n}\r\n";
        Assert.Same(text, IOUtils.ToCrlf(text));

        var noBreaks = "public class B { }";
        Assert.Same(noBreaks, IOUtils.ToCrlf(noBreaks));

        var onlyCrlf = "\r\n\r\n";
        Assert.Same(onlyCrlf, IOUtils.ToCrlf(onlyCrlf));
    }

    [Theory]
    [InlineData("a\nb", "a\r\nb")]
    [InlineData("a\rb", "a\r\nb")]
    [InlineData("a\r\nb", "a\r\nb")]
    [InlineData("a\nb\r\nc\rd", "a\r\nb\r\nc\r\nd")]
    [InlineData("\n", "\r\n")]
    [InlineData("\r", "\r\n")]
    [InlineData("\nstart", "\r\nstart")]
    [InlineData("end\n", "end\r\n")]
    [InlineData("end\r", "end\r\n")]
    [InlineData("\r\r\n", "\r\n\r\n")]
    [InlineData("\n\r", "\r\n\r\n")]
    [InlineData("\n\n\n", "\r\n\r\n\r\n")]
    [InlineData("\r\r", "\r\n\r\n")]
    [InlineData("a\r\n\nb", "a\r\n\r\nb")]
    public void EveryLineBreakBecomesCrlf(string text, string expected)
    {
        Assert.Equal(expected, IOUtils.ToCrlf(text));
    }

    [Fact]
    public void EmptyAndNullStayAsTheyAre()
    {
        Assert.Equal("", IOUtils.ToCrlf(""));
        Assert.Null(IOUtils.ToCrlf(null));
    }

    [Fact]
    public void AnyTextConvertsLikeTheThreeReplaceCalls()
    {
        //made-up text of letters and line breaks in every order, from a fixed seed so a failure repeats
        var random = new Random(20261114);
        var pieces = new[] { "a", "bc", "\r", "\n", "\r\n", "\n\r", " " };
        for(var i = 0; i < 5000; i++)
        {
            var text = new StringBuilder();
            var count = random.Next(0, 40);
            for(var j = 0; j < count; j++)
                text.Append(pieces[random.Next(pieces.Length)]);

            var value = text.ToString();
            Assert.Equal(ConvertTheOldWay(value), IOUtils.ToCrlf(value));
        }
    }

    [Fact]
    public void ALargeFileConvertsLikeTheThreeReplaceCalls()
    {
        var text = new StringBuilder();
        for(var i = 0; i < 20000; i++)
            text.Append("    public int Column").Append(i).Append(" { get; set; }").Append(i % 3 == 0 ? "\n" : "\r\n");

        var value = text.ToString();
        var converted = IOUtils.ToCrlf(value);
        Assert.Equal(ConvertTheOldWay(value), converted);
        Assert.Same(converted, IOUtils.ToCrlf(converted));
    }

    private static string ConvertTheOldWay(string content)
    {
        if(string.IsNullOrEmpty(content))
            return content;

        return content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
    }
}
