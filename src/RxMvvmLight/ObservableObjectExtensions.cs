using System.ComponentModel;
using System.Runtime.CompilerServices;
using R3;
using RxMvvmLight.Helpers;

namespace RxMvvmLight;

public static class ObservableObjectExtensions
{
    extension<TVM>(TVM vm) where TVM : INotifyPropertyChanged
    {
        public Observable<PropertyChangedEventArgs> Changed =>
            Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                 h => (sender, e) => h(e),
                 h => vm.PropertyChanged += h,
                 h => vm.PropertyChanged -= h);

        public Observable<TValue> ObserveChanged<TValue>(
            Func<TVM, TValue> accessor,
            [CallerArgumentExpression(nameof(accessor))] string expression = "")
        {
            var names = PropertyNameHelper.ExtractNames(expression);

            return vm.Changed
                .Where(p => names.Contains(p.PropertyName))
                .Select(p => accessor(vm))
                .Prepend(accessor(vm));
        }
    }

    extension<TVM>(TVM vm) where TVM : INotifyPropertyChanging
    {
        public Observable<PropertyChangingEventArgs> Changing =>
            Observable.FromEvent<PropertyChangingEventHandler, PropertyChangingEventArgs>(
                 h => (sender, e) => h(e),
                 h => vm.PropertyChanging += h,
                 h => vm.PropertyChanging -= h);

        public Observable<TValue> ObserveChanging<TValue>(
            Func<TVM, TValue> accessor,
            [CallerArgumentExpression(nameof(accessor))] string expression = "")
        {
            var names = PropertyNameHelper.ExtractNames(expression);

            return vm.Changing
                .Where(p => names.Contains(p.PropertyName))
                .Select(p => accessor(vm));
        }
    }
}
