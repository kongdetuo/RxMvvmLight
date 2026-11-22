using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RxMvvmLight;

namespace Demo.Views
{
    public abstract class ActivatableView : UserControl, IActivatableView
    {
        private readonly AsyncSignal _activated = new();
        private readonly AsyncSignal _deactivated = new();

        // 显式实现，避免与平台自带的 Activated / Deactivated 事件撞名。
        IAsyncSignal IActivatableView.Activated => _activated;

        IAsyncSignal IActivatableView.Deactivated => _deactivated;

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            _ = ActivateAsync();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            _ = DeactivateAsync();
        }

        private async Task ActivateAsync()
        {
            // 先 VM 后 View：WhenActivated 的回调能看到已初始化的 VM。
            if (DataContext is IHasLifecycle vm)
                await vm.Lifecycle.ActivateAsync();
            await _activated.InvokeAsync();
        }

        private async Task DeactivateAsync()
        {
            // 先 View 后 VM：View 先释放自己的订阅，再关闭 VM。
            await _deactivated.InvokeAsync();
            if (DataContext is IHasLifecycle vm)
                await vm.Lifecycle.DeactivateAsync();
        }
    }
}
