namespace KlineDataDownloader.Configuration;

/// <summary>appsettings.json 中 "KlineData" 节的强类型配置（Options 模式）。</summary>
public sealed class KlineDataOptions
{
    public const string SectionName = "KlineData";

    /// <summary>K 线 CSV 输出根目录（相对进程工作目录）。</summary>
    public string BaseDir { get; set; } = "./kline-data";
}
