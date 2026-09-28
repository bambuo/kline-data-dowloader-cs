# kline-data-downloader (C# 版)

从 Binance 公开数据归档（[data.binance.vision](https://data.binance.vision/)）拉取历史 K 线：
下载月度 zip → 解析 → 合并去重 → 按 UTC 天写入 CSV。内置 Vue 3 前端。

本目录是 Java/Spring Boot 版（`../规则引擎/kline-data-downloader`）的 C# 复刻，**功能与数据输出逐字节一致**，
但架构采用 .NET 惯用写法而非照搬 Spring。

## 运行

```bash
dotnet run            # 监听 http://0.0.0.0:8080
```

打开 http://localhost:8080 即为前端页面。数据写入 `./kline-data/`（相对进程工作目录）。

首次 clone 后如需页面：`cd ui && bun install && bun run build`，再 `dotnet run`（构建时自动同步 `ui/dist` → `wwwroot/`）；不做前端构建时 API 仍可直接使用。

## 配置（appsettings.json）

| 键 | 默认值 | 说明 |
|---|---|---|
| `Urls` | `http://0.0.0.0:8080` | 监听地址 |
| `KlineData:BaseDir` | `./kline-data` | CSV 输出根目录 |

## API

- `POST /api/v1/data/pull` — 拉取历史 K 线
  ```json
  {"category":"spot","baseAsset":"BTC","quoteAsset":"USDT","timeframe":"30m","startMonth":"2024-01","endMonth":"2024-06"}
  ```
  响应统一封装 `{"code":0,"message":"success","data":{...}}`，`code=0` 成功；异常返回 `{"code":5001,"message":"..."}`（HTTP 始终 200，与 Java 版一致）。
- `GET /api/v1/data/klines?category=&baseAsset=&quoteAsset=&timeframe=[&startMonth=&endMonth=]` — 读取本地 CSV，`time` 为 epoch 秒。

## 数据格式

`{BaseDir}/{category}/{SYMBOL}/{timeframe}/{yyyy-MM-dd}.csv`，如 `kline-data/spot/BTCUSDT/30m/2024-01-01.csv`，无表头：

```
timestamp_ms,open,high,low,close,volume
1704067200000,42283.58000000,42554.57000000,42261.02000000,42419.73000000,823.95971000
```

每次拉取整体覆写对应日期的文件；`category` 取 `spot` / `future`（future 对应币安 `futures/um` 归档）。

## 前端开发

前端源码在 `ui/`（Vue 3 + Arco Design，bun + vite），构建产物不入库（`ui/dist` 与 `wwwroot/` 均被忽略，`wwwroot/` 由 dotnet 构建时自动从 `ui/dist` 同步）：

```bash
cd ui && bun install
bun run dev        # 5173 端口，/api/v1 代理到 8080 的 C# 后端
bun run build      # 产物输出 ui/dist，构建 .NET 项目时会自动同步到 wwwroot/
```

## 与 Java 版的架构映射

| Java（Spring Boot 4） | C#（ASP.NET Core / net10.0） |
|---|---|
| `@SpringBootApplication` | `WebApplication.CreateBuilder` + top-level statement |
| `@RestController` | Minimal API（`MapGroup` + TypedResults） |
| `@Value` 注入配置 | Options 模式（`KlineDataOptions` + `IOptions<T>`） |
| `@HttpExchange` 声明式 RestClient | `IHttpClientFactory` 类型化客户端 `BinanceArchiveClient` |
| 固定线程池 + `invokeAll` | `Parallel.ForEachAsync`（MaxDegreeOfParallelism = 4） |
| `BigDecimal` + Builder 实体 | `decimal` + `readonly record struct Kline` |
| `java.time.YearMonth` | 自实现 `readonly record struct YearMonth` |
| 枚举方法 | `enum TimeFrame` + 扩展方法 |
| Jackson | System.Text.Json（camelCase；`Result<T>` null 字段省略，`DownloadResult` 保留 null——与 Jackson 注解行为逐项对应） |

与 Java 版仅有的已知差异：参数非法时 `code:5001` 响应里的异常消息文本来自各自框架（如月份解析错误的提示措辞），envelope 结构与各错误分支行为完全一致。
