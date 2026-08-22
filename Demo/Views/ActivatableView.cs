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
            (DataContext as IActivatable)?.Activator.Activate();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            (DataContext as IActivatable)?.Activator.Deactivate();
        }
    }
}