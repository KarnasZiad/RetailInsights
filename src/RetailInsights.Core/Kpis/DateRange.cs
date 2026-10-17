namespace RetailInsights.Core.Kpis;

/// <summary>Inclusive range of business days.</summary>
public readonly record struct DateRange
{
    public const int MaxDays = 366;

    private DateRange(DateOnly from, DateOnly to)
    {
        From = from;
        To = to;
    }

    public DateOnly From { get; }

    public DateOnly To { get; }

    public int Days => To.DayNumber - From.DayNumber + 1;

    public static DateRange Create(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new InvalidKpiQueryException($"'from' ({from:yyyy-MM-dd}) must be before 'to' ({to:yyyy-MM-dd}).");
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxDays)
        {
            throw new InvalidKpiQueryException($"The period cannot exceed {MaxDays} days.");
        }

        return new DateRange(from, to);
    }

    /// <summary>The period of the same length right before this one (comparison basis for growth).</summary>
    public DateRange Previous() => new(From.AddDays(-Days), From.AddDays(-1));

    public bool Contains(DateOnly day) => day >= From && day <= To;

    public override string ToString() => $"{From:yyyy-MM-dd}..{To:yyyy-MM-dd}";
}

public sealed class InvalidKpiQueryException(string message) : Exception(message);
