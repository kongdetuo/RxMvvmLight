using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using R3;

namespace RxMvvmLight.Validation;

public interface IValidationObservableObject : IObservableObject, INotifyDataErrorInfo
{
    Validator Validator { get; }
}

public class ValidationObservableObject : ObservableObject, IValidationObservableObject
{
    private bool errorDisplayActivated = false;
    private readonly Dictionary<string, List<string>?> lastNotifiedErrors = new();

    public Validator Validator => field ??= new();


    public bool HasErrors => errorDisplayActivated && Validator.HasErrors;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName) => errorDisplayActivated ? Validator.GetErrors(propertyName) : Enumerable.Empty<string>();

    protected ValidationObservableObject()
    {
        Validator.StateChanged.Subscribe(propertyName =>
        {
            var currentErrors = Validator.GetErrors(propertyName).Cast<string>().ToList();
            if (lastNotifiedErrors.TryGetValue(propertyName, out var last) && last.SequenceEqual(currentErrors))
                return;

            lastNotifiedErrors[propertyName] = currentErrors;

            if (this.errorDisplayActivated)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(HasErrors);
            }
        });

        // 忽略注册验证器之前的变化
        this.Changed
            .Where(p => Validator.ContainsProperty(p.PropertyName))
            .Take(1).Subscribe(x =>
            {
                this.errorDisplayActivated = true;
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(x.PropertyName));
            });
    }
}

public static class ValidationObservableObjectExtensions
{
    public static PropertyValidationBuilder<TValue> RuleFor<TVM, TValue>(
        this TVM viewModel,
        TValue value,
        [CallerArgumentExpression(nameof(value))] string propertyName = "")
        where TVM : IValidationObservableObject, INotifyDataErrorInfo
    {
        return new(viewModel.Validator, propertyName, viewModel.GetObservable(value, new Separator(), propertyName));
    }

    public static PropertyValidationBuilder<TValue> RuleFor<TVM, TValue>(
        this TVM viewModel,
        Observable<TValue> observable,
        string propertyName)
        where TVM : IValidationObservableObject, INotifyDataErrorInfo
    {
        return new(viewModel.Validator, propertyName, observable);
    }
}
