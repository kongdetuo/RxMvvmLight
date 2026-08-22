using R3;
using RxMvvmLight;

namespace RxMvvmLight.Tests;

public class ActivatableTests
{
    [Fact]
    public void Activator_InitialState_IsFalse()
    {
        var vm = new ActivatableVm();
        bool? state = null;
        vm.Activator.IsActive.Subscribe(x => state = x);

        Assert.False(state);
    }

    [Fact]
    public void Activator_ActivateThenDeactivate_FlipsState()
    {
        var vm = new ActivatableVm();
        var received = new List<bool>();
        vm.Activator.IsActive.Subscribe(received.Add);

        vm.Activator.Activate();
        vm.Activator.Deactivate();

        Assert.Equal(new[] { false, true, false }, received);
    }

    [Fact]
    public void Activator_RepeatedActivate_Dedups()
    {
        var vm = new ActivatableVm();
        var fired = 0;
        vm.WhenActivated(() => fired++);

        vm.Activator.Activate();
        vm.Activator.Activate();

        Assert.Equal(1, fired);
    }

    [Fact]
    public void WhenActivated_NotFiredAtConstructionWhenInactive()
    {
        var vm = new ActivatableVm();
        var fired = 0;
        vm.WhenActivated(() => fired++);

        Assert.Equal(0, fired);
    }

    [Fact]
    public void WhenActivated_FiresOnActivation()
    {
        var vm = new ActivatableVm();
        var fired = 0;
        vm.WhenActivated(() => fired++);

        vm.Activator.Activate();
        Assert.Equal(1, fired);
    }

    [Fact]
    public void WhenActivated_FiresAgainOnReactivate()
    {
        var vm = new ActivatableVm();
        var fired = 0;
        vm.WhenActivated(() => fired++);

        vm.Activator.Activate();
        vm.Activator.Deactivate();
        vm.Activator.Activate();

        Assert.Equal(2, fired);
    }

    [Fact]
    public void WhenActivated_RegisteredWhileActive_FiresImmediately()
    {
        var vm = new ActivatableVm();
        vm.Activator.Activate();

        var fired = 0;
        vm.WhenActivated(() => fired++);

        Assert.Equal(1, fired);
    }

    [Fact]
    public void WhenFirstActivated_FiresExactlyOnce()
    {
        var vm = new ActivatableVm();
        var fired = 0;
        vm.WhenFirstActivated(() => fired++);

        vm.Activator.Activate();
        vm.Activator.Deactivate();
        vm.Activator.Activate();

        Assert.Equal(1, fired);
    }

    [Fact]
    public void WhenFirstActivated_RunsBeforeWhenActivated_RegardlessOfRegistrationOrder()
    {
        var vm = new ActivatableVm();
        var order = new List<string>();
        vm.WhenActivated(() => order.Add("activated"));
        vm.WhenFirstActivated(() => order.Add("first"));

        vm.Activator.Activate();

        Assert.Equal(new[] { "first", "activated" }, order);
    }

    [Fact]
    public void WhenFirstActivated_RegisteredAfterFirstActivation_DoesNotFire()
    {
        var vm = new ActivatableVm();
        vm.Activator.Activate();

        var fired = 0;
        vm.WhenFirstActivated(() => fired++);

        Assert.Equal(0, fired);
    }

    [Fact]
    public void WhenDeactivated_NotFiredAtConstruction()
    {
        var vm = new ActivatableVm();
        var fired = 0;
        vm.WhenDeactivated(() => fired++);

        Assert.Equal(0, fired);
    }

    [Fact]
    public void WhenDeactivated_FiresOnTrueToFalseTransition()
    {
        var vm = new ActivatableVm();
        var fired = 0;
        vm.WhenDeactivated(() => fired++);

        vm.Activator.Activate();
        Assert.Equal(0, fired);
        vm.Activator.Deactivate();
        Assert.Equal(1, fired);
    }

    [Fact]
    public void WhenDeactivated_NotFiredOnReactivate()
    {
        var vm = new ActivatableVm();
        var fired = 0;
        vm.WhenDeactivated(() => fired++);

        vm.Activator.Activate();
        vm.Activator.Deactivate();
        Assert.Equal(1, fired);

        vm.Activator.Activate();
        Assert.Equal(1, fired);
    }

    [Fact]
    public async Task WhenActivatedAsync_SwitchCancelsInFlight()
    {
        var vm = new ActivatableVm();
        var started = 0;
        var cancelled = 0;
        var firstCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        vm.WhenActivated(async ct =>
        {
            started++;
            ct.Register(() =>
            {
                cancelled++;
                firstCancelled.TrySetResult();
            });
            await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
        });

        vm.Activator.Activate();
        Assert.Equal(1, started);

        vm.Activator.Deactivate();
        vm.Activator.Activate();

        await firstCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, started);
        Assert.Equal(1, cancelled);
    }

    [Fact]
    public void DisposeWith_NotDisposedOnDeactivate()
    {
        var vm = new ActivatableVm();
        var disposed = 0;
        Disposable.Create(() => disposed++).DisposeWith(vm.Activator);

        vm.Activator.Activate();
        vm.Activator.Deactivate();

        Assert.Equal(0, disposed);
    }

    [Fact]
    public void DisposeWith_DisposedOnActivatorDispose()
    {
        var vm = new ActivatableVm();
        var disposed = 0;
        Disposable.Create(() => disposed++).DisposeWith(vm.Activator);

        vm.Activator.Dispose();

        Assert.Equal(1, disposed);
    }

    [Fact]
    public void DisposeWith_DisposingLinkDetaches()
    {
        var vm = new ActivatableVm();
        var disposed = 0;
        var target = Disposable.Create(() => disposed++);
        var link = target.DisposeWith(vm.Activator);

        link.Dispose();
        vm.Activator.Dispose();

        Assert.Equal(0, disposed);
    }

    [Fact]
    public void DisposeWhenDeactivated_NotDisposedAtConstruction()
    {
        var vm = new ActivatableVm();
        var disposed = 0;
        Disposable.Create(() => disposed++).DisposeWhenDeactivated(vm.Activator);

        Assert.Equal(0, disposed);
    }

    [Fact]
    public void DisposeWhenDeactivated_DisposedOnFirstDeactivation()
    {
        var vm = new ActivatableVm();
        var disposed = 0;
        Disposable.Create(() => disposed++).DisposeWhenDeactivated(vm.Activator);

        vm.Activator.Activate();
        Assert.Equal(0, disposed);
        vm.Activator.Deactivate();
        Assert.Equal(1, disposed);
    }

    [Fact]
    public void DisposeWhenDeactivated_DisposedOnActivatorDisposeWithoutActivation()
    {
        var vm = new ActivatableVm();
        var disposed = 0;
        Disposable.Create(() => disposed++).DisposeWhenDeactivated(vm.Activator);

        vm.Activator.Dispose();

        Assert.Equal(1, disposed);
    }

    [Fact]
    public void DisposeWhenDeactivated_NotResurrectedOnReactivate()
    {
        var vm = new ActivatableVm();
        var disposed = 0;
        Disposable.Create(() => disposed++).DisposeWhenDeactivated(vm.Activator);

        vm.Activator.Activate();
        vm.Activator.Deactivate();
        Assert.Equal(1, disposed);

        vm.Activator.Activate();
        Assert.Equal(1, disposed);
    }

    [Fact]
    public void Activator_Dispose_CompletesIsActive()
    {
        var vm = new ActivatableVm();
        var completed = 0;
        vm.Activator.IsActive.Subscribe(_ => { }, _ => { }, _ => completed++);

        vm.Activator.Dispose();
        vm.Activator.Dispose();

        Assert.Equal(1, completed);
    }

    [Fact]
    public void ActivatableValidationVm_ActivatorWorks()
    {
        var vm = new ActivatableValidationVm();
        var fired = 0;
        vm.WhenFirstActivated(() => fired++);

        vm.Activator.Activate();

        Assert.Equal(1, fired);
    }
}