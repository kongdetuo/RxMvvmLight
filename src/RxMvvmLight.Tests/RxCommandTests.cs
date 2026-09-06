using R3;
using RxMvvmLight;
using System.Threading;

namespace RxMvvmLight.Tests;

public class RxCommandTests
{
    // 模拟 UI 同步上下文：Post 内联执行，使 ObserveOnCurrentSynchronizationContext 确定性交付。
    // MVVM 库按"存在 SynchronizationContext"设计（真实 UI 线程即此情形），测试据此书写。
    private sealed class InlineSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
        public override void Send(SendOrPostCallback d, object? state) => d(state);
    }

    private static void InUiContext(Action action)
    {
        var original = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
        try
        {
            action();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    private static async Task InUiContext(Func<Task> action)
    {
        var original = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
        try
        {
            await action();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    [Fact]
    public async Task CanExecute_ControlsExecution()
    {
        await InUiContext(async () =>
        {
            var canExecute = new Subject<bool>();
            var executed = 0;
            var cmd = RxCommand.Create(() => executed++, canExecute);
            await cmd.ExecuteAsync(new object());

            Assert.Equal(1, executed);

            canExecute.OnNext(false);
            await cmd.ExecuteAsync(new object());
            Assert.Equal(1, executed);

            canExecute.OnNext(true);
            await cmd.ExecuteAsync(new object());
            Assert.Equal(2, executed);
        });
    }

    [Fact]
    public async Task IsRunning_TracksExecution()
    {
        await InUiContext(async () =>
        {
            var cmd = RxCommand.Create(async () =>
            {
                await Task.Delay(100);
            });

            var running = new List<bool>();
            cmd.IsRunning.Subscribe(running.Add);

            await cmd.ExecuteAsync(new object());

            Assert.Contains(true, running);
            Assert.False(running[^1]);
        });
    }

    [Fact]
    public void CanExecuteChanged_RaisesOnStateChange()
    {
        InUiContext(() =>
        {
            var canExecute = new Subject<bool>();
            var cmd = RxCommand.Create(() => { }, canExecute);
            int raised = 0;
            cmd.CanExecuteChanged += (_, _) => raised++;

            canExecute.OnNext(false);
            Assert.Equal(1, raised);

            canExecute.OnNext(false);
            Assert.Equal(1, raised);

            canExecute.OnNext(true);
            Assert.Equal(2, raised);
        });
    }

    [Fact]
    public async Task ParameterlessCommand_ExecutesWithNullParameter()
    {
        await InUiContext(async () =>
        {
            var executed = 0;
            var cmd = RxCommand.Create(() => executed++);

            await cmd.ExecuteAsync(null);

            Assert.Equal(1, executed);
        });
    }

    [Fact]
    public async Task NullableReferenceCommand_ExecutesWithNullParameter()
    {
        await InUiContext(async () =>
        {
            string? received = "sentinel";
            var cmd = RxCommand.Create<string>(s => received = s);

            await cmd.ExecuteAsync(null);

            Assert.Null(received);
        });
    }

    [Fact]
    public async Task ValueTypeCommand_DoesNotExecuteWithNullParameter()
    {
        await InUiContext(async () =>
        {
            var executed = 0;
            var cmd = RxCommand.Create<int>(_ => executed++);

            await cmd.ExecuteAsync(null);

            Assert.Equal(0, executed);
        });
    }
}