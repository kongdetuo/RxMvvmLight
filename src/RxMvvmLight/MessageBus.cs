using R3;

namespace RxMvvmLight;

public sealed class MessageBus
{
    public static readonly MessageBus Instance = new();

    public void Publish<TMessage>(TMessage message) => MessageBus<TMessage>.Instance.Publish(message);

    public IDisposable Subscribe<TMessage>(Action<TMessage> handler) => MessageBus<TMessage>.Instance.Subscribe(handler);
}

internal class MessageBus<TMessage>
{
    public static readonly MessageBus<TMessage> Instance = new();

    readonly Lock _gate = new();
    readonly List<Action<TMessage>> list = [];

    public void Publish(TMessage message)
    {
        List<Action<TMessage>> snapshot;
        lock (_gate)
            snapshot = [.. list];

        foreach (var sub in snapshot)
        {
            sub.Invoke(message);
        }
    }

    public IDisposable Subscribe(Action<TMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_gate)
            list.Add(handler);

        return Disposable.Create(() =>
        {
            lock (_gate)
                list.Remove(handler);
        });
    }
}

public static class MessageBusExtensions
{
    extension(MessageBus self)
    {
        public IDisposable SubscribeOnce<TMessage>(Action<TMessage> handler, Predicate<TMessage>? predicate = null)
        {
            IDisposable? disposable = null;
            disposable = self.Subscribe<TMessage>(message =>
            {
                if (predicate is null || predicate(message))
                {
                    disposable?.Dispose();
                    handler(message);
                }
            });
            return disposable;
        }
    }

    extension(IDisposable self)
    {
        public IDisposable DisposeWithMessage<TMessage>(Predicate<TMessage>? predicate = null)
        {
            MessageBus.Instance.SubscribeOnce(_ =>
            {
                self.Dispose();
            }, predicate);
            return self;
        }
    }

    extension(IAsyncDisposable self)
    {
        public IAsyncDisposable DisposeWithMessage<TMessage>(Predicate<TMessage>? predicate = null)
        {
            MessageBus.Instance.SubscribeOnce(_ =>
            {
                self.DisposeAsync();
            }, predicate);
            return self;
        }
    }
}

