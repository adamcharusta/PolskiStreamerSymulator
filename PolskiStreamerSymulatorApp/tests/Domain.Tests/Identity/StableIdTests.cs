using PolskiStreamerSymulatorApp.Domain.Identity;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Identity;

public sealed class StableIdTests
{
    [Theory]
    [InlineData("regular_stream")]
    [InlineData("clip_backlash")]
    [InlineData("permanent_ban")]
    [InlineData("a")]
    [InlineData("event_2026")]
    public void AcceptsLowercaseSnakeCase(string value)
    {
        Assert.True(StableId.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Regular_stream")]
    [InlineData("1st_event")]
    [InlineData("_hidden")]
    [InlineData("regular-stream")]
    [InlineData("regular stream")]
    [InlineData("zażółć")]
    [InlineData("regular_stream\n")]
    public void RejectsOtherText(string value)
    {
        Assert.False(StableId.IsValid(value));
    }

    [Fact]
    public void RejectsNull()
    {
        Assert.False(StableId.IsValid(null));
    }

    [Fact]
    public void AcceptsSixtyFourCharactersAndRejectsSixtyFive()
    {
        Assert.True(StableId.IsValid("a" + new string('b', 63)));
        Assert.False(StableId.IsValid("a" + new string('b', 64)));
    }
}
