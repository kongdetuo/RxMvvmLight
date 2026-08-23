using System.ComponentModel;
using System.Runtime.CompilerServices;
using R3;

namespace RxMvvmLight;

public static class ObservableObjectExtensions
{

    public static Observable<TValue> GetObservable<TVM, TValue>(this TVM vm, TValue value,
        Separator _ = default,
        [CallerArgumentExpression(nameof(value))] string name = "")
        where TVM : IObservableObject
    {
        var index = name.LastIndexOf('.');
        if (index > 0)
        {
            name = name[(index + 1)..].Trim();
        }

        return vm.Changed.Where(p => p.PropertyName == name).Select(p => (TValue)p.Value!).Prepend(value);
    }

    public static Observable<(TValue1, TValue2)> GetObservable<TVM, TValue1, TValue2>(this TVM viewModel,
        TValue1 value1,
        TValue2 value2,
        Separator _ = default,
        [CallerArgumentExpression(nameof(value1))] string name1 = "",
        [CallerArgumentExpression(nameof(value2))] string name2 = "")
        where TVM : IObservableObject
    {
        return Observable.CombineLatest(
            GetObservable(viewModel, value1, new Separator(), name1),
            GetObservable(viewModel, value2, new Separator(), name2),
            (v1, v2) => ((v1, v2)));
    }

    public static Observable<TReault> GetObservable<TVM, TValue1, TValue2, TReault>(this TVM viewModel,
        TValue1 value1,
        TValue2 value2,
        Func<TValue1, TValue2, TReault> selector,
        Separator _ = default,
        [CallerArgumentExpression(nameof(value1))] string name1 = "",
        [CallerArgumentExpression(nameof(value2))] string name2 = "")
        where TVM : IObservableObject
    {
        return Observable.CombineLatest(
            GetObservable(viewModel, value1, new Separator(), name1),
            GetObservable(viewModel, value2, new Separator(), name2),
            selector);
    }

    public static Observable<(TValue1, TValue2, TValue3)> GetObservable<TVM, TValue1, TValue2, TValue3>(this TVM viewModel,
        TValue1 value1,
        TValue2 value2,
        TValue3 value3,
        Separator _ = default,
        [CallerArgumentExpression(nameof(value1))] string name1 = "",
        [CallerArgumentExpression(nameof(value2))] string name2 = "",
        [CallerArgumentExpression(nameof(value3))] string name3 = "")
        where TVM : IObservableObject
    {
        return Observable.CombineLatest(
            GetObservable(viewModel, value1, new Separator(), name1),
            GetObservable(viewModel, value2, new Separator(), name2),
            GetObservable(viewModel, value3, new Separator(), name3),
            (v1, v2, v3) => ((v1, v2, v3)));
    }

    public static Observable<TResult> GetObservable<TVM, TValue1, TValue2, TValue3, TResult>(this TVM viewModel,
        TValue1 value1,
        TValue2 value2,
        TValue3 value3,
        Func<TValue1, TValue2, TValue3, TResult> selector,
        Separator _ = default,
        [CallerArgumentExpression(nameof(value1))] string name1 = "",
        [CallerArgumentExpression(nameof(value2))] string name2 = "",
        [CallerArgumentExpression(nameof(value3))] string name3 = "")
        where TVM : IObservableObject
    {
        return Observable.CombineLatest(
            GetObservable(viewModel, value1, new Separator(), name1),
            GetObservable(viewModel, value2, new Separator(), name2),
            GetObservable(viewModel, value3, new Separator(), name3),
            selector);
    }

    public static Observable<(TValue1, TValue2, TValue3, TValue4)> GetObservable<TVM, TValue1, TValue2, TValue3, TValue4>(this TVM viewModel,
        TValue1 value1,
        TValue2 value2,
        TValue3 value3,
        TValue4 value4,
        Separator _ = default,
        [CallerArgumentExpression(nameof(value1))] string name1 = "",
        [CallerArgumentExpression(nameof(value2))] string name2 = "",
        [CallerArgumentExpression(nameof(value3))] string name3 = "",
        [CallerArgumentExpression(nameof(value4))] string name4 = "")
        where TVM : IObservableObject
    {
        return Observable.CombineLatest(
            GetObservable(viewModel, value1, new Separator(), name1),
            GetObservable(viewModel, value2, new Separator(), name2),
            GetObservable(viewModel, value3, new Separator(), name3),
            GetObservable(viewModel, value4, new Separator(), name4),
            (v1, v2, v3, v4) => ((v1, v2, v3, v4)));
    }

    public static Observable<TResult> GetObservable<TVM, TValue1, TValue2, TValue3, TValue4, TResult>(this TVM viewModel,
        TValue1 value1,
        TValue2 value2,
        TValue3 value3,
        TValue4 value4,
        Func<TValue1, TValue2, TValue3, TValue4, TResult> selector,
        Separator _ = default,
        [CallerArgumentExpression(nameof(value1))] string name1 = "",
        [CallerArgumentExpression(nameof(value2))] string name2 = "",
        [CallerArgumentExpression(nameof(value3))] string name3 = "",
        [CallerArgumentExpression(nameof(value4))] string name4 = "")
        where TVM : IObservableObject
    {
        return Observable.CombineLatest(
            GetObservable(viewModel, value1, new Separator(), name1),
            GetObservable(viewModel, value2, new Separator(), name2),
            GetObservable(viewModel, value3, new Separator(), name3),
            GetObservable(viewModel, value4, new Separator(), name4),
            selector);
    }

    public static Observable<(TValue1, TValue2, TValue3, TValue4, TValue5)> GetObservable<TVM, TValue1, TValue2, TValue3, TValue4, TValue5>(this TVM viewModel,
        TValue1 value1,
        TValue2 value2,
        TValue3 value3,
        TValue4 value4,
        TValue5 value5,
        Separator _ = default,
        [CallerArgumentExpression(nameof(value1))] string name1 = "",
        [CallerArgumentExpression(nameof(value2))] string name2 = "",
        [CallerArgumentExpression(nameof(value3))] string name3 = "",
        [CallerArgumentExpression(nameof(value4))] string name4 = "",
        [CallerArgumentExpression(nameof(value5))] string name5 = "")
        where TVM : IObservableObject
    {
        return Observable.CombineLatest(
            GetObservable(viewModel, value1, new Separator(), name1),
            GetObservable(viewModel, value2, new Separator(), name2),
            GetObservable(viewModel, value3, new Separator(), name3),
            GetObservable(viewModel, value4, new Separator(), name4),
            GetObservable(viewModel, value5, new Separator(), name5),
            (v1, v2, v3, v4, v5) => ((v1, v2, v3, v4, v5)));
    }

    public static Observable<TResult> GetObservable<TVM, TValue1, TValue2, TValue3, TValue4, TValue5, TResult>(this TVM viewModel,
        TValue1 value1,
        TValue2 value2,
        TValue3 value3,
        TValue4 value4,
        TValue5 value5,
        Func<TValue1, TValue2, TValue3, TValue4, TValue5, TResult> selector,
        Separator _ = default,
        [CallerArgumentExpression(nameof(value1))] string name1 = "",
        [CallerArgumentExpression(nameof(value2))] string name2 = "",
        [CallerArgumentExpression(nameof(value3))] string name3 = "",
        [CallerArgumentExpression(nameof(value4))] string name4 = "",
        [CallerArgumentExpression(nameof(value5))] string name5 = "")
    where TVM : IObservableObject
    {
        return Observable.CombineLatest(
            GetObservable(viewModel, value1, new Separator(), name1),
            GetObservable(viewModel, value2, new Separator(), name2),
            GetObservable(viewModel, value3, new Separator(), name3),
            GetObservable(viewModel, value4, new Separator(), name4),
            GetObservable(viewModel, value5, new Separator(), name5),
            selector);
    }


    [Obsolete("做的不好，不要用")]
    public static Observable<TValue?> ObserveChanged<TVM, TValue>(
        this TVM vm,
        Func<TVM, TValue> accessor,
        [CallerArgumentExpression(nameof(accessor))] string expression = "")
        where TVM : IObservableObject
    {
        var names = GetPropertyPath(expression);
        if (names.Length == 0)
            return Observable.Empty<TValue?>();

        return ObserveLevel<TValue>(vm, names, 0);
    }

    private static string[] GetPropertyPath(string expression)
    {
        var arrow = expression.IndexOf("=>", StringComparison.Ordinal);
        var rhs = arrow >= 0 ? expression[(arrow + 2)..] : expression;
        rhs = rhs.Trim().TrimStart('(');
        var segments = rhs.Split('.')
            .Select(s => s.Trim().TrimEnd('!', '?'))
            .Where(s => s.Length > 0)
            .ToArray();
        return segments.Length <= 1 ? Array.Empty<string>() : segments[1..];
    }

    private static Observable<TValue?> ObserveLevel<TValue>(IObservableObject current, string[] names, int index)
    {
        if (index == names.Length - 1)
        {
            return current.Changed.Where(p => p.PropertyName == names[index])
                .Select(p => (TValue?)p.Value)
                .Prepend(GetCurrent<TValue>(current, names, index));
        }

        var childChanges = current.Changed.Where(p => p.PropertyName == names[index])
            .Select(p => p.Value as IObservableObject);

        return childChanges
            .Prepend(current.GetPropertyValue(names[index]) as IObservableObject)
            .Select(child => child is null
                ? Observable.Return<TValue?>(default)
                : ObserveLevel<TValue>(child, names, index + 1))
            .Switch();
    }

    private static TValue? GetCurrent<TValue>(IObservableObject root, string[] names, int index)
    {
        object? current = root;
        for (var i = index; i < names.Length; i++)
        {
            if (current is not IObservableObject observable)
                return default;
            current = observable.GetPropertyValue(names[i]);
        }
        return (TValue?)current;
    }
}
