namespace RxMvvmLight.Validation.Helpers;

internal class WeakReferenceList<T>
    where T:class
{
    private readonly List<WeakReference<T>> list = [];

    public List<T> GetLiveItems()
    {
        var result = list.Select(p => p.TryGetTarget(out var target) ? target : null!).Where(p => p is not null).ToList();
        list.RemoveAll(p => !p.TryGetTarget(out var _));
        return result;
    }

    public void Add(T item)
    {
        list.RemoveAll(p => !p.TryGetTarget(out var _));
        list.Add(new WeakReference<T>(item));
    }
}
