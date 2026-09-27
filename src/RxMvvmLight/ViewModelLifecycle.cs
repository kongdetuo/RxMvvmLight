using RxMvvmLight.Helpers;

namespace RxMvvmLight;

public enum LifecycleState
{
    Created,
    Initialized,

    Activated,
    Destroyed,
}

public class ViewModelLifecycle
{
    private readonly Lock _gate = new();

    private readonly AsyncSignal _initializing = new();
    private readonly AsyncSignal _activating = new();
    private readonly AsyncSignal _deactivating = new();
    private readonly AsyncSignal _destroying = new();

    public IAsyncSignal Initializing => _initializing;
    public IAsyncSignal Activating => _activating;
    public IAsyncSignal Deactivating => _deactivating;
    public IAsyncSignal Destroying => _destroying;

    public LifecycleState State { get; private set; }

    private Task _tail = Task.CompletedTask;

    private Task EnqueueAsync(Func<Task> action)
    {
        lock (_gate)
        {
            async Task GetTask()
            {
                try
                {
                    await _tail;
                }
                catch { }
                await action();
            }

            return _tail = GetTask();
        }
    }

    public async Task InitializeAsync()
    {
        await EnqueueAsync(async () =>
        {
            await Transition(LifecycleState.Created, LifecycleState.Initialized, _initializing);
        });
    }

    public async Task ActivateAsync(bool ensureInitialized = true)
    {
        await EnqueueAsync(async () =>
        {
            if (ensureInitialized)
                await Transition(LifecycleState.Created, LifecycleState.Initialized, _initializing);
            await Transition(LifecycleState.Initialized, LifecycleState.Activated, _activating);
        });
    }

    public async Task DeactivateAsync()
    {
        await EnqueueAsync(async () =>
        {
            await Transition(LifecycleState.Activated, LifecycleState.Initialized, _deactivating);
        });
    }

    public async Task DestroyAsync()
    {
        await EnqueueAsync(async () =>
        {
            await Transition(LifecycleState.Activated, LifecycleState.Initialized, _deactivating);
            await Transition(LifecycleState.Initialized, LifecycleState.Destroyed, _destroying);
            await Transition(LifecycleState.Created, LifecycleState.Destroyed, _destroying);
        });
    }

    private async Task Transition(LifecycleState from, LifecycleState to, AsyncSignal signal)
    {
        if (State == from)
        {
            try
            {
                await signal.InvokeAsync();
            }
            finally
            {
                State = to;
            }
        }
    }

}

public interface IHasLifecycle
{
    ViewModelLifecycle Lifecycle { get; }
}

public static class LifecycleExtensions
{
    extension(IHasLifecycle self)
    {
        public IDisposable WhenInitialize(Action action)
            => self.WhenInitialize(action.AsValueTask());

        public IDisposable WhenInitialize(Func<ValueTask> action)
            => self.Lifecycle.Initializing.Subscribe(action);


        public IDisposable WhenActivate(Action action)
            => self.WhenActivate(action.AsValueTask());

        public IDisposable WhenActivate(Func<ValueTask> action)
            => self.Lifecycle.Activating.Subscribe(action);


        public IDisposable WhenDeactivate(Action action)
            => self.WhenDeactivate(action.AsValueTask());

        public IDisposable WhenDeactivate(Func<ValueTask> action)
            => self.Lifecycle.Deactivating.Subscribe(action);


        public IDisposable WhenDestroy(Action action)
            => self.WhenDestroy(action.AsValueTask());

        public IDisposable WhenDestroy(Func<ValueTask> action)
            => self.Lifecycle.Destroying.Subscribe(action);

    }

    extension(IDisposable self)
    {
        public IDisposable DisposeWith(IAsyncSignal signal)
        {
            // 取消订阅这个动作本身也是一个订阅
            // 这里让这个订阅完成任务后取消自身
            IDisposable? disposable = null;
            disposable = signal.Subscribe(() =>
            {
                try
                {
                    self.Dispose();
                }
                finally
                {
                    disposable?.Dispose();  // 如果某 IAsyncSignal 允许订阅时执行，这里就是null
                }
                return ValueTask.CompletedTask;
            });

            // 返回原始 IDisposable 方便链式挂载到其他触发源。
            return self;
        }

        public IDisposable DisposeWithDeactivate(IHasLifecycle vm)
            => self.DisposeWith(vm.Lifecycle.Deactivating);


        public IDisposable DisposeWithDeactivate(ViewModelLifecycle lifecycle)
            => self.DisposeWith(lifecycle.Deactivating);


        public IDisposable DisposeWithDestroy(IHasLifecycle vm)
            => self.DisposeWith(vm.Lifecycle.Destroying);


        public IDisposable DisposeWithDestroy(ViewModelLifecycle lifecycle)
            => self.DisposeWith(lifecycle.Destroying);
    }

    extension(IAsyncDisposable self)
    {
        public IAsyncDisposable DisposeWith(IAsyncSignal signal)
        {
            // 取消订阅这个动作本身也是一个订阅
            // 这里让这个订阅完成任务后取消自身
            IDisposable? disposable = null; 
            disposable = signal.Subscribe(async () =>
            {
                try
                {
                    await self.DisposeAsync();
                }
                finally
                {
                    disposable?.Dispose(); // 如果某 IAsyncSignal 允许订阅时执行，这里就是null
                }
            });

            // 返回原始 IDisposable 方便链式挂载到其他触发源。
            return self;
        }

        public IAsyncDisposable DisposeWithDeactivate(IHasLifecycle vm)
            => self.DisposeWith(vm.Lifecycle.Deactivating);


        public IAsyncDisposable DisposeWithDeactivate(ViewModelLifecycle lifecycle)
            => self.DisposeWith(lifecycle.Deactivating);


        public IAsyncDisposable DisposeWithDestroy(IHasLifecycle vm)
            => self.DisposeWith(vm.Lifecycle.Destroying);


        public IAsyncDisposable DisposeWithDestroy(ViewModelLifecycle lifecycle)
            => self.DisposeWith(lifecycle.Destroying);
    }
}
