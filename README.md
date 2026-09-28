# kline-data-downloader

从 Binance 公开数据归档（[data.binance.vision](https://data.binance.vision/)）批量下载历史 K 线，自动解压、排序、去重后按 UTC 天存储为 CSV，并提供 Web 管理界面与查询 API。

## 功能特性

- **历史 K 线拉取**：按月份区间下载 Binance 官方归档 zip（现货 / USDT-M 合约），单次请求内并行下载（最多 4 并发）
- **按天分文件存储**：`{BaseDir}/{category}/{SYMBOL}/{timeframe}/{yyyy-MM-dd}.csv`，价格精度原样保留（`decimal`，无浮点误差）
- **自动清洗**：时间戳自动归一化（秒 / 毫秒 / 微秒统一为毫秒）并做范围校验，重复时间戳去重（保留首条）
- **查询 API**：按交易对 + 周期读取本地 CSV，支持月份过滤，输出图表库可直接消费的秒级时间戳数据
- **内置前端**：Vue 3 + Arco Design 单页应用，拉取配置与结果展示开箱即用

## 环境要求

| 依赖 | 版本 | 用途 |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | 后端构建与运行（必装） |
| [Bun](https://bun.sh/) | 1.0+ | 前端构建与本地开发（仅前端相关操作需要） |

## 快速开始

```bash
git clone <repo-url>
cd kline-data-downloader
dotnet run
```

浏览器打开 <http://localhost:8080> 即可使用，数据默认写入 `./kline-data/`（相对进程工作目录）。

> 前端构建产物（`wwwroot/` 与 `ui/dist`）不入库，新 clone 后如需页面请先构建前端：
>
> ```bash
> cd ui && bun install && bun run build
> ```
>
> 之后 `dotnet run` / `dotnet build` 会自动将 `ui/dist` 同步到 `wwwroot/`。不构建前端时，API 仍可正常使用。

## 配置

`appsettings.json`：

| 配置键 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `Urls` | string | `http://0.0.0.0:8080` | 服务监听地址 |
| `KlineData:BaseDir` | string | `./kline-data` | K 线 CSV 输出根目录（相对进程工作目录） |

## API

所有接口均返回 HTTP 200，业务结果通过统一封装表达：

```json
{"code": 0, "message": "success", "data": ...}
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `code` | integer | `0` 成功；`5001` 异常（参数非法、IO 失败等） |
| `message` | string | `code=0` 时固定为 `"success"`；异常时为错误描述，可直接透出给用户 |
| `data` | object / array | 业务数据；异常时不返回该字段 |

### 拉取 K 线

`POST /api/v1/data/pull`

请求体（JSON）：

| 参数 | 类型 | 必填 | 取值 | 说明 |
|---|---|---|---|---|
| `category` | string | 是 | `spot` / `future` | 交易类型；`future` 对应币安 USDT-M 合约归档（`futures/um`） |
| `baseAsset` | string | 是 | 如 `BTC` | 基础币，服务端自动转大写 |
| `quoteAsset` | string | 是 | 如 `USDT` | 报价币，服务端自动转大写 |
| `timeframe` | string | 是 | `1m` `5m` `15m` `30m` `1h` `4h` `1d` | K 线周期 |
| `startMonth` | string | 是 | `YYYY-MM` | 起始月份（含） |
| `endMonth` | string | 是 | `YYYY-MM` | 结束月份（含），须不早于 `startMonth` |

可提交示例：

```json
{"category":"spot","baseAsset":"BTC","quoteAsset":"USDT","timeframe":"30m","startMonth":"2024-01","endMonth":"2024-01"}
```

成功响应（完整，数据为实测样例）：

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "downloadedFiles": 1,
    "totalKlines": 1488,
    "filePath": "./kline-data/spot/BTCUSDT/30m",
    "errors": []
  }
}
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `downloadedFiles` | integer | 实际下载到数据的月份数（归档缺失或无数据的月份不计入） |
| `totalKlines` | integer | 去重后实际写盘的 K 线总数 |
| `filePath` | string | 输出目录；校验失败或未写盘时为 `null`（该字段为 `null` 时仍会返回，不省略） |
| `errors` | string[] | 错误列表；空数组表示无错误 |

单个月份失败不会中断整体：归档中不存在的月份（404/403）静默跳过，其余错误逐月记入 `errors`。所有月份均无数据时 `filePath` 为 `null`，`errors` 为 `["No data downloaded"]`。

参数校验失败不抛异常，仍返回 `code=0`，通过 `errors` 表达：

| 场景 | errors 内容 |
|---|---|
| `timeframe` 不在枚举内 | `["Invalid timeframe: <原值>"]` |
| `startMonth` 晚于 `endMonth` | `["startMonth must be <= endMonth"]` |
| `category` 非法 | 每个月份一条 `"Month download failed: Unknown category: <原值>"` |

`startMonth` / `endMonth` 格式非法（非 `YYYY-MM`）时返回 `code=5001`。

### 查询本地 K 线

`GET /api/v1/data/klines`

Query 参数：

| 参数 | 类型 | 必填 | 取值 | 说明 |
|---|---|---|---|---|
| `category` | string | 是 | `spot` / `future` | 交易类型 |
| `baseAsset` | string | 是 | 如 `BTC` | 基础币 |
| `quoteAsset` | string | 是 | 如 `USDT` | 报价币 |
| `timeframe` | string | 是 | `1m` `5m` `15m` `30m` `1h` `4h` `1d` | K 线周期 |
| `startMonth` | string | 否 | `YYYY-MM` | 过滤起始月份（含）；须与 `endMonth` **同时提供**才生效，缺一不过滤 |
| `endMonth` | string | 否 | `YYYY-MM` | 过滤结束月份（含） |

响应示例（完整，数据为实测样例）：

```json
{
  "code": 0,
  "message": "success",
  "data": [
    {"time": 1704067200, "open": 42283.58000000, "high": 42554.57000000, "low": 42261.02000000, "close": 42419.73000000, "volume": 823.95971000},
    {"time": 1704069000, "open": 42419.73000000, "high": 42490.74000000, "low": 42354.19000000, "close": 42475.23000000, "volume": 447.72137000}
  ]
}
```

`data` 按时间升序，`time` 为 **epoch 秒**（CSV 中存毫秒）。`category` / `timeframe` 非法时返回 `code=5001`。

## 数据存储

目录结构（按天拆分）：

```
{BaseDir}/
  spot/
    BTCUSDT/
      1m/
        2024-01-01.csv
        2024-01-02.csv
  future/
    ETHUSDT/
      30m/
        2024-04-01.csv
```

CSV 无表头，行格式 `timestamp_ms,open,high,low,close,volume`：

```
1704067200000,42283.58000000,42554.57000000,42261.02000000,42419.73000000,823.95971000
```

- 文件按 **UTC 天**拆分，读取端按文件名（即日期）顺序合并
- 每次拉取**整体覆写**对应日期的文件，不做增量合并：重复拉取同一区间以最新结果覆盖

## 前端开发

```bash
cd ui
bun install
bun run dev      # http://localhost:5173，/api/v1 自动代理到 8080 后端
bun run build    # 产物输出 ui/dist，dotnet 构建时自动同步到 wwwroot/
```

技术栈：Vue 3（Composition API + `<script setup lang="ts">`）、vue-router（hash 模式）、Arco Design Vue、axios、Vite。

## 项目结构

```
KlineDataDownloader.csproj
Program.cs                        # 主机装配 + Minimal API 端点
appsettings.json                  # 配置
Configuration/KlineDataOptions.cs # Options 模式配置绑定
Domain/                           # 领域模型（Category / TimeFrame / Pair / YearMonth / Kline）
Contracts/                        # API 契约（Result / DownloadRequest / DownloadResult / KlinePoint）
Binance/BinanceArchiveClient.cs   # 归档下载客户端（IHttpClientFactory 类型化客户端）
Services/KlineDownloader.cs       # 拉取主流程：并行下载 → 解析 → 去重 → 按天写盘
Storage/KlineStore.cs             # 本地 CSV 读取
ui/                               # Vue 3 前端源码
wwwroot/                          # 前端构建产物（构建时自动生成，不入库）
```
