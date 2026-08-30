using R3;
using System.Data;

namespace RxMvvmLight.Validation;

public class PropertyValidationBuilder<T>
{
    private readonly ReactiveValidator Validator;
    private readonly Observable<T> source;
    private readonly List<IRule<T>> rules = [];
    private CascadeMode cascadeMode = RxMvvmLight.Validation.CascadeMode.Stop;
    private readonly List<Observable<Unit>> dependsOnSource = [];
    public string PropertyName { get; }

    public PropertyValidationBuilder(ReactiveValidator validator, string propertyName, Observable<T> source)
    {
        this.Validator = validator;
        this.PropertyName = Normalize(propertyName);
        this.source = source;
    }

    public PropertyValidationBuilder<T> CascadeMode(CascadeMode mode)
    {
        this.cascadeMode = mode;
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

    public PropertyValidationBuilder<T> RegisterRule<TMessage>(Func<T, bool> evaluate, TMessage messageProvider)
    {
        return RegisterAsyncRule((value, _) => Task.FromResult(evaluate(value)), messageProvider);
    }

    public PropertyValidationBuilder<T> RegisterAsyncRule<TMessage>(Func<T, Task<bool>> evaluate, TMessage messageProvider)
    {
        return RegisterAsyncRule(async (value, token) => await evaluate(value), messageProvider);
    }

    public PropertyValidationBuilder<T> RegisterAsyncRule<TMessage>(Func<T, CancellationToken, Task<bool>> evaluate, TMessage messageProvider)
    {
        var rule = new AsyncTokenFuncRule<T> { Evaluate = evaluate, MessageProvider = () => MessageConverter.Convert(messageProvider) };
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

        Validator.Register(PropertyName, inputSource, rules, cascadeMode);
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
