# RxMvvmLight

> 基于 [R3](https://github.com/Cysharp/R3) 的轻量、AOT 友好的 MVVM 基础库

[![NuGet](https://img.shields.io/nuget/v/RxMvvmLight.svg)](https://www.nuget.org/packages/RxMvvmLight)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE.txt)

[English](README.en.md) | **中文**

---

## 简介

RxMvvmLight 是一个面向 .NET 10 的轻量级 MVVM 基础库，以 [R3](https://github.com/Cysharp/R3) 作为响应式底座。它只提供构建 ViewModel 真正需要的那些基础能力，并针对 Native AOT / 裁剪（trimming）做了设计，尽量不依赖反射。

**核心特性**

- **ObservableObject** —— 实现 `INotifyPropertyChanged` / `INotifyPropertyChanging` 的基类，配合 `SetProperty` 一行属性即可。
- **ObserveChanged / ObserveChanging** —— 把属性变化暴露为 R3 `Observable`，支持多属性元组组合；属性名通过 `CallerArgumentExpression` 编译期捕获，无反射，AOT 安全。
- **RxCommand** —— 实现 `ICommand` 的命令类型，原生支持异步、`IsRunning` 状态流与 `Observable<bool>` 可执行条件。
- **Interaction** —— 异步的请求 / 响应（对话框、确认框等），带 handler 栈与中间件链。
- **MessageBus** —— 按消息类型分发的松耦合发布 / 订阅。
- **ViewModelLifecycle** —— 初始化 / 激活 / 失活 / 销毁的生命周期状态机，信号串行执行，天然配合释放订阅。
- **Validation** —— 流式验证构建器，支持同步 / 异步规则、防抖、条件规则、依赖刷新、级联模式，并实现 `INotifyDataErrorInfo`。

## 环境要求

- .NET 10（`net10.0`）
- [R3](https://www.nuget.org/packages/R3) 1.3.1 或更高
- 兼容 Native AOT 与裁剪（库标注了 `IsAotCompatible` / `IsTrimmable`）

## 安装

```bash
dotnet add package RxMvvmLight
```

## 快速上手

```csharp
using RxMvvmLight;

public partial class MainViewModel : ObservableObject
{
    // 使用 C# 的 field 关键字，无需显式声明字段
    public string Greeting { get => field; set => this.SetProperty(ref field, value); } = "Hello";

    public RxCommand SayHello { get; }

    public MainViewModel()
    {
        SayHello = RxCommand.Create(() => Greeting = "Hello, RxMvvmLight!");
    }
}
```

## ObservableObject

`ObservableObject` 实现了 `INotifyPropertyChanged` 与 `INotifyPropertyChanging`。`SetProperty` 仅在值真正发生变化时，按 `PropertyChanging` → 赋值 → `PropertyChanged` 的顺序触发通知，并返回是否发生改变。

```csharp
public partial class PersonViewModel : ObservableObject
{
    public string Name { get => field; set => this.SetProperty(ref field, value); } = "";
    public int Age { get => field; set => this.SetProperty(ref field, value); }
}
```

## ObserveChanged / ObserveChanging

把属性变化转换为 R3 的 `Observable<T>`，无需手写属性名。

```csharp
// 单个属性：订阅时立即推送当前值，之后每次变化推送新值
this.ObserveChanged(x => x.Name)
    .Subscribe(name => Console.WriteLine(name));

// 多个属性（元组）：任一属性变化都会推送组合值
this.ObserveChanged(x => (x.Name, x.Email))
    .Subscribe(t => Console.WriteLine($"{t.Name} / {t.Email}"));

// 变化前（拿到的是旧值）
this.ObserveChanging(x => x.Name)
    .Subscribe(oldName => Console.WriteLine(oldName));

// 也可以直接使用事件流
this.Changed.Subscribe(e => Console.WriteLine(e.PropertyName));
this.Changing.Subscribe(e => Console.WriteLine(e.PropertyName));
```

属性名通过 `CallerArgumentExpression` 在编译期捕获，再由 `PropertyNameHelper` 解析，不依赖反射，因此对 AOT 友好。支持链式属性（取第一段）、元组与可空标记（`x?.Name`）。

## RxCommand

`RxCommand` 实现了 `ICommand`，原生支持异步执行。执行期间 `IsRunning` 为 `true`；`CanExecute` 由 `IsRunning` 与可选的 `Observable<bool>` 组合而成。

```csharp
// 无参命令
public RxCommand Refresh { get; } = RxCommand.Create(() => { /* ... */ });

// 异步命令
public RxCommand Save { get; } = RxCommand.Create(async () =>
{
    await Task.Delay(1000);
});

// 带参数命令
public RxCommand<string> Search { get; } = RxCommand.Create<string>(async keyword =>
{
    await Task.Delay(300);
});

// 用 Observable<bool> 控制可执行状态
private readonly Subject<bool> canSubmit = new();
public RxCommand Submit { get; }

public MyViewModel()
{
    Submit = RxCommand.Create(() => { /* ... */ }, canSubmit);
    canSubmit.OnNext(false); // 禁用
    canSubmit.OnNext(true);  // 启用
}
```

- 正在执行或 `canExecute` 为 `false` 时，`CanExecute` 返回 `false`，并触发 `CanExecuteChanged`。
- 可订阅 `IsRunning` 显示 loading 状态。
- `RxCommand` 实现了 `IDisposable`，不再使用时调用 `Dispose()`。

```csharp
Save.IsRunning.Subscribe(running => IsBusy = running);
```

## Interaction

`Interaction<TIn, TOut>` 用于 ViewModel 向 View 发起异步请求（确认框、选择框等），实现 UI 与逻辑解耦。

```csharp
public class AboutViewModel
{
    public Interaction<string, bool> Confirm { get; } = new();

    public RxCommand AskConfirm { get; }

    public AboutViewModel()
    {
        AskConfirm = RxCommand.Create(async () =>
        {
            var ok = await Confirm.Handle("确认继续这个操作？");
            // 使用 ok ...
        });
    }
}
```

在 View 层注册 handler（返回的 `IDisposable` 可用于注销）：

```csharp
Confirm.RegisterHandler(message => ShowDialog(message));
```

后注册的 handler 位于栈顶，`Handle` 调用栈顶 handler。使用 `HandlerWithPrevious` 可以像中间件一样包裹前一个 handler：

```csharp
Confirm.RegisterHandler(async (message, previous) =>
{
    Log("向用户发起询问");
    var result = await previous(message); // 调用栈中的上一个 handler
    Log($"用户结果：{result}");
    return result;
});
```

## MessageBus

按消息类型分发的全局发布 / 订阅总线。

```csharp
// 发布
MessageBus.Instance.Publish(new UserLoggedIn("kongdetuo"));

// 订阅
var sub = MessageBus.Instance.Subscribe<UserLoggedIn>(e => Console.WriteLine(e.Name));

// 只接收一次
MessageBus.Instance.SubscribeOnce<UserLoggedIn>(e => { /* ... */ });

// 带条件的一次性订阅
MessageBus.Instance.SubscribeOnce<UserLoggedIn>(e => { /* ... */ }, e => e.Name == "kongdetuo");

// 让某个 IDisposable 在收到消息时自动释放
someSubscription.DisposeWithMessage<UserLoggedIn>();
```

## ViewModelLifecycle

`ViewModelLifecycle` 提供 `Created → Initialized → Activated ⇄ Initialized → Destroyed` 的状态机，并对每个阶段暴露一个异步信号 `IAsyncSignal`。

```csharp
public partial class HomeViewModel : ObservableObject, IHasLifecycle
{
    public ViewModelLifecycle Lifecycle => field ??= new();

    public int Ticks { get => field; set => this.SetProperty(ref field, value); }

    public HomeViewModel()
    {
        this.WhenInitialize(() => { /* 首次进入时加载数据 */ });
        this.WhenActivate(() => { /* 每次进入页面 */ });
        this.WhenDeactivate(() => { /* 离开页面 */ });

        // 订阅随失活自动释放
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Subscribe(_ => Ticks++)
            .DisposeWithDeactivate(Lifecycle);
    }
}
```

```csharp
// 触发时机
await vm.Lifecycle.ActivateAsync();   // 必要时自动 Initialize，然后 Activate
await vm.Lifecycle.DeactivateAsync();
await vm.Lifecycle.DestroyAsync();    // 必要时先 Deactivate

// 可观察当前状态
LifecycleState state = vm.Lifecycle.State;
```

- `ActivateAsync(ensureInitialized: true)` 会在需要时自动完成初始化。
- `Initialize` / `Activate` 等信号按语义只触发一次或每次触发一次，重复调用会去重。
- 状态只有在所有 handler 执行完成后才推进；并发调用会按调用顺序串行化。
- `WhenInitialize / WhenActivate / WhenDeactivate / WhenDestroy` 返回 `IDisposable`，可取消订阅。
- `DisposeWith` / `DisposeWithDeactivate` / `DisposeWithDestroy` 可把任意 `IDisposable` / `IAsyncDisposable` 挂接到相应信号上自动释放。

View 侧挂接（示例中的 `ActivatableView`）：

```csharp
public abstract class ActivatableView : UserControl, IActivatableView
{
    private readonly AsyncSignal _activated = new();
    private readonly AsyncSignal _deactivated = new();

    // 显式实现，避免与平台自带的 Activated / Deactivated 事件撞名
    IAsyncSignal IActivatableView.Activated => _activated;
    IAsyncSignal IActivatableView.Deactivated => _deactivated;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _ = ActivateAsync();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _ = DeactivateAsync();
    }

    private async Task ActivateAsync()
    {
        if (DataContext is IHasLifecycle vm)
            await vm.Lifecycle.ActivateAsync();
        await _activated.InvokeAsync();
    }

    private async Task DeactivateAsync()
    {
        await _deactivated.InvokeAsync();
        if (DataContext is IHasLifecycle vm)
            await vm.Lifecycle.DeactivateAsync();
    }
}
```

`IActivatableView` 让 View 也拥有自己的激活/失活时机，用法与 VM 一致：

```csharp
this.WhenActivated(() => { /* 每次进入页面 */ });
this.WhenActivated(() => { /* 订阅后的下一次激活，执行一次后自动退订 */ }, once: true);
this.WhenDeactivated(() => { /* 每次离开页面 */ });

someSubscription.DisposeWithDeactivate(this);
```

`WhenActivated` / `WhenDeactivated` 只提供同步 `Action` 重载；`once: true` 表示「订阅后的下一次激活执行一次，然后自动退订」，等价于内建的 `SubscribeOnce`，并不代表生命周期意义上的「第一次激活」。`WhenDeactivated` 没有 `once` 参数。

示例里激活时先推进 VM 生命周期再触发 `Activated`，失活时先触发 `Deactivated` 再失活 VM，形成嵌套的清理顺序。

## Validation

验证模块以 `ValidationObservableObject` 为入口。它继承自 `ObservableObject`，并实现 `INotifyDataErrorInfo`，可直接与 WPF / Avalonia 的绑定验证集成。

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
        // 同步规则，实时验证
        this.RuleFor(x => x.Name)
            .Required("姓名不能为空")
            .MinLength(3, "长度不能少于{0}个字符")
            .Subscribe();

        // 条件规则：有值时才验证
        this.RuleFor(x => x.Nickname)
            .When(n => n?.Length > 0)
            .Must(n => n?.Length <= 12, () => $"昵称最多12个字符，当前 {Nickname?.Length ?? 0}")
            .Subscribe();

        // 条件防抖：输入内容后开始 300ms 防抖
        this.RuleFor(x => x.Email)
            .When(e => e?.Length > 0)
            .Debounce(300)
            .Email("邮箱格式不正确")
            .Subscribe();

        // 异步查重
        this.RuleFor(x => x.Phone)
            .When(p => p?.Length > 0)
            .Matches(@"^1[3-9]\d{9}$", "手机号格式不正确")
            .Debounce(300)
            .MustAsync(async (phone, ct) =>
            {
                await Task.Delay(500, ct); // 模拟远程查重
                return phone != "13800000000";
            }, "该手机号已被注册")
            .Subscribe();

        // 所有验证通过后提交按钮才可用
        Submit = RxCommand.Create(() => { /* ... */ }, Validator.IsValid);
    }
}
```

> `RuleFor(...)` 之后必须调用 `.Subscribe()`，验证管线才会真正启用。

### 可见错误行为

`ValidationObservableObject` 在第一个受验证属性发生变化之前不会对外报告错误（`HasErrors` 为 `false`、`GetErrors` 返回空）。这样在用户尚未编辑时不会立刻显示红色错误。

### ReactiveValidator

每个 `ValidationObservableObject` 都拥有一个 `ReactiveValidator`，也可以单独使用。

```csharp
Validator.IsValid.Subscribe(valid => { });              // 是否全部通过
Validator.IsValidating.Subscribe(v => { });             // 是否有属性正在验证

Validator.ObserveState(nameof(Email)).Subscribe(state => { });      // ValidatorState
Validator.ObserveValidating(nameof(Phone)).Subscribe(v => IsPhoneChecking = v);
Validator.ObserveValid(nameof(Phone)).Subscribe(v => { });

Validator.GetInvalidProperties();    // 当前处于 Invalid 的属性名
Validator.ContainsProperty("Name");  // 是否注册了该属性
Validator.RefreshMessage();          // 重新生成错误消息（如切换语言后）
ReactiveValidator.RefreshAllMessage(); // 刷新所有实例
```

`ValidatorState` 取值：`NotValidated`、`Validating`、`Valid`、`Invalid`。

### 规则与构建器

`PropertyValidatorBuilder<T>` 常用方法：

| 方法 | 说明 |
| --- | --- |
| `Must(predicate, message)` | 同步谓词规则 |
| `MustAsync(async predicate, message)` | 异步规则，支持 `CancellationToken` |
| `Not(predicate, message)` / `In(values, message)` | 取反 / 属于集合 |
| `GreaterThan` / `GreaterThanOrEqual` / `LessThan` / `LessThanOrEqual` / `Between` | 基于 `IComparable<T>` 的区间比较 |
| `When(condition)` | 条件：为真时才执行后续规则 |
| `Debounce(TimeSpan / uint)` | 防抖（按毫秒） |
| `DependsOn(observable)` | 其它流变化时重新验证 |
| `CascadeMode(Stop / Continue)` | 级联模式，默认 `Stop`（首个失败即停止） |
| `RegisterRule` / `RegisterAsyncRule` | 注册自定义规则 |

字符串专用规则：`Required`、`NotBlank`、`MinLength`、`MaxLength`、`Length`、`Matches(string | Regex)`、`Email`、`Url`。

异步验证遇到新输入会自动取消进行中的旧任务（`AwaitOperation.Switch`），避免过期结果覆盖最新状态；规则抛出异常时该属性会验证失败，但管线保持存活。

### MessageConverter（消息本地化）

错误消息可以是 `string`、`Func<string>` 或自定义类型。内置消息类型 `BuiltInMessage` 默认提供中文文案，可通过 `MessageConverter` 注册替换：

```csharp
// 覆盖内置消息
MessageConverter.Register<BuiltInMessage>(m => Localizer[m]);

// 注册自定义消息类型
MessageConverter.Register<IObservable<string>>(m => m.CurrentValue);
```

`BuiltInMessage` 包含：`Required`、`TextNotBlank`、`TextMinLength`、`TextMaxLength`、`TextLength`、`InvalidFormat`、`InvalidEmailFormat`、`InvalidUrlFormat`。

## AOT / 裁剪

- 库标注了 `IsAotCompatible` 与 `IsTrimmable`。
- 属性名通过 `CallerArgumentExpression` 编译期捕获并做字符串解析，不使用反射。
- 消息总线、验证等均基于泛型静态类型分发，便于裁剪器静态分析。

## 构建与测试

```bash
# 构建整个解决方案目录
dotnet build ./src/

# 运行单元测试
dotnet test ./src/RxMvvmLight.Tests

# 运行 Avalonia 示例
dotnet run --project ./src/Demo
```

## 许可证

基于 [MIT 许可证](./LICENSE.txt) 发布，Copyright (c) 2026 kongdetuo。
