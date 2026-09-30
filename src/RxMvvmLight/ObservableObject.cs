using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RxMvvmLight;


public class ObservableObject : INotifyPropertyChanging, INotifyPropertyChanged
{
    PropertyChangingEventHandler? propertyChangingEventHandler;

    PropertyChangedEventHandler? propertyChangedEventHandler;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => propertyChangedEventHandler += value;
        remove => propertyChangedEventHandler -= value;
    }

    public event PropertyChangingEventHandler? PropertyChanging
    {
        add => propertyChangingEventHandler += value;
        remove => propertyChangingEventHandler -= value;
    }

    protected void OnPropertyChanging([CallerMemberName] string propertyName = "")
    {
        propertyChangingEventHandler?.Invoke(this, new(propertyName));
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        propertyChangedEventHandler?.Invoke(this, new(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            OnPropertyChanging(propertyName);
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        return false;
    }
}
