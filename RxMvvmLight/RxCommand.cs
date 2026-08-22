using R3;
using System.Windows.Input;

namespace RxMvvmLight;

public abstract class RxCommand : ICommand, IDisposable
{
    public abstract event EventHandler? CanExecuteChanged;

    public abstract Observable<bool> IsRunning { get; }

    public abstract bool CanExecute(object? parameter);

    public abstract void Execute(object? parameter);

    public abstract Task ExecuteAsync(object? parameter);

    public abstract void Dispose();

    public static RxCommand Create(Action action) =>
        new RxCommand<object>(_ => { action(); return Task.CompletedTask; }, Observable.Return(true));

    public static RxCommand Create(Action action, Observable<bool> canExecuteObservable) =>
        new RxCommand<object>(_ => { action(); return Task.CompletedTask; }, canExecuteObservable);

    public static RxCommand Create(Func<Task> action) =>
        new RxCommand<object>(_ => action(), Observable.Return(true));

    public static RxCommand Create(Func<Task> action, Observable<bool> canExecuteObservable) =>
        new RxCommand<object>(_ => action(), canExecuteObservable);

    public static RxCommand<T> Create<T>(Action<T> action) =>
        new RxCommand<T>(obj => { action(obj); return Task.CompletedTask; }, Observable.Return(true));

    public static RxCommand<T> Create<T>(Action<T> action, Observable<bool> canExecuteObservable) =>
        new RxCommand<T>(obj => { action(obj); return Task.CompletedTask; }, canExecuteObservable);

    public static RxCommand<T> Create<T>(Func<T, Task> action) =>
        new RxCommand<T>(action, Observable.Return(true));

    public static RxCommand<T> Create<T>(Func<T, Task> action, Observable<bool> canExecuteObservable) =>
        new RxCommand<T>(action, canExecuteObservable);
}

public class RxCommand<T> : RxCommand
{
    private readonly Func<T, Task> execute;
    private readonly IDisposable dis;
    private bool canExecute;
    private readonly Subject<bool> runningSubject = new();

    internal RxCommand(Func<T, Task> execute, Observable<bool> canExecuteObservable)
    {
        this.execute = execute;
        this.dis = Observable.CombineLatest([IsRunning.Prepend(false), canExecuteObservable.Prepend(true)])
            //.ObserveOnCurrentSynchronizationContext()
            .Subscribe(p =>
            {
                var can = !p[0] && p[1];
                if (can != this.canExecute)
                {
                    this.canExecute = can;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            });
    }

    public override Observable<bool> IsRunning => runningSubject;

    public override event EventHandler? CanExecuteChanged;

    public override bool CanExecute(object? parameter) => canExecute;

    public override async void Execute(object? parameter) => await ExecuteAsync(parameter);

    public override async Task ExecuteAsync(object? parameter)
    {
        if (!canExecute)
            return;

        T param;
        if (parameter is T typed)
        {
            param = typed;
        }
        else if (parameter is null && default(T) is null)
        {
            param = default(T)!;
        }
        else
        {
            return;
        }

        try
        {
            runningSubject.OnNext(true);
            await execute(param);
        }
        finally
        {
            runningSubject.OnNext(false);
        }
    }

    public override void Dispose()
    {
        dis.Dispose();
        runningSubject.OnCompleted();
    }
}