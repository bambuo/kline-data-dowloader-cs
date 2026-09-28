namespace KlineDataDownloader.Contracts;

/// <summary>
/// 下载结果报告。FilePath 为 null 时仍输出 "filePath": null，
/// 与 Result&lt;T&gt; 的 null 省略策略不同（此处刻意保留 null 字段）。
/// </summary>
public sealed record DownloadResult(
    int DownloadedFiles,
    int TotalKlines,
    string? FilePath,
    List<string> Errors);
