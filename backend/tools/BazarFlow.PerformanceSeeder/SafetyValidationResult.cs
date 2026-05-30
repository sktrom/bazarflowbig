namespace BazarFlow.PerformanceSeeder;

public sealed record SafetyValidationResult(bool IsSuccess, IReadOnlyList<string> Errors)
{
    public static SafetyValidationResult Success() => new(true, []);

    public static SafetyValidationResult Fail(IReadOnlyList<string> errors) => new(false, errors);
}
