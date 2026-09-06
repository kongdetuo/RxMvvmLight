using R3;

namespace RxMvvmLight;

public sealed class Interaction<TIn, TOut> : IDisposable
{
    private sealed class Request
    {
        public Request(TIn input, TaskCompletionSource<TOut> completion)
        {
            Input = input;
            Completion = completion;
        }

        public TIn Input { get; }

        public TaskCompletionSource<TOut> Completion { get; }
    }

    private readonly List<Func<TIn, Task<TOut>>> handlers = new();
    private readonly Lock gate = new();
    private bool disposed;

    public IDisposable RegisterHandler(Func<TIn, TOut> handler) =>
        RegisterHandler(input => Task.FromResult(handler(input)));

    public IDisposable RegisterHandler(Func<TIn, Task<TOut>> handler)
    {
        lock (gate)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Interaction<TIn, TOut>));
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

    public Task<TOut> Handle(TIn input)
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

        var tcs = new TaskCompletionSource<TOut>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = ProcessAsync(new Request(input, tcs), handler);
        return tcs.Task;
    }

    private static async Task ProcessAsync(Request request, Func<TIn, Task<TOut>> handler)
    {
        try
        {
            var result = await handler(request.Input).ConfigureAwait(false);
            request.Completion.TrySetResult(result);
        }
        catch (Exception ex)
        {
            request.Completion.TrySetException(ex);
        }
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