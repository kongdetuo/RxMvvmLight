using R3;

namespace RxMvvmLight.Validation;

internal interface IPropertyValidator : IDisposable
{
    string PropertyName { get; }
    void Start();
}

internal sealed class PropertyValidator<T> : IPropertyValidator
{
    private readonly ReactiveValidator validator;
    private readonly object gate = new();
    private readonly Observable<T> source;
    private readonly List<IRule<T>> allRules;
    private CascadeMode cascadeMode;
    private IDisposable? subscription;

    public string PropertyName { get; }

    internal PropertyValidator(ReactiveValidator validator, string propertyName, Observable<T> source, List<IRule<T>> rules, CascadeMode cascadeMode)
    {
        this.validator = validator;
        this.PropertyName = propertyName;
        this.source = source;
        this.allRules = rules;
        this.cascadeMode = cascadeMode;
    }

    public void Start()
    {
        subscription = BuildSubscription();
    }

    public void Dispose()
    {
        IDisposable? sub;
        lock (gate)
        {
            sub = subscription;
            subscription = null;
        }
        sub?.Dispose();
    }

    private IDisposable BuildSubscription()
    {
        var rulesSnapshot = allRules.ToArray();
        var currentBehavior = cascadeMode;
        return source
            .SelectAwait(async (value, ct) =>
            {
                validator.SetState(PropertyName, ValidatorState.Validating);
                try
                {
                    return await Evaluate(rulesSnapshot, value, currentBehavior, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ObservableSystem.GetUnhandledExceptionHandler().Invoke(ex);
                    return [() => ex.Message];
                }
            }, AwaitOperation.Switch)
            .Subscribe(errors =>
            {
                validator.SetState(PropertyName, errors.Count == 0 ? ValidatorState.Valid : ValidatorState.Invalid);
                validator.SetErrors(PropertyName, errors);
            });
    }

    private async Task<List<Func<string>>> Evaluate(IReadOnlyList<IRule<T>> rules, T value, CascadeMode mode, CancellationToken token)
    {
        var errors = new List<Func<string>>();
        var count = 0;
        foreach (var rule in rules)
        {
            if (mode == CascadeMode.Stop && errors.Count > 0)
            {
                break;
            }

            if (token.IsCancellationRequested)
            {
                break;
            }

            if (rule is DebounceRule<T> debounceRule)
            {
                // 这里不要把token传进去，会变卡
                await Task.Delay(debounceRule.Duration);
            }
            else if (rule is ConditionalRule<T> conditionalRule)
            {
                if (conditionalRule.Condition(value))
                {
                    var innerErrors = await Evaluate(conditionalRule.InnerRules, value, mode, token);
                    errors.AddRange(innerErrors);
                }
            }
            else if (rule is AsyncTokenFuncRule<T> asyncFuncRule)
            {
                var isValid = await asyncFuncRule.Evaluate(value, token);
                if (!isValid)
                {
                    errors.Add(asyncFuncRule.MessageProvider);
                }
            }
            else
            {
                throw new InvalidOperationException($"不支持的规则类型: {rule.GetType().Name}");
            }

            if (errors.Count != count)
            {
                count = errors.Count;
                token.ThrowIfCancellationRequested();
                validator.SetErrors(PropertyName, errors);
            }
        }
        validator.SetErrors(PropertyName, errors);
        return errors;
    }
}
