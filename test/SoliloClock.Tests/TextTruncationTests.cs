using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class TextTruncationTests
{
    [Fact]
    public void ShortText_IsUnchanged()
    {
        Assert.Equal("SoliloClock", TextTruncation.Truncate("SoliloClock", 63));
    }

    [Fact]
    public void LongText_IsCutWithAnEllipsis()
    {
        var result = TextTruncation.Truncate("abcdefghij", 5);

        Assert.Equal("abcd…", result);
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void EmojiSurrogatePairs_AreNeverSplit()
    {
        // Four clock emoji, each two UTF-16 code units.
        var result = TextTruncation.Truncate("🕐🕑🕒🕓", 3);

        Assert.Equal("🕐🕑…", result);
        Assert.DoesNotContain('\uFFFD', result);
    }

    [Fact]
    public void CombiningMarks_StayWithTheirBaseLetter()
    {
        var text = "e\u0301e\u0301e\u0301";

        var result = TextTruncation.Truncate(text, 2);

        Assert.Equal("e\u0301…", result);
    }

    [Fact]
    public void TinyLimit_StillReturnsAnEllipsis()
    {
        Assert.Equal("…", TextTruncation.Truncate("abcdef", 1));
    }

    [Fact]
    public void NullText_IsTreatedAsEmpty()
    {
        Assert.Equal(string.Empty, TextTruncation.Truncate(null, 10));
    }
}
