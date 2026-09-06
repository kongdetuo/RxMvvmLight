using R3;

namespace RxMvvmLight;

public interface IActivatable
{
    ViewModelActivator Activator { get; }
}

public sealed class ViewModelActivator : IDisposable
{
    private readonly BehaviorSubject<bool> activeSubject = new(false);
    private readonly Subject<Unit> firstActivatedSubject = new();
    private readonly Lock gate = new();
    private bool disposed;
    private bool active;
    private bool firstActivated;

    public Observable<bool> IsActive => activeSubject;

    public Observable<Unit> FirstActivated => firstActivatedSubject;

    public void Activate() => SetActive(true);

    public void Deactivate() => SetActive(false);

    public void SetActive(bool value)
    {
        bool first;
        lock (gate)
        {
            if (disposed || active == value)
                return;
            active = value;
            first = value && !firstActivated;
            if (first)
                firstActivated = true;
        }

        if (first)
        {
            firstActivatedSubject.OnNext(Unit.Default);
            firstActivatedSubject.OnCompleted();
        }

        activeSubject.OnNext(value);
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
        }

        firstActivatedSubject.OnCompleted();
        activeSubject.OnCompleted();
        firstActivatedSubject.Dispose();
        activeSubject.Dispose();
    }
}

public static class ActivatableExtensions
{
    public static IDisposable WhenActivated(this IActivatable vm, Action onActivated) =>
        vm.Activator.IsActive
            .DistinctUntilChanged()
            .Where(x => x)
            .Subscribe(_ => onActivated());

    public static IDisposable WhenActivated(this IActivatable vm, Func<CancellationToken, Task> onActivated) =>
        vm.Activator.IsActive
            .DistinctUntilChanged()
            .Where(x => x)
            .SubscribeAwait(async (_, ct) => await onActivated(ct), AwaitOperation.Switch);

    public static IDisposable WhenFirstActivated(this IActivatable vm, Action onActivated) =>
        vm.Activator.FirstActivated
            .Subscribe(_ => onActivated());

    public static IDisposable WhenFirstActivated(this IActivatable vm, Func<CancellationToken, Task> onActivated) =>
        vm.Activator.FirstActivated
            .SubscribeAwait(async (_, ct) => await onActivated(ct), AwaitOperation.Switch);

    public static IDisposable WhenDeactivated(this IActivatable vm, Action onDeactivated) =>
        vm.Activator.IsActive
            .DistinctUntilChanged()
            .Skip(1)
            .Where(x => !x)
            .Subscribe(_ => onDeactivated());

    public static IDisposable WhenDeactivated(this IActivatable vm, Func<CancellationToken, Task> onDeactivated) =>
        vm.Activator.IsActive
            .DistinctUntilChanged()
            .Skip(1)
            .Where(x => !x)
            .SubscribeAwait(async (_, ct) => await onDeactivated(ct), AwaitOperation.Switch);

    public static IDisposable DisposeWith(this IDisposable target, ViewModelActivator activator) =>
        activator.IsActive.Subscribe(_ => { }, _ => { }, _ => target.Dispose());

    public static IDisposable DisposeWhenDeactivated(this IDisposable target, ViewModelActivator activator)
    {
        bool beenActive = false;
        IDisposable? link = null;
        link = activator.IsActive.Subscribe(
            active =>
            {
                if (active)
                {
                    beenActive = true;
                }
                else if (beenActive)
                {
                    target.Dispose();
                    link?.Dispose();
                }
            },
            _ => { },
            _ => target.Dispose());
        return link;
    }
}