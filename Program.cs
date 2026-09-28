using KlineDataDownloader.Binance;
using KlineDataDownloader.Configuration;
using KlineDataDownloader.Contracts;
using KlineDataDownloader.Domain;
using KlineDataDownloader.Services;
using KlineDataDownloader.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KlineDataOptions>(
    builder.Configuration.GetSection(KlineDataOptions.SectionName));
builder.Services.AddHttpClient<BinanceArchiveClient>(http =>
{
    http.BaseAddress = new Uri("https://data.binance.vision");
    // 与 Java 版 RestClient 一致：不限制请求超时
    http.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddSingleton<KlineDownloader>();

var app = builder.Build();

// 前端 SPA（wwwroot，源自 ui/dist）
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api/v1/data");

// 拉取 Binance 历史 K 线
api.MapPost("/pull", async (DownloadRequest req, KlineDownloader downloader, CancellationToken ct) =>
{
    Result<DownloadResult> result;
    try
    {
        result = Result<DownloadResult>.Success(await downloader.PullAsync(req, ct));
    }
    catch (Exception e)
    {
        result = Result<DownloadResult>.Error(5001, e.Message);
    }
    return result;
});

// 查询已下载的历史 K 线
api.MapGet("/klines", (string category, string baseAsset, string quoteAsset, string timeframe,
    string? startMonth, string? endMonth, KlineDownloader downloader) =>
{
    Result<List<KlinePoint>> result;
    try
    {
        var pair = new Pair(baseAsset, quoteAsset);
        var cat = Enum.Parse<Category>(category, ignoreCase: true);
        var tf = TimeFrameExtensions.FromBinanceCode(timeframe);

        var store = new KlineStore(downloader.KlineBaseDir);
        var all = store.ReadHistory(pair, cat, tf);

        IEnumerable<Kline> query = all;
        if (startMonth is not null && endMonth is not null)
        {
            var startYm = YearMonth.Parse(startMonth);
            var endYm = YearMonth.Parse(endMonth);
            query = query.Where(k =>
            {
                var ym = YearMonth.FromUtc(k.OpenTime);
                return !ym.IsBefore(startYm) && !ym.IsAfter(endYm);
            });
        }

        var points = query
            .OrderBy(k => k.OpenTime)
            .Select(k => new KlinePoint(
                Time: k.OpenTime.ToUnixTimeMilliseconds() / 1000,
                Open: k.Open,
                High: k.High,
                Low: k.Low,
                Close: k.Close,
                Volume: k.Volume))
            .ToList();

        result = Result<List<KlinePoint>>.Success(points);
    }
    catch (Exception e)
    {
        result = Result<List<KlinePoint>>.Error(5001, e.Message);
    }
    return result;
});

app.Run();
