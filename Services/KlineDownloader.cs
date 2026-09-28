using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using KlineDataDownloader.Binance;
using KlineDataDownloader.Configuration;
using KlineDataDownloader.Contracts;
using KlineDataDownloader.Domain;
using Microsoft.Extensions.Options;

namespace KlineDataDownloader.Services;

/// <summary>
/// 从 Binance 数据归档 (https://data.binance.vision/) 拉取历史 K 线：
/// 下载月度 zip → 解析 → 合并去重 → 按天写入 kline-data 目录。
/// 月份间异步并行下载（最大并发 4）。
/// </summary>
public sealed class KlineDownloader(
    BinanceArchiveClient client,
    IOptions<KlineDataOptions> options,
    ILogger<KlineDownloader> logger)
{
    // 时间戳归一化阈值与有效窗口（毫秒）
    private const long TimestampSplit = 100_000_000_000_000L;
    private const long MinTimestampMillis = 946_684_800_000L;    // 2000-01-01
    private const long MaxTimestampMillis = 4_102_444_800_000L;  // 2100-01-01
    private const int MaxParallelDownloads = 4;

    public string KlineBaseDir => options.Value.BaseDir;

    /// <summary>执行拉取，返回结果报告。</summary>
    public async Task<DownloadResult> PullAsync(DownloadRequest req, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var downloaded = 0;
        var totalKlines = 0;

        var pair = new Pair(req.BaseAsset!, req.QuoteAsset!);

        TimeFrame tf;
        try
        {
            tf = TimeFrameExtensions.FromBinanceCode(req.Timeframe);
        }
        catch (Exception)
        {
            return new DownloadResult(0, 0, null, [$"Invalid timeframe: {req.Timeframe ?? "null"}"]);
        }

        // 解析月份范围
        var start = YearMonth.Parse(req.StartMonth!);
        var end = YearMonth.Parse(req.EndMonth!);
        if (start.IsAfter(end))
        {
            return new DownloadResult(0, 0, null, ["startMonth must be <= endMonth"]);
        }

        var targetDir = Path.Combine(options.Value.BaseDir, req.Category!, pair.ToSymbol());
        try
        {
            Directory.CreateDirectory(targetDir);
        }
        catch (Exception e)
        {
            return new DownloadResult(0, 0, null, [$"Cannot create directory: {e.Message}"]);
        }

        var months = new List<YearMonth>();
        for (var ym = start; !ym.IsAfter(end); ym = ym.PlusMonths(1))
        {
            months.Add(ym);
        }

        // 并行下载所有月份；结果与错误按月份下标归集，保证合并与报错顺序跟月份顺序一致
        var monthData = new ConcurrentDictionary<int, List<Kline>>();
        var monthErrors = new ConcurrentDictionary<int, string>();
        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, months.Count),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Math.Min(months.Count, MaxParallelDownloads)),
                    CancellationToken = ct,
                },
                async (i, token) =>
                {
                    try
                    {
                        monthData[i] = await DownloadMonthAsync(req.Category!, pair, tf, months[i], token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        monthErrors[i] = e.Message;
                    }
                });
        }
        catch (OperationCanceledException e)
        {
            errors.Add($"Download interrupted: {e.Message}");
            return new DownloadResult(downloaded, totalKlines, null, errors);
        }

        var allKlines = new List<Kline>();
        for (var i = 0; i < months.Count; i++)
        {
            if (monthErrors.TryGetValue(i, out var msg))
            {
                var text = $"Month download failed: {msg}";
                logger.LogWarning("{Message}", text);
                errors.Add(text);
                continue;
            }
            var data = monthData.GetValueOrDefault(i, []);
            if (data.Count == 0)
            {
                continue;
            }
            allKlines.AddRange(data);
            downloaded++;
            totalKlines += data.Count;
            logger.LogInformation("Downloaded {Day} ({Count} klines)",
                data[0].OpenTime.UtcDateTime.ToString("yyyy-MM-dd"), data.Count);
        }

        if (allKlines.Count == 0)
        {
            return new DownloadResult(0, 0, null, errors.Count == 0 ? ["No data downloaded"] : errors);
        }

        // 按时间稳定排序（须用 OrderBy，List.Sort 不稳定）后相邻去重，重复时间戳保留首条
        var deduped = new List<Kline>(allKlines.Count);
        foreach (var k in allKlines.OrderBy(k => k.OpenTime))
        {
            if (deduped.Count == 0 || deduped[^1].OpenTime != k.OpenTime)
            {
                deduped.Add(k);
            }
        }

        // 按天分组（按日期键序）并覆写日文件
        var tfDir = Path.Combine(targetDir, tf.ToBinanceCode());
        try
        {
            Directory.CreateDirectory(tfDir);
        }
        catch (Exception e)
        {
            errors.Add($"Cannot create directory: {e.Message}");
            return new DownloadResult(0, 0, null, errors);
        }

        var daily = new SortedDictionary<string, List<Kline>>(StringComparer.Ordinal);
        foreach (var k in deduped)
        {
            var day = k.OpenTime.UtcDateTime.ToString("yyyy-MM-dd");
            if (!daily.TryGetValue(day, out var dayKlines))
            {
                dayKlines = [];
                daily[day] = dayKlines;
            }
            dayKlines.Add(k);
        }

        long writtenKlines = 0;
        var newLine = Environment.NewLine;
        foreach (var (day, dayKlines) in daily)
        {
            if (dayKlines.Count == 0)
            {
                continue;
            }
            var dayFile = Path.Combine(tfDir, day + ".csv");
            try
            {
                using var writer = new StreamWriter(dayFile, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                foreach (var k in dayKlines)
                {
                    writer.Write(string.Create(CultureInfo.InvariantCulture,
                        $"{k.OpenTime.ToUnixTimeMilliseconds()},{k.Open},{k.High},{k.Low},{k.Close},{k.Volume}{newLine}"));
                }
                writtenKlines += dayKlines.Count;
            }
            catch (Exception e)
            {
                errors.Add($"Write failed for {day}: {e.Message}");
            }
        }

        logger.LogInformation("Download complete: {Files} files, {Klines} klines -> {Dir}/",
            downloaded, deduped.Count, tfDir);
        return new DownloadResult(downloaded, (int)writtenKlines, tfDir, errors);
    }

    /// <summary>下载单个月份的 zip 并解析。该月无数据（404/403/空响应）返回空列表。</summary>
    private async Task<List<Kline>> DownloadMonthAsync(
        string category, Pair pair, TimeFrame tf, YearMonth ym, CancellationToken ct)
    {
        var symbol = pair.ToSymbol();
        var bc = tf.ToBinanceCode();

        var categoryPath = category switch
        {
            "spot" => "spot",
            "future" => "futures/um",
            _ => throw new ArgumentException($"Unknown category: {category}"),
        };

        var zipData = await client.DownloadMonthlyKlinesZipAsync(categoryPath, symbol, bc, ym, ct);
        if (zipData is null)
        {
            return [];
        }

        var result = new List<Kline>();
        using var stream = new MemoryStream(zipData);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.Entries.FirstOrDefault();
        if (entry is null)
        {
            return result;
        }

        using var reader = new StreamReader(entry.Open());
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }
            var parts = line.Split(',');
            if (parts.Length < 6)
            {
                continue;
            }
            try
            {
                var tsMs = long.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
                while (tsMs > TimestampSplit)
                {
                    tsMs /= 1000;
                }
                if (tsMs < MinTimestampMillis || tsMs > MaxTimestampMillis)
                {
                    continue;
                }
                result.Add(new Kline(
                    OpenTime: DateTimeOffset.FromUnixTimeMilliseconds(tsMs),
                    Open: decimal.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                    High: decimal.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                    Low: decimal.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                    Close: decimal.Parse(parts[4].Trim(), CultureInfo.InvariantCulture),
                    Volume: decimal.Parse(parts[5].Trim(), CultureInfo.InvariantCulture)));
            }
            catch (Exception e) when (e is FormatException or OverflowException)
            {
                // 坏行静默丢弃
            }
        }
        return result;
    }
}
