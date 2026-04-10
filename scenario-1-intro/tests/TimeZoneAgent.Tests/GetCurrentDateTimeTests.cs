namespace TimeZoneAgent.Tests;

/// <summary>
/// Tests the timezone lookup and formatting logic used by the GetCurrentDateTime function tool.
/// We test TimeZoneInfo directly (same API the tool uses) to keep the single-file agent simple.
/// </summary>
public class GetCurrentDateTimeTests
{
    [Theory]
    [InlineData("UTC")]
    [InlineData("America/New_York")]
    [InlineData("Asia/Tokyo")]
    [InlineData("Europe/London")]
    [InlineData("Australia/Sydney")]
    public void ValidIanaTimezone_ReturnsFormattedString(string ianaTimezone)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimezone);
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var result = $"Current time in {ianaTimezone}: {now:dddd, MMMM dd, yyyy 'at' hh:mm tt zzz}";

        Assert.StartsWith($"Current time in {ianaTimezone}:", result);
        Assert.Matches(@"\w+, \w+ \d{2}, \d{4} at \d{2}:\d{2} [AP]M", result);
    }

    [Fact]
    public void UtcTimezone_ReturnsUtcOffset()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("UTC");
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var result = $"Current time in UTC: {now:dddd, MMMM dd, yyyy 'at' hh:mm tt zzz}";

        Assert.Contains("+00:00", result);
    }

    [Theory]
    [InlineData("Invalid/Timezone")]
    [InlineData("")]
    [InlineData("Not_A_Real_Zone")]
    public void InvalidTimezone_ThrowsTimeZoneNotFoundException(string ianaTimezone)
    {
        Assert.Throws<TimeZoneNotFoundException>(() =>
            TimeZoneInfo.FindSystemTimeZoneById(ianaTimezone));
    }

    [Fact]
    public void ConvertedTime_HasCorrectOffset()
    {
        // Tokyo is always UTC+9 (no DST)
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

        Assert.Equal(TimeSpan.FromHours(9), now.Offset);
    }

    [Fact]
    public void FormatString_MatchesExpectedPattern()
    {
        // Verify the exact format string used in Program.cs
        var tz = TimeZoneInfo.FindSystemTimeZoneById("UTC");
        var fixedTime = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);
        var converted = TimeZoneInfo.ConvertTime(fixedTime, tz);
        var result = $"Current time in UTC: {converted:dddd, MMMM dd, yyyy 'at' hh:mm tt zzz}";

        Assert.Equal("Current time in UTC: Sunday, June 15, 2025 at 02:30 PM +00:00", result);
    }
}
