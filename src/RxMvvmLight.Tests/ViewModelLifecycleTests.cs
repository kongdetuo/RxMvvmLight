namespace RxMvvmLight.Tests;

public class ViewModelLifecycleTests
{
    [Fact]
    public void NewLifecycle_StartsInCreatedState()
    {
        var lifecycle = new ViewModelLifecycle();

        Assert.Equal(LifecycleState.Created, lifecycle.State);
    }

    [Fact]
    public async Task InitializeAsync_TransitionsToInitialized()
    {
        var lifecycle = new ViewModelLifecycle();

        await lifecycle.InitializeAsync();

        Assert.Equal(LifecycleState.Initialized, lifecycle.State);
    }

    [Fact]
    public async Task InitializeAsync_FiresInitializingOnlyOnce()
    {
        var lifecycle = new ViewModelLifecycle();
        var count = 0;
        lifecycle.Initializing.Subscribe(() =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        await lifecycle.InitializeAsync();
        await lifecycle.InitializeAsync();

        Assert.Equal(1, count);
        Assert.Equal(LifecycleState.Initialized, lifecycle.State);
    }

    [Fact]
    public async Task ActivateAsync_FromCreated_InitializesThenActivates()
    {
        var vm = new LifecycleVm();
        var order = new List<string>();
        vm.WhenInitialize(() => order.Add("initialize"));
        vm.WhenActivate(() => order.Add("activate"));

        await vm.Lifecycle.ActivateAsync();

        Assert.Equal(new[] { "initialize", "activate" }, order);
        Assert.Equal(LifecycleState.Activated, vm.Lifecycle.State);
    }

    [Fact]
    public async Task ActivateAsync_Twice_DedupsActivating()
    {
        var lifecycle = new ViewModelLifecycle();
        var count = 0;
        lifecycle.Activating.Subscribe(() =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        await lifecycle.ActivateAsync();
        await lifecycle.ActivateAsync();

        Assert.Equal(1, count);
        Assert.Equal(LifecycleState.Activated, lifecycle.State);
    }

    [Fact]
    public async Task ActivateAsync_EnsureInitializedFalse_FromCreated_DoesNothing()
    {
        var lifecycle = new ViewModelLifecycle();
        var initializing = 0;
        var activating = 0;
        lifecycle.Initializing.Subscribe(() =>
        {
            initializing++;
            return ValueTask.CompletedTask;
        });
        lifecycle.Activating.Subscribe(() =>
        {
            activating++;
            return ValueTask.CompletedTask;
        });

        await lifecycle.ActivateAsync(ensureInitialized: false);

        Assert.Equal(LifecycleState.Created, lifecycle.State);
        Assert.Equal(0, initializing);
        Assert.Equal(0, activating);
    }

    [Fact]
    public async Task ActivateAsync_EnsureInitializedFalse_FromInitialized_Activates()
    {
        var lifecycle = new ViewModelLifecycle();
        var activating = 0;
        lifecycle.Activating.Subscribe(() =>
        {
            activating++;
            return ValueTask.CompletedTask;
        });
        await lifecycle.InitializeAsync();

        await lifecycle.ActivateAsync(ensureInitialized: false);

        Assert.Equal(LifecycleState.Activated, lifecycle.State);
        Assert.Equal(1, activating);
    }

    [Fact]
    public async Task DeactivateAsync_WhenActivated_FiresDeactivating()
    {
        var lifecycle = new ViewModelLifecycle();
        var count = 0;
        lifecycle.Deactivating.Subscribe(() =>
        {
            count++;
            return ValueTask.CompletedTask;
        });
        await lifecycle.ActivateAsync();

        await lifecycle.DeactivateAsync();

        Assert.Equal(1, count);
        Assert.Equal(LifecycleState.Initialized, lifecycle.State);
    }

    [Fact]
    public async Task DeactivateAsync_WhenNotActivated_IsNoOp()
    {
        var lifecycle = new ViewModelLifecycle();
        var count = 0;
        lifecycle.Deactivating.Subscribe(() =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        await lifecycle.DeactivateAsync();
        await lifecycle.InitializeAsync();
        await lifecycle.DeactivateAsync();

        Assert.Equal(0, count);
        Assert.Equal(LifecycleState.Initialized, lifecycle.State);
    }

    [Fact]
    public async Task Reactivation_FiresActivatingEachTime()
    {
        var lifecycle = new ViewModelLifecycle();
        var activating = 0;
        var deactivating = 0;
        lifecycle.Activating.Subscribe(() =>
        {
            activating++;
            return ValueTask.CompletedTask;
        });
        lifecycle.Deactivating.Subscribe(() =>
        {
            deactivating++;
            return ValueTask.CompletedTask;
        });

        await lifecycle.ActivateAsync();
        await lifecycle.DeactivateAsync();
        await lifecycle.ActivateAsync();

        Assert.Equal(2, activating);
        Assert.Equal(1, deactivating);
        Assert.Equal(LifecycleState.Activated, lifecycle.State);
    }

    [Fact]
    public async Task DestroyAsync_FromActivated_DeactivatesBeforeDestroying()
    {
        var lifecycle = new ViewModelLifecycle();
        var order = new List<string>();
        lifecycle.Deactivating.Subscribe(() =>
        {
            order.Add("deactivate");
            return ValueTask.CompletedTask;
        });
        lifecycle.Destroying.Subscribe(() =>
        {
            order.Add("destroy");
            return ValueTask.CompletedTask;
        });
        await lifecycle.ActivateAsync();

        await lifecycle.DestroyAsync();

        Assert.Equal(new[] { "deactivate", "destroy" }, order);
        Assert.Equal(LifecycleState.Destroyed, lifecycle.State);
    }

    [Fact]
    public async Task DestroyAsync_FromInitialized_OnlyDestroys()
    {
        var lifecycle = new ViewModelLifecycle();
        var deactivating = 0;
        var destroying = 0;
        lifecycle.Deactivating.Subscribe(() =>
        {
            deactivating++;
            return ValueTask.CompletedTask;
        });
        lifecycle.Destroying.Subscribe(() =>
        {
            destroying++;
            return ValueTask.CompletedTask;
        });
        await lifecycle.InitializeAsync();

        await lifecycle.DestroyAsync();

        Assert.Equal(0, deactivating);
        Assert.Equal(1, destroying);
        Assert.Equal(LifecycleState.Destroyed, lifecycle.State);
    }

    [Fact]
    public async Task DestroyAsync_FromCreated_DestroysDirectly()
    {
        var lifecycle = new ViewModelLifecycle();
        var destroying = 0;
        lifecycle.Destroying.Subscribe(() =>
        {
            destroying++;
            return ValueTask.CompletedTask;
        });

        await lifecycle.DestroyAsync();

        Assert.Equal(1, destroying);
        Assert.Equal(LifecycleState.Destroyed, lifecycle.State);
    }

    [Fact]
    public async Task DestroyAsync_Twice_SecondIsNoOp()
    {
        var lifecycle = new ViewModelLifecycle();
        var destroying = 0;
        lifecycle.Destroying.Subscribe(() =>
        {
            destroying++;
            return ValueTask.CompletedTask;
        });

        await lifecycle.DestroyAsync();
        await lifecycle.DestroyAsync();

        Assert.Equal(1, destroying);
        Assert.Equal(LifecycleState.Destroyed, lifecycle.State);
    }

    [Fact]
    public async Task AfterDestroy_AllTransitionsAreNoOps()
    {
        var lifecycle = new ViewModelLifecycle();
        var initializing = 0;
        var activating = 0;
        var deactivating = 0;
        var destroying = 0;
        lifecycle.Initializing.Subscribe(() =>
        {
            initializing++;
            return ValueTask.CompletedTask;
        });
        lifecycle.Activating.Subscribe(() =>
        {
            activating++;
            return ValueTask.CompletedTask;
        });
        lifecycle.Deactivating.Subscribe(() =>
        {
            deactivating++;
            return ValueTask.CompletedTask;
        });
        lifecycle.Destroying.Subscribe(() =>
        {
            destroying++;
            return ValueTask.CompletedTask;
        });
        await lifecycle.ActivateAsync();
        await lifecycle.DestroyAsync();

        await lifecycle.InitializeAsync();
        await lifecycle.ActivateAsync();
        await lifecycle.DeactivateAsync();
        await lifecycle.DestroyAsync();

        Assert.Equal(1, initializing);
        Assert.Equal(1, activating);
        Assert.Equal(1, deactivating);
        Assert.Equal(1, destroying);
        Assert.Equal(LifecycleState.Destroyed, lifecycle.State);
    }

    [Fact]
    public async Task State_AdvancesOnlyAfterHandlersComplete()
    {
        var lifecycle = new ViewModelLifecycle();
        LifecycleState? stateDuringHandler = null;
        lifecycle.Activating.Subscribe(() =>
        {
            stateDuringHandler = lifecycle.State;
            return ValueTask.CompletedTask;
        });

        await lifecycle.ActivateAsync();

        Assert.Equal(LifecycleState.Initialized, stateDuringHandler);
        Assert.Equal(LifecycleState.Activated, lifecycle.State);
    }

    [Fact]
    public async Task HandlerException_StillAdvancesState_AndTailRecovers()
    {
        var lifecycle = new ViewModelLifecycle();
        var activating = 0;
        lifecycle.Initializing.Subscribe(() => throw new InvalidOperationException("boom"));
        lifecycle.Activating.Subscribe(() =>
        {
            activating++;
            return ValueTask.CompletedTask;
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => lifecycle.InitializeAsync());
        Assert.Equal(LifecycleState.Initialized, lifecycle.State);

        await lifecycle.ActivateAsync();
        Assert.Equal(1, activating);
        Assert.Equal(LifecycleState.Activated, lifecycle.State);
    }

    [Fact]
    public async Task AsyncHandler_IsAwaitedBeforeTransitionCompletes()
    {
        var lifecycle = new ViewModelLifecycle();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = false;
        lifecycle.Activating.Subscribe(async () =>
        {
            started = true;
            await gate.Task;
        });

        var activate = lifecycle.ActivateAsync();

        Assert.True(started);
        Assert.False(activate.IsCompleted);
        Assert.Equal(LifecycleState.Initialized, lifecycle.State);

        gate.SetResult();
        await activate;

        Assert.Equal(LifecycleState.Activated, lifecycle.State);
    }

    [Fact]
    public async Task ConcurrentTransitions_AreSerializedInCallOrder()
    {
        var lifecycle = new ViewModelLifecycle();
        var order = new List<string>();
        lifecycle.Activating.Subscribe(async () =>
        {
            await Task.Delay(10);
            order.Add("activate");
        });
        lifecycle.Deactivating.Subscribe(() =>
        {
            order.Add("deactivate");
            return ValueTask.CompletedTask;
        });

        var activate = lifecycle.ActivateAsync();
        var deactivate = lifecycle.DeactivateAsync();

        await Task.WhenAll(activate, deactivate);

        Assert.Equal(new[] { "activate", "deactivate" }, order);
        Assert.Equal(LifecycleState.Initialized, lifecycle.State);
    }

    [Fact]
    public async Task SignalSubscription_Disposed_StopsReceivingEvents()
    {
        var lifecycle = new ViewModelLifecycle();
        var count = 0;
        var subscription = lifecycle.Activating.Subscribe(() =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        await lifecycle.ActivateAsync();
        subscription.Dispose();
        await lifecycle.DeactivateAsync();
        await lifecycle.ActivateAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WhenActivate_FiresOnEachActivation()
    {
        var vm = new LifecycleVm();
        var count = 0;
        vm.WhenActivate(() => count++);

        await vm.Lifecycle.ActivateAsync();
        await vm.Lifecycle.DeactivateAsync();
        await vm.Lifecycle.ActivateAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task WhenInitialize_FiresOnlyOnce()
    {
        var vm = new LifecycleVm();
        var count = 0;
        vm.WhenInitialize(() => count++);

        await vm.Lifecycle.ActivateAsync();
        await vm.Lifecycle.DeactivateAsync();
        await vm.Lifecycle.ActivateAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WhenDeactivate_FiresOnlyOnDeactivation()
    {
        var vm = new LifecycleVm();
        var count = 0;
        vm.WhenDeactivate(() => count++);

        await vm.Lifecycle.ActivateAsync();
        Assert.Equal(0, count);

        await vm.Lifecycle.DeactivateAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WhenDestroy_FiresOnDestroy()
    {
        var vm = new LifecycleVm();
        var count = 0;
        vm.WhenDestroy(() => count++);

        await vm.Lifecycle.ActivateAsync();
        await vm.Lifecycle.DestroyAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WhenSubscription_Disposed_DoesNotFire()
    {
        var vm = new LifecycleVm();
        var count = 0;
        var subscription = vm.WhenActivate(() => count++);
        subscription.Dispose();

        await vm.Lifecycle.ActivateAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DisposeWithDeactivate_DisposesOnDeactivate()
    {
        var vm = new LifecycleVm();
        var target = new CountingDisposable();
        target.DisposeWithDeactivate(vm);

        await vm.Lifecycle.ActivateAsync();
        Assert.Equal(0, target.DisposeCount);

        await vm.Lifecycle.DeactivateAsync();
        Assert.Equal(1, target.DisposeCount);
    }

    [Fact]
    public async Task DisposeWithDestroy_DisposesOnDestroy()
    {
        var vm = new LifecycleVm();
        var target = new CountingDisposable();
        target.DisposeWithDestroy(vm);

        await vm.Lifecycle.ActivateAsync();
        await vm.Lifecycle.DestroyAsync();

        Assert.Equal(1, target.DisposeCount);
    }

    [Fact]
    public async Task DisposeWith_WithLifecycleInstance_Works()
    {
        var lifecycle = new ViewModelLifecycle();
        var target = new CountingDisposable();
        target.DisposeWith(lifecycle.Deactivating);

        await lifecycle.ActivateAsync();
        await lifecycle.DeactivateAsync();

        Assert.Equal(1, target.DisposeCount);
    }

    [Fact]
    public async Task DisposeWith_SelfUnsubscribesAfterFirstFire()
    {
        var vm = new LifecycleVm();
        var target = new CountingDisposable();
        target.DisposeWithDeactivate(vm);

        await vm.Lifecycle.ActivateAsync();
        await vm.Lifecycle.DeactivateAsync();
        await vm.Lifecycle.ActivateAsync();
        await vm.Lifecycle.DeactivateAsync();

        Assert.Equal(1, target.DisposeCount);
    }

    [Fact]
    public void DisposeWith_WhenSignalNeverFires_NotDisposed()
    {
        var target = new CountingDisposable();
        target.DisposeWithDeactivate(new ViewModelLifecycle());

        Assert.Equal(0, target.DisposeCount);
    }

    [Fact]
    public void DisposeWith_ReturnsOriginalTarget()
    {
        var target = new CountingDisposable();
        var returned = target.DisposeWithDestroy(new ViewModelLifecycle());

        Assert.Same(target, returned);
    }

    [Fact]
    public async Task DisposeWith_AsyncDisposable_DisposesOnDeactivate()
    {
        var vm = new LifecycleVm();
        var target = new AsyncCountingDisposable();
        target.DisposeWithDeactivate(vm);

        await vm.Lifecycle.ActivateAsync();
        Assert.Equal(0, target.DisposeCount);

        await vm.Lifecycle.DeactivateAsync();
        Assert.Equal(1, target.DisposeCount);
    }

    [Fact]
    public async Task DisposeWith_AsyncDisposable_SelfUnsubscribesAfterFirstFire()
    {
        var vm = new LifecycleVm();
        var target = new AsyncCountingDisposable();
        target.DisposeWithDeactivate(vm);

        await vm.Lifecycle.ActivateAsync();
        await vm.Lifecycle.DeactivateAsync();
        await vm.Lifecycle.ActivateAsync();
        await vm.Lifecycle.DeactivateAsync();

        Assert.Equal(1, target.DisposeCount);
    }

    [Fact]
    public async Task LifecycleValidationVm_WorksWithLifecycleExtensions()
    {
        var vm = new LifecycleValidationVm();
        var order = new List<string>();
        vm.WhenInitialize(() => order.Add("initialize"));
        vm.WhenActivate(() => order.Add("activate"));
        vm.WhenDeactivate(() => order.Add("deactivate"));
        vm.WhenDestroy(() => order.Add("destroy"));

        await vm.Lifecycle.ActivateAsync();
        await vm.Lifecycle.DeactivateAsync();
        await vm.Lifecycle.DestroyAsync();

        Assert.Equal(new[] { "initialize", "activate", "deactivate", "destroy" }, order);
    }

    private sealed class CountingDisposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    private sealed class AsyncCountingDisposable : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}
