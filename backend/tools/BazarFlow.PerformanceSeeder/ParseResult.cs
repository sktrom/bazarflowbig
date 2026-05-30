namespace BazarFlow.PerformanceSeeder;

public sealed record ParseResult<T>(bool IsSuccess, T? Value, string? Error)
{
    public static ParseResult<T> Ok(T value) => new(true, value, null);

    public static ParseResult<T> Fail(string error) => new(false, default, error);
}
