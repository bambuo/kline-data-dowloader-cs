namespace KlineDataDownloader.Contracts;

/// <summary>下载请求参数（前端 JSON body；字段可缺省，缺失时为 null）。</summary>
public sealed record DownloadRequest(
    string? Category,
    string? BaseAsset,
    string? QuoteAsset,
    string? Timeframe,
    string? StartMonth,
    string? EndMonth);
