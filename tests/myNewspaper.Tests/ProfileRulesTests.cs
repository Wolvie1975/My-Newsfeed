using myNewspaper.Web.Profiles;

namespace myNewspaper.Tests;

public class ProfileRulesTests
{
    [Theory]
    [InlineData("0000")]
    [InlineData("4821")]
    public void Four_digit_pins_are_valid(string pin) => Assert.True(ProfileRules.IsValidPin(pin));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12a4")]
    [InlineData(" 1234")]
    [InlineData("1234\n")]
    [InlineData("١٢٣٤")]   // Arabic-Indic digits
    public void Other_pins_are_rejected(string? pin) => Assert.False(ProfileRules.IsValidPin(pin));

    [Fact]
    public void Names_are_trimmed_and_normalized()
    {
        Assert.Equal("Alex", ProfileRules.NormalizeName("  Alex "));
        Assert.Equal("Zoë", ProfileRules.NormalizeName("Zoë"));
        Assert.Equal(new string('a', 40), ProfileRules.NormalizeName(new string('a', 40)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("Al\u0000ex")]
    [InlineData("Al\tex")]
    public void Empty_names_and_control_characters_are_rejected(string? name) =>
        Assert.Throws<ArgumentException>(() => ProfileRules.NormalizeName(name));

    [Fact]
    public void Names_longer_than_40_characters_are_rejected() =>
        Assert.Throws<ArgumentException>(() => ProfileRules.NormalizeName(new string('a', 41)));

    [Fact]
    public void Colors_are_kept_in_upper_case() => Assert.Equal("#0051BA", ProfileRules.NormalizeColor("#0051ba"));

    [Theory]
    [InlineData(null)]
    [InlineData("0051BA")]
    [InlineData("#051BA")]
    [InlineData("#0051BAA")]
    [InlineData("#00G1BA")]
    [InlineData("red")]
    [InlineData("#0051BA;background:url(x)")]
    [InlineData("#0051BA\n")]
    public void Anything_but_a_hex_color_is_rejected(string? color) =>
        Assert.Throws<ArgumentException>(() => ProfileRules.NormalizeColor(color));

    [Fact]
    public void The_fifth_wrong_pin_locks_and_restarts_the_count()
    {
        var count = 0;
        for (var i = 1; i < ProfileRules.MaxWrongPins; i++)
        {
            (count, var locked) = ProfileRules.AfterWrongPin(count);
            Assert.False(locked);
            Assert.Equal(i, count);
            Assert.Equal(ProfileRules.MaxWrongPins - i, ProfileRules.TriesLeft(count));
        }
        Assert.Equal((0, true), ProfileRules.AfterWrongPin(count));
    }
}
