namespace RxMvvmLight;

/// <summary>
/// 视图侧的激活/失活信号。View 不需要实现完整的状态机，只需在合适时机触发这两个信号。
/// </summary>
public interface IActivatableView
{
    /// <summary>视图激活时触发。</summary>
    IAsyncSignal Activated { get; }

    /// <summary>视图失活时触发。</summary>
    IAsyncSignal Deactivated { get; }
}

public static class ActivatableViewExtensions
{
    extension(IActivatableView self)
    {
        public IDisposable WhenActivated(Action action, bool once = false)
            => once
                ? self.Activated.SubscribeOnce(action)
                : self.Activated.Subscribe(action);

        public IDisposable WhenDeactivated(Action action)
            => self.Deactivated.Subscribe(action);
    }

    extension(IDisposable self)
    {
        public IDisposable DisposeWithDeactivate(IActivatableView view)
            => self.DisposeWith(view.Deactivated);
    }

    extension(IAsyncDisposable self)
    {
        public IAsyncDisposable DisposeWithDeactivate(IActivatableView view)
            => self.DisposeWith(view.Deactivated);
    }
}
