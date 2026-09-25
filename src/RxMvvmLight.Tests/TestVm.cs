using RxMvvmLight.Validation;

namespace RxMvvmLight.Tests;

internal sealed class TestVm : ValidationObservableObject
{
    private string name = "";
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    private string email = "";
    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    private string age = "";
    public string Age
    {
        get => age;
        set => SetProperty(ref age, value);
    }

    private SubVm foo = new();
    public SubVm Foo
    {
        get => foo;
        set => SetProperty(ref foo, value);
    }
}

internal sealed class SubVm : ObservableObject
{
    private string title = "";
    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }
}

internal sealed class LifecycleVm : ObservableObject, IHasLifecycle
{
    public ViewModelLifecycle Lifecycle => field ??= new();
}

internal sealed class LifecycleValidationVm : ValidationObservableObject, IHasLifecycle
{
    public ViewModelLifecycle Lifecycle => field ??= new();
}