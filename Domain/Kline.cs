namespace KlineDataDownloader.Domain;

/// <summary>单根 K 线（OpenTime 为 UTC，价格与成交量用 decimal 保持精度）。</summary>
public readonly record struct Kline(
    DateTimeOffset OpenTime,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);
