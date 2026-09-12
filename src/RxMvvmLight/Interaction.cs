using R3;

namespace RxMvvmLight;

public sealed class Interaction<TIn, TOut> : IDisposable
{
    public delegate Task<TOut> HandlerWithPrevious(TIn input, Func<TIn, Task<TOut>> previousHandler);
    private readonly List<Func<TIn, Task<TOut>>> handlers = [];
    private readonly Lock gate = new();
    private bool disposed;

    public IDisposable RegisterHandler(Func<TIn, TOut> handler) =>
        RegisterHandler(input => Task.FromResult(handler(input)));

    public IDisposable RegisterHandler(Func<TIn, Task<TOut>> handler)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
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

    public IDisposable RegisterHandler(HandlerWithPrevious handler)
    {
        Func<TIn, Task<TOut>> innerHandler = null!;

        innerHandler = new Func<TIn, Task<TOut>>(async (input) =>
        {
            async Task<TOut> previous(TIn input)
            {
                Func<TIn, Task<TOut>> innerFallback = null!;
                lock (gate)
                {
                    ObjectDisposedException.ThrowIf(disposed, this);

                    var index = handlers.IndexOf(innerHandler);
                    innerFallback = index > 0 ? handlers[index - 1] : throw new InvalidOperationException("没有回退 handler");
                }
                return await innerFallback(input);
            }

            return await handler(input, previous);
        });

        return RegisterHandler(innerHandler);
    }

    public async Task<TOut> Handle(TIn input)
    {
        Func<TIn, Task<TOut>> handler;
        lock (gate)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Interaction<TIn, TOut>));
            if (handlers.Count == 0)
                throw new InvalidOperationException("Interaction 未注册任何 handler");
            handler = handlers[^1];
        }

        return await handler(input);
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
            handlers.Clear();
        }
    }
}