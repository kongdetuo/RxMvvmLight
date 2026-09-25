using Avalonia.Controls;
using Avalonia.Interactivity;
using RxMvvmLight;

namespace Demo.Views
{
    public abstract class ActivatableView : UserControl
    {
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (DataContext is IHasLifecycle vm)
                _ = vm.Lifecycle.ActivateAsync();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            if (DataContext is IHasLifecycle vm)
                _ = vm.Lifecycle.DeactivateAsync();
        }
    }
}