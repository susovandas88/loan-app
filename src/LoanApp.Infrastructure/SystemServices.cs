using LoanApp.Application.Contracts;

namespace LoanApp.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class FeatureFlags : IFeatureFlags
{
    public bool AiAssistant { get; init; }
    public bool AiVerificationAssist { get; init; }
}
