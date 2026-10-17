using RetailInsights.Core.Kpis;

namespace RetailInsights.UnitTests;

public sealed class DateRangeTests
{
    [Fact]
    public void Range_is_inclusive()
    {
        var range = DateRange.Create(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        Assert.Equal(31, range.Days);
        Assert.True(range.Contains(new DateOnly(2026, 3, 31)));
        Assert.False(range.Contains(new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public void Previous_period_has_the_same_length_and_ends_the_day_before()
    {
        var range = DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 8));

        var previous = range.Previous();

        Assert.Equal(new DateOnly(2026, 2, 23), previous.From);
        Assert.Equal(new DateOnly(2026, 3, 1), previous.To);
        Assert.Equal(range.Days, previous.Days);
    }

    [Fact]
    public void From_must_not_be_after_to() =>
        Assert.Throws<InvalidKpiQueryException>(() => DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 1)));

    [Fact]
    public void Period_is_limited_to_one_year()
    {
        Assert.Throws<InvalidKpiQueryException>(() => DateRange.Create(new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 2)));
        Assert.Equal(DateRange.MaxDays, DateRange.Create(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)).Days);
    }
}
