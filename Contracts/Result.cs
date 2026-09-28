using System.Text.Json.Serialization;

namespace KlineDataDownloader.Contracts;

/// <summary>统一响应封装：code=0 成功；null 字段不序列化。</summary>
public sealed record Result<T>(
    int Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] T? Data)
{
    public static Result<T> Success(T data) => new(0, "success", data);

    public static Result<T> Error(int code, string? message) => new(code, message, default);
}
