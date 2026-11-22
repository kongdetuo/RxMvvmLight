namespace RxMvvmLight.Tests;

public class ActivatableViewTests
{
    [Fact]
    public async Task WhenActivated_FiresOnActivated()
    {
        var view = new TestActivatableView();
        var count = 0;
        view.WhenActivated(() => count++);

        await view.ActivateAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WhenActivated_FiresOnEachActivation()
    {
        var view = new TestActivatableView();
        var count = 0;
        view.WhenActivated(() => count++);

        await view.ActivateAsync();
        await view.ActivateAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task WhenActivated_Disposed_DoesNotFire()
    {
        var view = new TestActivatableView();
        var count = 0;
        var subscription = view.WhenActivated(() => count++);
        subscription.Dispose();

        await view.ActivateAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task WhenActivated_Once_FiresOnlyOnFirstActivation()
    {
        var view = new TestActivatableView();
        var count = 0;
        view.WhenActivated(() => count++, once: true);

        await view.ActivateAsync();
        await view.ActivateAsync();
        await view.ActivateAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WhenActivated_Once_DisposedBeforeActivation_DoesNotFire()
    {
        var view = new TestActivatableView();
        var count = 0;
        var subscription = view.WhenActivated(() => count++, once: true);
        subscription.Dispose();

        await view.ActivateAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task WhenDeactivated_FiresOnDeactivated()
    {
        var view = new TestActivatableView();
        var count = 0;
        view.WhenDeactivated(() => count++);

        await view.DeactivateAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WhenDeactivated_Disposed_DoesNotFire()
    {
        var view = new TestActivatableView();
        var count = 0;
        var subscription = view.WhenDeactivated(() => count++);
        subscription.Dispose();

        await view.DeactivateAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DisposeWithDeactivate_ActivatableView_DisposesOnDeactivated()
    {
        var view = new TestActivatableView();
        var target = new CountingDisposable();
        target.DisposeWithDeactivate(view);

        await view.ActivateAsync();
        Assert.Equal(0, target.DisposeCount);

        await view.DeactivateAsync();
        Assert.Equal(1, target.DisposeCount);
    }

    [Fact]
    public async Task DisposeWithDeactivate_ActivatableView_SelfUnsubscribesAfterFirstFire()
    {
        var view = new TestActivatableView();
        var target = new CountingDisposable();
        target.DisposeWithDeactivate(view);

        await view.ActivateAsync();
        await view.DeactivateAsync();
        await view.ActivateAsync();
        await view.DeactivateAsync();

        Assert.Equal(1, target.DisposeCount);
    }

    [Fact]
    public async Task DisposeWithDeactivate_ActivatableView_AsyncDisposable()
    {
        var view = new TestActivatableView();
        var target = new AsyncCountingDisposable();
        target.DisposeWithDeactivate(view);

        await view.ActivateAsync();
        Assert.Equal(0, target.DisposeCount);

        await view.DeactivateAsync();
        Assert.Equal(1, target.DisposeCount);
    }

    [Fact]
    public void DisposeWithDeactivate_ActivatableView_ReturnsOriginalTarget()
    {
        var view = new TestActivatableView();
        var target = new CountingDisposable();

        var returned = target.DisposeWithDeactivate(view);

        Assert.Same(target, returned);
    }

    private sealed class TestActivatableView : IActivatableView
    {
        private readonly AsyncSignal _activated = new();
        private readonly AsyncSignal _deactivated = new();

        IAsyncSignal IActivatableView.Activated => _activated;

        IAsyncSignal IActivatableView.Deactivated => _deactivated;

        public Task ActivateAsync() => _activated.InvokeAsync();

        public Task DeactivateAsync() => _deactivated.InvokeAsync();
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
