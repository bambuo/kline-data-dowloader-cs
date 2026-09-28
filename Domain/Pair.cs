namespace KlineDataDownloader.Domain;

/// <summary>交易对值对象，不可变。构造时统一转为大写（与 Java 版一致）。</summary>
public readonly record struct Pair
{
    public Pair(string baseAsset, string quoteAsset)
    {
        BaseAsset = baseAsset.ToUpperInvariant();
        QuoteAsset = quoteAsset.ToUpperInvariant();
    }

    public string BaseAsset { get; }
    public string QuoteAsset { get; }

    /// <summary>无分隔符格式（BTCUSDT），主流交易平台默认格式。</summary>
    public string ToSymbol() => BaseAsset + QuoteAsset;

    /// <summary>带分隔符格式，如 ToSymbol("-") → "BTC-USDT"。</summary>
    public string ToSymbol(string separator) => BaseAsset + separator + QuoteAsset;

    /// <summary>从拼接符号反解析，如 "BTCUSDT" → Pair("BTC","USDT")。</summary>
    public static Pair FromSymbol(string symbol)
    {
        string[] knownQuotes = ["USDT", "USDC", "BUSD", "USD", "BNB", "ETH", "BTC", "TRX", "SOL", "DAI", "FDUSD"];
        foreach (var q in knownQuotes)
        {
            if (symbol.EndsWith(q, StringComparison.Ordinal) && symbol.Length > q.Length)
            {
                return new Pair(symbol[..(symbol.Length - q.Length)], q);
            }
        }
        var split = symbol.Length - 4;
        return split > 0 ? new Pair(symbol[..split], symbol[split..]) : new Pair(symbol, "");
    }

    public override string ToString() => $"{BaseAsset}/{QuoteAsset}";
}
