# RxMvvmLight

> A lightweight, AOT-friendly MVVM foundation built on [R3](https://github.com/Cysharp/R3)

[![NuGet](https://img.shields.io/nuget/v/RxMvvmLight.svg)](https://www.nuget.org/packages/RxMvvmLight)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE.txt)

**English** | [中文](README.md)

---

## Introduction

RxMvvmLight is a lightweight MVVM foundation for .NET 10, built on top of [R3](https://github.com/Cysharp/R3). It ships only the building blocks a ViewModel actually needs, and is designed for Native AOT / trimming with a reflection-free approach.

**Highlights**

- **ObservableObject** — an `INotifyPropertyChanged` / `INotifyPropertyChanging` base class with a one-line `SetProperty`.
- **ObserveChanged / ObserveChanging** — expose property changes as R3 `Observable`s, including multi-property tuples; property names are captured at compile time via `CallerArgumentExpression` (no reflection, AOT-safe).
- **RxCommand** — an `ICommand` implementation with first-class async support, an `IsRunning` stream, and an `Observable<bool>` can-execute condition.
- **Interaction** — async request/response (dialogs, confirmations) with a handler stack and middleware chain.
- **MessageBus** — loosely-coupled, type-keyed publish/subscribe.
- **ViewModelLifecycle** — an initialize/activate/deactivate/destroy state machine whose signals run serially and pair naturally with subscription disposal.
- **Validation** — a fluent rule builder supporting sync/async rules, debounce, conditional rules, dependency refresh and cascade mode, plus `INotifyDataErrorInfo`.

## Requirements

- .NET 10 (`net10.0`)
- [R3](https://www.nuget.org/packages/R3) 1.3.1 or later
- Native AOT / trimming compatible (`IsAotCompatible` / `IsTrimmable`)

## Installation

```bash
dotnet add package RxMvvmLight
```

## Quick Start

```csharp
using RxMvvmLight;

public partial class MainViewModel : ObservableObject
{
    // Uses the C# `field` keyword; no explicit backing field needed
    public string Greeting { get => field; set => this.SetProperty(ref field, value); } = "Hello";

    public RxCommand SayHello { get; }

    public MainViewModel()
    {
        SayHello = RxCommand.Create(() => Greeting = "Hello, RxMvvmLight!");
    }
}
```

## ObservableObject

`ObservableObject` implements both `INotifyPropertyChanged` and `INotifyPropertyChanging`. `SetProperty` only raises notifications when the value actually changes, in the order `PropertyChanging` → assignment → `PropertyChanged`, and returns whether it changed.

```csharp
public partial class PersonViewModel : ObservableObject
{
    public string Name { get => field; set => this.SetProperty(ref field, value); } = "";
    public int Age { get => field; set => this.SetProperty(ref field, value); }
}
```

## ObserveChanged / ObserveChanging

Turn property changes into R3 `Observable<T>` without writing property names by hand.

```csharp
// Single property: replays the current value on subscribe, then pushes on change
this.ObserveChanged(x => x.Name)
    .Subscribe(name => Console.WriteLine(name));

// Multiple properties (tuple): pushes the combined value when any changes
this.ObserveChanged(x => (x.Name, x.Email))
    .Subscribe(t => Console.WriteLine($"{t.Name} / {t.Email}"));

// Before change (you receive the old value)
this.ObserveChanging(x => x.Name)
    .Subscribe(oldName => Console.WriteLine(oldName));

// Raw event streams are also available
this.Changed.Subscribe(e => Console.WriteLine(e.PropertyName));
this.Changing.Subscribe(e => Console.WriteLine(e.PropertyName));
```

Property names are captured at compile time via `CallerArgumentExpression` and parsed by `PropertyNameHelper`, so no reflection is involved — AOT-friendly. Chained properties (first segment), tuples, and null-conditional markers (`x?.Name`) are supported.

## RxCommand

`RxCommand` implements `ICommand` with native async support. While executing, `IsRunning` is `true`; `CanExecute` is a combination of `IsRunning` and an optional `Observable<bool>`.

```csharp
// Parameterless
public RxCommand Refresh { get; } = RxCommand.Create(() => { /* ... */ });

// Async
public RxCommand Save { get; } = RxCommand.Create(async () =>
{
    await Task.Delay(1000);
});

// With parameter
public RxCommand<string> Search { get; } = RxCommand.Create<string>(async keyword =>
{
    await Task.Delay(300);
});

// Drive can-execute with an Observable<bool>
private readonly Subject<bool> canSubmit = new();
public RxCommand Submit { get; }

public MyViewModel()
{
    Submit = RxCommand.Create(() => { /* ... */ }, canSubmit);
    canSubmit.OnNext(false); // disable
    canSubmit.OnNext(true);  // enable
}
```

- While running, or when `canExecute` is `false`, `CanExecute` returns `false` and `CanExecuteChanged` is raised.
- Subscribe to `IsRunning` to drive a busy indicator.
- `RxCommand` is `IDisposable`; call `Dispose()` when done.

```csharp
Save.IsRunning.Subscribe(running => IsBusy = running);
```

## Interaction

`Interaction<TIn, TOut>` lets a ViewModel make async requests to the View (confirmations, pickers, ...), decoupling UI from logic.

```csharp
public class AboutViewModel
{
    public Interaction<string, bool> Confirm { get; } = new();

    public RxCommand AskConfirm { get; }

    public AboutViewModel()
    {
        AskConfirm = RxCommand.Create(async () =>
        {
            var ok = await Confirm.Handle("Continue this operation?");
            // use ok ...
        });
    }
}
```

Register a handler in the View (the returned `IDisposable` unregisters it):

```csharp
Confirm.RegisterHandler(message => ShowDialog(message));
```

The most recently registered handler is on top of the stack, and `Handle` invokes it. Use `HandlerWithPrevious` to wrap the previous handler like middleware:

```csharp
Confirm.RegisterHandler(async (message, previous) =>
{
    Log("Asking the user");
    var result = await previous(message); // invoke the previous handler in the stack
    Log($"User answered: {result}");
    return result;
});
```

## MessageBus

A global, type-keyed publish/subscribe bus.

```csharp
// Publish
MessageBus.Instance.Publish(new UserLoggedIn("kongdetuo"));

// Subscribe
var sub = MessageBus.Instance.Subscribe<UserLoggedIn>(e => Console.WriteLine(e.Name));

// Receive once
MessageBus.Instance.SubscribeOnce<UserLoggedIn>(e => { /* ... */ });

// One-shot with a predicate
MessageBus.Instance.SubscribeOnce<UserLoggedIn>(e => { /* ... */ }, e => e.Name == "kongdetuo");

// Auto-dispose an IDisposable when a message arrives
someSubscription.DisposeWithMessage<UserLoggedIn>();
```

## ViewModelLifecycle

`ViewModelLifecycle` provides the `Created → Initialized → Activated ⇄ Initialized → Destroyed` state machine and exposes an async signal (`IAsyncSignal`) per phase.

```csharp
public partial class HomeViewModel : ObservableObject, IHasLifecycle
{
    public ViewModelLifecycle Lifecycle => field ??= new();

    public int Ticks { get => field; set => this.SetProperty(ref field, value); }

    public HomeViewModel()
    {
        this.WhenInitialize(() => { /* load data on first entry */ });
        this.WhenActivate(() => { /* each time the page is entered */ });
        this.WhenDeactivate(() => { /* when leaving the page */ });

        // Subscription disposed automatically on deactivate
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Subscribe(_ => Ticks++)
            .DisposeWithDeactivate(Lifecycle);
    }
}
```

```csharp
// Transitions
await vm.Lifecycle.ActivateAsync();   // initializes if needed, then activates
await vm.Lifecycle.DeactivateAsync();
await vm.Lifecycle.DestroyAsync();    // deactivates first if needed

// Observe current state
LifecycleState state = vm.Lifecycle.State;
```

- `ActivateAsync(ensureInitialized: true)` performs initialization when required.
- `Initialize` / `Activate` signals fire once, or once per occurrence depending on semantics; repeated calls are de-duplicated.
- State advances only after all handlers complete; concurrent calls are serialized in call order.
- `WhenInitialize / WhenActivate / WhenDeactivate / WhenDestroy` return `IDisposable` so subscriptions can be cancelled.
- `DisposeWith` / `DisposeWithDeactivate` / `DisposeWithDestroy` attach any `IDisposable` / `IAsyncDisposable` to the matching signal for automatic disposal.

View integration (the demo's `ActivatableView`):

```csharp
protected override void OnLoaded(RoutedEventArgs e)
{
    base.OnLoaded(e);
    if (DataContext is IHasLifecycle vm)
        _ = vm.Lifecycle.ActivateAsync();
}

protected override void OnUnloaded(RoutedEventArgs e)
{
    base.OnUnloaded(e);
    if (DataContext is IHasLifecycle vm)
        _ = vm.Lifecycle.DeactivateAsync();
}
```

## Validation

The validation entry point is `ValidationObservableObject`. It derives from `ObservableObject` and implements `INotifyDataErrorInfo`, so it integrates directly with WPF / Avalonia binding validation.

```csharp
using RxMvvmLight.Validation;

public partial class ValidationViewModel : ValidationObservableObject, IHasLifecycle
{
    public ViewModelLifecycle Lifecycle { get; } = new();

    public string Name { get => field; set => this.SetProperty(ref field, value); } = "";
    public string? Nickname { get => field; set => this.SetProperty(ref field, value); }
    public string Email { get => field; set => this.SetProperty(ref field, value); } = "";
    public string Phone { get => field; set => this.SetProperty(ref field, value); } = "";

    public RxCommand Submit { get; }

    public ValidationViewModel()
    {
        // Sync rules, validated in real time
        this.RuleFor(x => x.Name)
            .Required("Name is required")
            .MinLength(3, "Must be at least {0} characters")
            .Subscribe();

        // Conditional: only validate when there is a value
        this.RuleFor(x => x.Nickname)
            .When(n => n?.Length > 0)
            .Must(n => n?.Length <= 12, () => $"Nickname must be at most 12 characters, currently {Nickname?.Length ?? 0}")
            .Subscribe();

        // Conditional debounce: wait for input, then debounce for 300ms
        this.RuleFor(x => x.Email)
            .When(e => e?.Length > 0)
            .Debounce(300)
            .Email("Invalid email format")
            .Subscribe();

        // Async uniqueness check
        this.RuleFor(x => x.Phone)
            .When(p => p?.Length > 0)
            .Matches(@"^1[3-9]\d{9}$", "Invalid phone number format")
            .Debounce(300)
            .MustAsync(async (phone, ct) =>
            {
                await Task.Delay(500, ct); // simulate a remote lookup
                return phone != "13800000000";
            }, "This phone number is already registered")
            .Subscribe();

        // Submit is enabled only when everything is valid
        Submit = RxCommand.Create(() => { /* ... */ }, Validator.IsValid);
    }
}
```

> After `RuleFor(...)`, you must call `.Subscribe()` to actually start the validation pipeline.

### Visible-error behavior

`ValidationObservableObject` does not report errors until the first validated property changes (`HasErrors` is `false` and `GetErrors` returns empty). This avoids showing red errors before the user has edited anything.

### ReactiveValidator

Every `ValidationObservableObject` owns a `ReactiveValidator`, which can also be used standalone.

```csharp
Validator.IsValid.Subscribe(valid => { });              // all rules pass
Validator.IsValidating.Subscribe(v => { });             // any property is validating

Validator.ObserveState(nameof(Email)).Subscribe(state => { });      // ValidatorState
Validator.ObserveValidating(nameof(Phone)).Subscribe(v => IsPhoneChecking = v);
Validator.ObserveValid(nameof(Phone)).Subscribe(v => { });

Validator.GetInvalidProperties();     // properties currently in Invalid state
Validator.ContainsProperty("Name");   // whether a property is registered
Validator.RefreshMessage();           // regenerate error messages (e.g. after a language switch)
ReactiveValidator.RefreshAllMessage(); // refresh every instance
```

`ValidatorState` values: `NotValidated`, `Validating`, `Valid`, `Invalid`.

### Rules and builder

Common `PropertyValidatorBuilder<T>` methods:

| Method | Description |
| --- | --- |
| `Must(predicate, message)` | Synchronous predicate rule |
| `MustAsync(async predicate, message)` | Async rule, supports `CancellationToken` |
| `Not(predicate, message)` / `In(values, message)` | Negation / set membership |
| `GreaterThan` / `GreaterThanOrEqual` / `LessThan` / `LessThanOrEqual` / `Between` | Range checks based on `IComparable<T>` |
| `When(condition)` | Condition: subsequent rules run only when true |
| `Debounce(TimeSpan / uint)` | Debounce (by milliseconds) |
| `DependsOn(observable)` | Re-validate when another stream changes |
| `CascadeMode(Stop / Continue)` | Cascade mode, defaults to `Stop` (stop at first failure) |
| `RegisterRule` / `RegisterAsyncRule` | Register custom rules |

String-specific rules: `Required`, `NotBlank`, `MinLength`, `MaxLength`, `Length`, `Matches(string | Regex)`, `Email`, `Url`.

When new input arrives during async validation, the in-flight task is cancelled automatically (`AwaitOperation.Switch`), preventing stale results from overwriting the latest state. If a rule throws, the property fails validation but the pipeline stays alive.

### MessageConverter (message localization)

Messages can be a `string`, a `Func<string>`, or a custom type. The built-in `BuiltInMessage` type ships with default (Chinese) text and can be overridden via `MessageConverter`:

```csharp
// Override built-in messages
MessageConverter.Register<BuiltInMessage>(m => Localizer[m]);

// Register a custom message type
MessageConverter.Register<IObservable<string>>(m => m.CurrentValue);
```

`BuiltInMessage` values: `Required`, `TextNotBlank`, `TextMinLength`, `TextMaxLength`, `TextLength`, `InvalidFormat`, `InvalidEmailFormat`, `InvalidUrlFormat`.

## AOT / Trimming

- The library is marked `IsAotCompatible` and `IsTrimmable`.
- Property names are captured at compile time via `CallerArgumentExpression` and parsed as strings; no reflection is used.
- The message bus and validation dispatch through static generic types, which the trimmer can analyze statically.

## Build & Test

```bash
# Build the solution folder
dotnet build ./src/

# Run unit tests
dotnet test ./src/RxMvvmLight.Tests

# Run the Avalonia demo
dotnet run --project ./src/Demo
```

## License

Released under the [MIT License](./LICENSE.txt). Copyright (c) 2026 kongdetuo.
