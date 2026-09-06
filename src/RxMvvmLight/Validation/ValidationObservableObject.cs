using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using R3;

namespace RxMvvmLight.Validation;

public interface IValidationObservableObject : INotifyDataErrorInfo
{
    ReactiveValidator Validator { get; }
}

public class ValidationObservableObject : ObservableObject, IValidationObservableObject
{
    private bool errorDisplayActivated = false;

    public ReactiveValidator Validator => field ??= new();

    public bool HasErrors => errorDisplayActivated && Validator.HasErrors;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName) => errorDisplayActivated ? Validator.GetErrors(propertyName) : Enumerable.Empty<string>();

    protected ValidationObservableObject()
    {
        Validator!.ErrorsChanged.Subscribe(errors =>
        {
            if (this.errorDisplayActivated)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(errors.PropertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        });

        // 忽略注册验证器之前的变化
        this.Changed
            .Where(p => p.PropertyName != null)
            .Where(p => Validator.ContainsProperty(p.PropertyName!))
            .Take(1).Subscribe(x =>
            {
                this.errorDisplayActivated = true;
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(x.PropertyName));
            });
    }
}

public static class ValidationObservableObjectExtensions
{
    public static PropertyValidatorBuilder<TValue> RuleFor<TVM, TValue>(
        this TVM viewModel,
        Func<TVM, TValue> expression,
        [CallerArgumentExpression(nameof(expression))] string propertyName = "")
        where TVM : IValidationObservableObject, INotifyDataErrorInfo, INotifyPropertyChanged
    {
        var name = RxMvvmLight.Helpers.PropertyNameHelper.ExtractNames(propertyName)[0];
        return new(viewModel.Validator, name, viewModel.ObserveChanged(expression, propertyName));
    }

    public static PropertyValidatorBuilder<TValue> RuleFor<TVM, TValue>(
        this TVM viewModel,
        Observable<TValue> observable,
        string propertyName)
        where TVM : IValidationObservableObject, INotifyDataErrorInfo
    {
        return new(viewModel.Validator, propertyName, observable);
    }
}
