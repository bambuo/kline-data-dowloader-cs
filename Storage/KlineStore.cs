using System.Globalization;
using KlineDataDownloader.Domain;

namespace KlineDataDownloader.Storage;

/// <summary>
/// 从本地目录读取历史 K 线 CSV（按天拆分）：
///   {baseDir}/{category}/{SYMBOL}/{timeframe}/{yyyy-MM-dd}.csv
/// 行格式（无表头）：timestamp_ms,open,high,low,close,volume
/// </summary>
public sealed class KlineStore(string baseDir)
{
    /// <summary>读取指定上下文的历史 K 线，按文件名（即日期）顺序合并。</summary>
    public List<Kline> ReadHistory(Pair pair, Category category, TimeFrame tf)
    {
        var tfDir = Path.Combine(
            baseDir,
            category.ToString().ToLowerInvariant(),
            pair.ToSymbol(),
            tf.ToBinanceCode());
        if (!Directory.Exists(tfDir))
        {
            return [];
        }

        var result = new List<Kline>();
        foreach (var file in Directory.EnumerateFiles(tfDir, "*.csv").OrderBy(f => f, StringComparer.Ordinal))
        {
            result.AddRange(ReadCsv(file));
        }
        return result;
    }

    private static List<Kline> ReadCsv(string file)
    {
        var result = new List<Kline>();
        foreach (var raw in File.ReadLines(file))
        {
            var line = raw.Trim();
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
                result.Add(new Kline(
                    OpenTime: DateTimeOffset.FromUnixTimeMilliseconds(
                        long.Parse(parts[0].Trim(), CultureInfo.InvariantCulture)),
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
