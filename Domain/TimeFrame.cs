namespace KlineDataDownloader.Domain;

/// <summary>K 线周期，映射到币安命名规范。</summary>
public enum TimeFrame
{
    Min1,
    Min5,
    Min15,
    Min30,
    Hour1,
    Hour4,
    Day1,
}

/// <summary>TimeFrame 与分钟数 / 币安周期代码之间的转换。</summary>
public static class TimeFrameExtensions
{
    /// <summary>周期对应分钟数。</summary>
    public static int ToMinutes(this TimeFrame tf) => tf switch
    {
        TimeFrame.Min1 => 1,
        TimeFrame.Min5 => 5,
        TimeFrame.Min15 => 15,
        TimeFrame.Min30 => 30,
        TimeFrame.Hour1 => 60,
        TimeFrame.Hour4 => 240,
        TimeFrame.Day1 => 1440,
        _ => throw new ArgumentOutOfRangeException(nameof(tf), tf, null),
    };

    /// <summary>币安风格周期代码，如 "1m"、"30m"、"1h"、"4h"、"1d"。</summary>
    public static string ToBinanceCode(this TimeFrame tf) => tf switch
    {
        TimeFrame.Min1 => "1m",
        TimeFrame.Min5 => "5m",
        TimeFrame.Min15 => "15m",
        TimeFrame.Min30 => "30m",
        TimeFrame.Hour1 => "1h",
        TimeFrame.Hour4 => "4h",
        TimeFrame.Day1 => "1d",
        _ => throw new ArgumentOutOfRangeException(nameof(tf), tf, null),
    };

    /// <summary>从币安风格代码反解析，如 "30m" → Min30。未知代码抛 ArgumentException。</summary>
    public static TimeFrame FromBinanceCode(string? code) => code switch
    {
        "1m" => TimeFrame.Min1,
        "5m" => TimeFrame.Min5,
        "15m" => TimeFrame.Min15,
        "30m" => TimeFrame.Min30,
        "1h" => TimeFrame.Hour1,
        "4h" => TimeFrame.Hour4,
        "1d" => TimeFrame.Day1,
        _ => throw new ArgumentException($"Unknown binance timeframe code: {code ?? "null"}"),
    };
}
