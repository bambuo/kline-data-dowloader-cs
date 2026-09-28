using System.Globalization;

namespace KlineDataDownloader.Domain;

/// <summary>年月值对象（仅年 + 月，不含日与时区信息）。</summary>
public readonly record struct YearMonth(int Year, int Month) : IComparable<YearMonth>
{
    /// <summary>按 "yyyy-MM" 严格解析，失败抛 FormatException。</summary>
    public static YearMonth Parse(string text)
    {
        var date = DateTime.ParseExact(text, "yyyy-MM", CultureInfo.InvariantCulture);
        return new YearMonth(date.Year, date.Month);
    }

    /// <summary>取 UTC 时间的年月部分。</summary>
    public static YearMonth FromUtc(DateTimeOffset time) => new(time.UtcDateTime.Year, time.UtcDateTime.Month);

    public YearMonth PlusMonths(int months)
    {
        var index = Year * 12 + (Month - 1) + months;
        var remainder = index % 12;
        return new YearMonth((index - remainder) / 12, remainder + 1);
    }

    public bool IsAfter(YearMonth other) => CompareTo(other) > 0;

    public bool IsBefore(YearMonth other) => CompareTo(other) < 0;

    public int CompareTo(YearMonth other) =>
        Year != other.Year ? Year.CompareTo(other.Year) : Month.CompareTo(other.Month);

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
