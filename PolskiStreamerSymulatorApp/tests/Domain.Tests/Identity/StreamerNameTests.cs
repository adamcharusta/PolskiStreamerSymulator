using System.Text;
using PolskiStreamerSymulatorApp.Domain.Identity;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Identity;

public sealed class StreamerNameTests
{
    [Theory]
    [InlineData("NeonBorsuk")]
    [InlineData("Cichy Kret")]
    [InlineData("Zażółć_gęślą-1")]
    [InlineData("Ab")]
    [InlineData("42")]
    public void AcceptsValidNamesUnchanged(string name)
    {
        bool valid = StreamerName.TryNormalize(name, out string normalized);

        Assert.True(valid);
        Assert.Equal(name, normalized);
        Assert.True(StreamerName.IsNormalizedValid(name));
    }

    [Fact]
    public void TrimsOuterWhitespace()
    {
        bool valid = StreamerName.TryNormalize("  NeonBorsuk \t", out string normalized);

        Assert.True(valid);
        Assert.Equal("NeonBorsuk", normalized);
        Assert.False(StreamerName.IsNormalizedValid("  NeonBorsuk \t"));
    }

    [Fact]
    public void ConvertsDecomposedTextToComposedForm()
    {
        string composed = "Zażółć".Normalize(NormalizationForm.FormC);
        string decomposed = composed.Normalize(NormalizationForm.FormD);

        bool valid = StreamerName.TryNormalize(decomposed, out string normalized);

        Assert.True(valid);
        Assert.NotEqual(decomposed, composed);
        Assert.Equal(composed, normalized);
        Assert.False(StreamerName.IsNormalizedValid(decomposed));
        Assert.True(StreamerName.IsNormalizedValid(composed));
    }

    [Fact]
    public void AcceptsThirtyTwoDisplayedCharactersAndRejectsThirtyThree()
    {
        Assert.True(StreamerName.TryNormalize(new string('a', 32), out _));
        Assert.False(StreamerName.TryNormalize(new string('a', 33), out _));
    }

    [Fact]
    public void CountsALetterWithACombiningMarkAsOneDisplayedCharacter()
    {
        // q with a combining acute accent has no composed form, so each pair stays two UTF-16 code units.
        string marked = string.Concat(Enumerable.Repeat("q\u0301", 32));

        Assert.True(StreamerName.TryNormalize(marked, out string normalized));
        Assert.Equal(64, normalized.Length);
    }

    [Fact]
    public void RejectsNamesLongerThanSixtyFourUtf16CodeUnits()
    {
        string overlong = "ab" + new string('\u0301', 63);

        Assert.False(StreamerName.TryNormalize(overlong, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("Neon  Borsuk")]
    [InlineData("Neon\tBorsuk")]
    [InlineData("Neon\u0000Borsuk")]
    [InlineData("Neon\U0001F600")]
    [InlineData("Neon!")]
    [InlineData("--")]
    [InlineData("_ -")]
    [InlineData("\u0301ab")]
    public void RejectsInvalidNames(string name)
    {
        bool valid = StreamerName.TryNormalize(name, out string normalized);

        Assert.False(valid);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void RejectsTextThatIsNotValidUtf16()
    {
        string[] names =
        [
            "ab" + (char)0xD800,
            (char)0xDC00 + "ab",
            "a" + (char)0xD800 + "b",
            "ab" + (char)0xFFFE + "cd",
        ];

        Assert.All(names, static name =>
        {
            Assert.False(StreamerName.TryNormalize(name, out string normalized));
            Assert.Equal(string.Empty, normalized);
            Assert.False(StreamerName.IsNormalizedValid(name));
        });
    }

    [Fact]
    public void RejectsNull()
    {
        Assert.False(StreamerName.TryNormalize(null, out _));
        Assert.False(StreamerName.IsNormalizedValid(null));
    }
}
