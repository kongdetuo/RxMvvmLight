using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using R3;

namespace RxMvvmLight;

public record PropertyValue(string PropertyName, object? Value);

public interface IObservableObject
{
    Observable<PropertyValue> Changing { get; }
    Observable<PropertyValue> Changed { get; }
    object? GetPropertyValue(string name);
}

public class ObservableObject : IObservableObject, INotifyPropertyChanging, INotifyPropertyChanged
{
    private readonly Subject<PropertyValue> changingSubject = new();
    private readonly Subject<PropertyValue> changedSubject = new();

    PropertyChangingEventHandler? propertyChangingEventHandler;

    PropertyChangedEventHandler? propertyChangedEventHandler;

    public Observable<PropertyValue> Changing => changingSubject;

    public Observable<PropertyValue> Changed => changedSubject;

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add
        {
            propertyChangedEventHandler += value;
        }

        remove
        {
            propertyChangedEventHandler -= value;
        }
    }

    event PropertyChangingEventHandler? INotifyPropertyChanging.PropertyChanging
    {
        add
        {
            propertyChangingEventHandler += value;
        }

        remove
        {
            propertyChangingEventHandler -= value;
        }
    }

    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> propertyCache = new();

    public virtual object? GetPropertyValue(string name)
    {
        var properties = propertyCache.GetOrAdd(GetType(), t => t.GetProperties()
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, StringComparer.Ordinal));
        return properties.TryGetValue(name, out var property) ? property.GetValue(this) : null;
    }

    protected void OnPropertyChanging<T>(T value, [CallerArgumentExpression(nameof(value))] string propertyName = "")
    {
        propertyName = propertyName[(propertyName.IndexOf('.') + 1)..].Trim();

        changingSubject?.OnNext(new(propertyName, value));
        propertyChangingEventHandler?.Invoke(this, new(propertyName));
    }

    protected void OnPropertyChanged<T>(T value, [CallerArgumentExpression(nameof(value))] string propertyName = "")
    {
        propertyName = propertyName[(propertyName.IndexOf('.') + 1)..].Trim();

        changedSubject?.OnNext(new(propertyName, value));
        propertyChangedEventHandler?.Invoke(this, new(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            OnPropertyChanging(field, propertyName);
            field = value;
            OnPropertyChanged(value, propertyName);
            return true;
        }
        return false;
    }
}
