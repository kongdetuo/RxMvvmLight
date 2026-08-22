using R3;
using RxMvvmLight.Validation;

namespace RxMvvmLight;

internal interface IPropertyPipeline : IDisposable
{
    string PropertyName { get; }
}

internal sealed class PropertyPipeline<T> : IPropertyPipeline
{
    private readonly Validator validator;
    private readonly object gate = new();
    private readonly Observable<T> source;
    private readonly List<IRule<T>> allRules = new();
    private ValidationBehavior behavior = ValidationBehavior.FailFast;
    private IDisposable? subscription;

    public string PropertyName { get; }

    internal PropertyPipeline(Validator validator, string propertyName, Observable<T> source)
    {
        this.validator = validator;
        this.PropertyName = propertyName;
        this.source = source;
    }

    internal void AddRegistration(List<IRule<T>> registrationRules, ValidationBehavior behavior)
    {
        lock (gate)
        {
            allRules.AddRange(registrationRules);
            this.behavior = behavior;

            subscription?.Dispose();
            subscription = BuildSubscription();
        }
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
        var currentBehavior = behavior;
        return source
            .SelectAwait(async (value, ct) =>
            {
                validator.BeginEvaluation(PropertyName);
                try
                {
                    return await Evaluate(rulesSnapshot, value, currentBehavior, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch(Exception ex)
                {
                    ObservableSystem.GetUnhandledExceptionHandler().Invoke(ex);
                    return new List<string> { ex.Message };
                }
            }, AwaitOperation.Switch)
            .Subscribe(errors =>
            {
                validator.SetState(PropertyName, errors.Count == 0 ? ValidationState.Valid : ValidationState.Invalid);
                validator.SetErrors(PropertyName, errors);
            });
    }

    private async Task<List<string>> Evaluate(IReadOnlyList<IRule<T>> rules, T value, ValidationBehavior behavior, CancellationToken token)
    {
        var errors = new List<string>();
        var count = 0;
        foreach (var rule in rules)
        {
            if(behavior == ValidationBehavior.FailFast && errors.Count > 0)
            {
                break;
            }

            token.ThrowIfCancellationRequested();

            if (rule is DebounceRule<T> debounceRule)
            {
                await Task.Delay(debounceRule.Duration, token);
            }
            else if (rule is ConditionalRule<T> conditionalRule)
            {
                if (conditionalRule.Condition(value))
                {
                    var innerErrors = await Evaluate(conditionalRule.InnerRules, value, behavior, token);
                    errors.AddRange(innerErrors);
                }
            }
            else if (rule is AsyncTokenFuncRule<T> asyncFuncRule)
            {
                errors.AddRange(await asyncFuncRule.Evaluate(value, token));
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
