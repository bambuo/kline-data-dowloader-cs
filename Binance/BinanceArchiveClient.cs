using KlineDataDownloader.Domain;

namespace KlineDataDownloader.Binance;

/// <summary>
/// Binance 公开数据归档（https://data.binance.vision）客户端。
/// 通过 IHttpClientFactory 注册为类型化客户端（见 Program.cs）。
/// </summary>
public sealed class BinanceArchiveClient(HttpClient http)
{
    /// <summary>
    /// 下载月度 K 线 zip。返回 null 表示该月无数据（非 2xx 或空响应）；
    /// 网络异常向上抛出，由调用方按"该月失败"处理。
    /// </summary>
    public async Task<byte[]?> DownloadMonthlyKlinesZipAsync(
        string categoryPath, string symbol, string binanceCode, YearMonth month, CancellationToken ct = default)
    {
        var filename = $"{symbol}-{binanceCode}-{month.Year:D4}-{month.Month:D2}.zip";
        using var response = await http.GetAsync(
            $"/data/{categoryPath}/monthly/klines/{symbol}/{binanceCode}/{filename}",
            HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        var data = await response.Content.ReadAsByteArrayAsync(ct);
        return data.Length == 0 ? null : data;
    }
}
