namespace KlineDataDownloader.Contracts;

/// <summary>
/// 下载结果报告。注意：FilePath 为 null 时仍输出 "filePath": null，
/// 与 Java 版 DownloadResult（无 @JsonInclude 注解，Jackson 默认行为）一致。
/// </summary>
public sealed record DownloadResult(
    int DownloadedFiles,
    int TotalKlines,
    string? FilePath,
    List<string> Errors);
