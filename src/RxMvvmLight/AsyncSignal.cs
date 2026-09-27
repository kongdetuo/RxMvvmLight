using R3;
using RxMvvmLight.Helpers;

namespace RxMvvmLight;


public interface IAsyncSignal
{
    IDisposable Subscribe(Func<ValueTask> action);
}

internal class AsyncSignal : IAsyncSignal
{
    private readonly Lock gate = new();
    private readonly List<Func<ValueTask>> handlers = [];

    public async Task InvokeAsync()
    {
        // 复制一份，防止在执行中途取消订阅导致报错
        // 换言之：只要触发了，必须全部执行(除非报错)，不许执行一半突然决定不执行了
        // 如果有类似需求，让他自己用 Canceltoken 搞。
        List<Func<ValueTask>> snapshot;
        lock (gate)
        {
            snapshot = [.. this.handlers];
        }

        foreach (var item in snapshot)
        {
            await item.Invoke();
        }
    }

    public IDisposable Subscribe(Func<ValueTask> handler)
    {
        lock (gate)
        {
            handlers.Add(handler);
        }
        return Disposable.Create(() =>
        {
            lock (gate)
            {
                handlers.Remove(handler);
            }
        });
    }

    public void Clear()
    {
        lock (gate)
        {
            handlers.Clear();
        }
    }
}
public static class AsyncSignalExtensions
{
    extension(IAsyncSignal self)
    {
        public IDisposable SubscribeOnce(Func<ValueTask> handler)
        {
            IDisposable? disposable = null;
            disposable = self.Subscribe(async () =>
            {
                disposable?.Dispose();
                await handler();
            });
            return disposable;
        }
    }
}