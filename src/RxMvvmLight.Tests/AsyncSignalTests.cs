namespace RxMvvmLight.Tests;

public class AsyncSignalTests
{
    [Fact]
    public async Task InvokeAsync_WithoutHandlers_DoesNothing()
    {
        var signal = new AsyncSignal();

        await signal.InvokeAsync();
    }

    [Fact]
    public async Task InvokeAsync_CallsHandlersInSubscriptionOrder()
    {
        var signal = new AsyncSignal();
        var order = new List<string>();
        signal.Subscribe(() =>
        {
            order.Add("first");
            return ValueTask.CompletedTask;
        });
        signal.Subscribe(() =>
        {
            order.Add("second");
            return ValueTask.CompletedTask;
        });

        await signal.InvokeAsync();

        Assert.Equal(new[] { "first", "second" }, order);
    }

    [Fact]
    public async Task InvokeAsync_CallsHandlerOncePerInvocation()
    {
        var signal = new AsyncSignal();
        var count = 0;
        signal.Subscribe(() =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        await signal.InvokeAsync();
        await signal.InvokeAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task InvokeAsync_AwaitsAsyncHandlers()
    {
        var signal = new AsyncSignal();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = false;
        signal.Subscribe(async () =>
        {
            started = true;
            await gate.Task;
        });

        var invoke = signal.InvokeAsync();

        Assert.True(started);
        Assert.False(invoke.IsCompleted);

        gate.SetResult();
        await invoke;
    }

    [Fact]
    public async Task Subscribe_DisposeDetachesHandler()
    {
        var signal = new AsyncSignal();
        var count = 0;
        var subscription = signal.Subscribe(() =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        await signal.InvokeAsync();
        Assert.Equal(1, count);

        subscription.Dispose();
        await signal.InvokeAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Subscribe_DisposeTwice_DoesNotThrow()
    {
        var signal = new AsyncSignal();
        var subscription = signal.Subscribe(() => ValueTask.CompletedTask);

        subscription.Dispose();
        subscription.Dispose();

        await signal.InvokeAsync();
    }

    [Fact]
    public async Task InvokeAsync_UnsubscribeDuringInvocation_DoesNotAffectCurrentInvocation()
    {
        var signal = new AsyncSignal();
        var firstCount = 0;
        var secondCount = 0;
        IDisposable? second = null;
        signal.Subscribe(() =>
        {
            firstCount++;
            second?.Dispose();
            return ValueTask.CompletedTask;
        });
        second = signal.Subscribe(() =>
        {
            secondCount++;
            return ValueTask.CompletedTask;
        });

        // 只要触发了，快照里的 handler 必须全部执行
        await signal.InvokeAsync();
        Assert.Equal(1, firstCount);
        Assert.Equal(1, secondCount);

        // 下一次触发时已取消订阅的 handler 不再执行
        await signal.InvokeAsync();
        Assert.Equal(2, firstCount);
        Assert.Equal(1, secondCount);
    }

    [Fact]
    public async Task InvokeAsync_HandlerThrows_PropagatesAndSkipsRemainingHandlers()
    {
        var signal = new AsyncSignal();
        var secondRan = false;
        signal.Subscribe(() => throw new InvalidOperationException("boom"));
        signal.Subscribe(() =>
        {
            secondRan = true;
            return ValueTask.CompletedTask;
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => signal.InvokeAsync());
        Assert.False(secondRan);
    }

    [Fact]
    public async Task Clear_RemovesAllHandlers()
    {
        var signal = new AsyncSignal();
        var count = 0;
        signal.Subscribe(() =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        signal.Clear();
        await signal.InvokeAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Clear_ThenSubscribe_WorksAgain()
    {
        var signal = new AsyncSignal();
        var count = 0;
        signal.Clear();

        signal.Subscribe(() =>
        {
            count++;
            return ValueTask.CompletedTask;
        });
        await signal.InvokeAsync();

        Assert.Equal(1, count);
    }
}
