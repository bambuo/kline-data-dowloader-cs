namespace KlineDataDownloader.Contracts;

/// <summary>图表数据点。Time 为 epoch 秒（CSV 中存的是毫秒）。</summary>
public sealed record KlinePoint(
    long Time,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);
