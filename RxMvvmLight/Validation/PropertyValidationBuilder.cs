using R3;
using RxMvvmLight.Validation;
using System.Data;

namespace RxMvvmLight;

public class PropertyValidationBuilder<T>
{
    private readonly Validator Validator;
    private readonly Observable<T> source;
    private readonly List<IRule<T>> rules = [];
    private ValidationBehavior behavior = ValidationBehavior.FailFast;
    private readonly List<Observable<Unit>> dependsOnSource = [];
    public string PropertyName { get; }

    public PropertyValidationBuilder(Validator validator, string propertyName, Observable<T> source)
    {
        this.Validator = validator;
        this.PropertyName = Normalize(propertyName);
        this.source = source;
    }

    public PropertyValidationBuilder<T> Behavior(ValidationBehavior behavior = ValidationBehavior.FailFast)
    {
        this.behavior = behavior;
        return this;
    }

    public PropertyValidationBuilder<T> Debounce(TimeSpan timeSpan)
    {
        AddRule(new DebounceRule<T>(timeSpan));
        return this;
    }

    public PropertyValidationBuilder<T> Debounce(uint timeSpan) => Debounce(TimeSpan.FromMilliseconds(timeSpan));

    public PropertyValidationBuilder<T> When(Func<T, bool> condition)
    {
        if (rules.Count > 0 && rules[^1] is ConditionalRule<T> last && last.InnerRules.Count == 0)
        {
            rules[^1] = new ConditionalRule<T>(v => last.Condition(v) && condition(v));
        }
        else
        {
            this.rules.Add(new ConditionalRule<T>(condition));
        }
        return this;
    }

    public PropertyValidationBuilder<T> DependsOn<TRefresh>(Observable<TRefresh> source)
    {
        dependsOnSource.Add(source.Select(_ => Unit.Default));
        return this;
    }

    public PropertyValidationBuilder<T> RegisterRule(Func<T, string[]> evaluate)
    {
        return RegisterAsyncRule((value, _) => Task.FromResult(evaluate(value)));
    }

    public PropertyValidationBuilder<T> RegisterAsyncRule(Func<T, Task<string[]>> evaluate)
    {
        return RegisterAsyncRule(async (value, token) => await evaluate(value));
    }

    public PropertyValidationBuilder<T> RegisterAsyncRule(Func<T, CancellationToken, Task<string[]>> evaluate)
    {
        var rule = new AsyncTokenFuncRule<T> { Evaluate = evaluate };
        return AddRule(rule);
    }

    public void Subscribe()
    {
        while (rules.Count > 0 && rules[^1] is DebounceRule<T>)
            rules.RemoveAt(rules.Count - 1);

        var inputSource = source;
        if(dependsOnSource.Count > 0)
        {
            var denpend = dependsOnSource.Count > 1
                ? Observable.CombineLatest(dependsOnSource).Select(_ => Unit.Default)
                : dependsOnSource[0];
            inputSource = source.CombineLatest(denpend, (x, _) => x);
        }

        var pipeline = Validator.GetOrCreatePipeline(PropertyName, inputSource);
        pipeline.AddRegistration(rules, behavior);
    }

    private PropertyValidationBuilder<T> AddRule(IRule<T> rule)
    {
        if (rules.Count > 0 && rules.Last() is ConditionalRule<T> conditional)
        {
            conditional.InnerRules.Add(rule);
        }
        else
        {
            rules.Add(rule);
        }
        return this;
    }

    private static string Normalize(string expression)
    {
        var arrow = expression.IndexOf("=>", StringComparison.Ordinal);
        var rhs = arrow >= 0 ? expression[(arrow + 2)..] : expression;
        rhs = rhs.Trim().TrimStart('(');
        var index = rhs.LastIndexOf('.');
        return (index >= 0 ? rhs[(index + 1)..] : rhs).Trim().TrimEnd('!', '?');
    }
}
