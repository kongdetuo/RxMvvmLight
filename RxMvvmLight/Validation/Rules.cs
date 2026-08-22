namespace RxMvvmLight;

public enum ValidationBehavior
{
    FailFast,
    CollectAll
}

internal interface IRule<T>
{
}

internal sealed class DebounceRule<T>(TimeSpan duration) : IRule<T>
{
    public TimeSpan Duration { get; } = duration;
}

internal sealed class AsyncTokenFuncRule<T> : IRule<T>
{
    public required Func<T, CancellationToken, Task<string[]>> Evaluate { get; init; }
}

internal sealed class ConditionalRule<T>(Func<T, bool> condition) : IRule<T>
{
    public Func<T, bool> Condition { get; } = condition;

    public List<IRule<T>> InnerRules { get; init; } = [];
}
