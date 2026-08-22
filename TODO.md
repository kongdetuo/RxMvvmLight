# RxMvvmLight 待办

> 业余项目，攒灵感一起做。记录设计讨论中遗留的重要事项，按主题分组，灵感到了再动手。

## 已完成

- 链式观察 `ObserveChanged(x => x.X.Y.Z)`：初始回放、换分支回放、null 中间对象发 null。无表达式树、无源生成器（`ObservableObjectExtensions.cs`）
- `IObservableObject.GetPropertyValue(name)`：反射 + 每类型缓存，`virtual` 供生成器以后 override（`ObservableObject.cs`）
- `RxCommand` 修复：`CombineLatest(IsRunning.StartWith(false), canExecute.StartWith(true))` + 取反逻辑修正
- `RxCommand.ExecuteAsync` 修复：`parameter is T` 匹配不上 null，改为 `parameter is T typed || parameter is null && default(T) is null`（引用类型 / `Nullable<T>` 接受 null，纯值类型仍拒绝）
- 删除 expression 版 / 字符串版 `ObserveChanged`，API 收敛为 `GetObservable`（免 lambda）+ `ObserveChanged`（lambda 链）两条线
- **Activable**（`Activatable.cs`）：`ViewModelActivator`（`IsActive` BehaviorSubject + `FirstActivated` 独立流）+ `IActivatable` + 扩展 `WhenActivated` / `WhenFirstActivated` / `WhenDeactivated`（含 `CancellationToken` 异步版，Switch 取消）+ `DisposeWith` / `DisposeWhenDeactivated` 销毁时机扩展。VM 自行实现 `IActivatable` + 任一基类（`ObservableObject` / `ValidationObservableObject`）并带 `Activator` 属性
  - `FirstActivated` 在首次激活时先于 `IsActive` 推送 → `WhenFirstActivated` 恒先于 `WhenActivated`（与注册顺序无关），首次后晚注册不再触发
  - `ViewModelActivator.Dispose()` 完成两流 = 销毁信号；订阅失活不拆、跟随 VM 生命周期，`DisposeWhenDeactivated` 可选"首次失活即拆"
- **默认跳过首值 + `ForceValidate`**：Builder 默认 `source.Skip(1)`，初始值不参与验证对外不可见（但管线初始化仍置 `State=Unknown`，门控拦截）。提供 `ForceValidate()` 允许检查初始值。`ValidationState` 新增 `Validating` 枚举值：管线每次发射经 `BeginEvaluation` 置 `State=Validating`，评估结果落定后转 `Valid`/`Invalid`
- **验证管线：per-rule Debounce + 线性评估 + 实时错误展示**：`Debounce` 位置敏感，对后续规则生效（`DebounceRule<T>` 作为 `IRule<T>` 插入 flat rules list）。`Behavior(behavior)` 设置管线级默认策略（`FailFast` 顺序失败即停 / `CollectAll` 顺序全部执行）。管线用单个 `SelectAwait` + foreach 循环遍历 flat rules list：遇 `DebounceRule` → `await Task.Delay`，遇普通规则 → 调用求值。**每条规则失败后立即 emit 部分错误**（`SetState` + `SetErrors`），用户无需等待所有规则完成。`CollectAll` 模式下慢速异步规则不会阻塞即时错误的展示。取消控制完全依赖 `CancellationToken` + `AwaitOperation.Switch`，无 Generation 计数器
- **简化 API**：`Subscribe()` 返回 `void`（验证流水线跟随验证器生命周期，无需单独管理）；删除 `PropertyValidationRegistration<T>` 类；删除 `RuleGroup<T>` 嵌套结构
- **验证 API 重新设计**：核心 API 改为 `RegisterRule(Func<T, string[]>)` 和 `RegisterAsyncRule(Func<T, Task<string[]>>)` / `RegisterAsyncRule(Func<T, CancellationToken, Task<string[]>>)` 低级原语。`Must`/`MustAsync` 退化为扩展方法（语法糖），不再属于核心 API。删除 `Parallel` 分支
- **`RxCommand` 还原 `ObserveOnCurrentSynchronizationContext`**：这是 MVVM 库的应有立场——在存在 SynchronizationContext（UI 线程）环境下启动，跨线程 marshal 归队。曾因测试竞态误改；测试改为模拟 UI 同步上下文（`Post` 内联执行的 `InlineSynchronizationContext`）使交付确定性，不动库代码。临时背景：`Validator` 防抖导致验证结果在错误线程上发 `CanExecuteChanged`，靠此 marshal 归队 UI 线程
- **Interaction**（`Interaction.cs`）：`Interaction<TIn,TOut>` 请求-响应信箱。VM 侧 `Handle(input)` 返回 `Task<TOut>`，View 侧 `RegisterHandler(Func<TIn,TOut>)` / `RegisterHandler(Func<TIn,Task<TOut>>)` 返回 `IDisposable`（注销即退订）
  - 无 handler 时 `Handle` 抛 `InvalidOperationException`（ReactiveUI 是挂死，这里 fail-fast）；Dispose 后 `Handle` 抛 `ObjectDisposedException`；多 handler 是 **LIFO 栈**：只调用栈顶（最近注册）的 handler 应答，注销栈顶恢复前一个——结果永远确定
  - Demo：About 页按钮 → VM `Handle` → View 注册 handler 弹模态确认窗返回 bool
  - Demo：`ActivatableView` 基类接 `OnLoaded/OnUnloaded` → 自动 Activate/Deactivate；Home/About 页面切换演示反复激活

## 已知 bug

- `async void Execute` + `catch { throw; }`：异常直接冒到 UI 上下文崩应用，需异常透出通道
- `RxCommand.IsRunning` 对外部订阅者不回放当前状态（"状态流回放"契约差最后一条）

## 验证模块

- 完整实现：`RuleFor` + `RegisterRule`/`RegisterAsyncRule`（核心原语）+ `Must`/`MustAsync`（扩展语法糖）+ `Debounce` + `Behavior`（FailFast / CollectAll）+ `ForceValidate`。管线为单个 async foreach 循环，逐条规则实时 emit 错误，无嵌套 group 结构，无 Generation 计数器

## AOT（可选，放后面）

- `GetPropertyValue` 反射版不抗裁剪；后续源生成器为 partial VM 生成 `switch(name)` override（`virtual` 已铺路）
- 参与链的 VM 需 `partial`（与 C#13 partial property 兼容）

## 分析器（可选，攒灵感）

- [强锁] `GetObservable` 参数形状：`CallerArgumentExpression` 文本必须是 `<receiver>.<直接属性>`，治静默 no-op
- [强锁] 链路径每段对类型图静态校验
- [警告] 被观察属性的 setter 没走 `SetProperty`；`OnPropertyChanged(变量)` 捕获到变量名
- [锁不住] `RxCommand` 的 `IObservable<bool>` canExecute 语义是运行时行为，分析器救不了，靠防御性设计 + 文档

## 性能细节

- `PropertyValue` 原本是 struct，改成 record（class）后每次属性变化多一次堆分配；改回 `readonly record struct` 可降为"仅装箱"（值类型）

## 线程 / 调度

- 决策：强依赖 Rx 调度（`ObserveOn`/`SubscribeOn`），库不自建 Dispatcher 封装，除非发现 Rx 不够用
- `Subject` 并发 `OnNext` 不安全；Demo 里 `Task.Run` 改属性踩线
- `Changed` 是热流不回放初值（契约由 helper 的 `StartWith` 补），直接订阅 `Changed` 拿不到当前值
- `CanExecuteChanged` 由 `RxCommand` 经 `ObserveOnCurrentSynchronizationContext` 归队创建线程的同步上下文；无 SynchronizationContext 的环境（纯控制台/裸测试）下 R3 走 ThreadPool 异步交付，属非目标场景（MVVM 库按有 UI 上下文设计）。测试用 `InlineSynchronizationContext` 模拟

## Demo

- `MainWindowViewModel` 页面切换壳（Home / About / Validation + 导航命令），页面 VM 复用实例，切换触发反复 `WhenActivated` / 一次性 `WhenFirstActivated`
- Home：激活演示（首次加载 / 每次激活 / 失活 / `DisposeWhenDeactivated` 心跳计时器）+ 验证 + `GetObservable`
- About：`Interaction` 模态确认（VM `Handle` ↔ View `RegisterHandler`）
- Validation：`RuleFor` 同步规则（非空 / 长度）+ per-rule 防抖 300ms + `MustAsync` 异步查重（CancellationToken）+ 默认跳过首值 + `Validator.IsValid` 门控提交按钮；错误显示交给 Avalonia 内置 `INotifyDataErrorInfo` 错误模板（红框 + 提示），不手写 TextBlock。`Must`/`MustAsync` 现为扩展方法，底层使用 `RegisterRule`/`RegisterAsyncRule` 核心原语

## 路线图（按优先级）

对照 ReactiveUI 功能面，按"价值 / 难度"排。**目标：轻量，但传统痛点该有的得有。**

1. **Validation** — 已完成：`INotifyDataErrorInfo` + 错误聚合 + `GetErrors` + per-rule Debounce + `Behavior`（FailFast / CollectAll）+ `RegisterRule`/`RegisterAsyncRule` 核心原语 + `Must`/`MustAsync` 扩展语法糖 + 单 async foreach 评估 + 实时 partial emit + CancellationToken 取消控制
2. **观察列表元素属性变化** — 传统痛点（DynamicData 领域）。`ObservableCollection<T>` 中元素自身属性变化（如任一 item 的 `IsSelected`）：
   - 监听 CollectionChanged（增 / 删 / 替换 / Reset）→ 对每个 item 钩住 `Changed`，`Merge` 成一条流
   - 移除时释放订阅、替换时重钩、Reset 全量重挂；每元素订阅用 `IDisposable` 跟踪
   - item 需是 `IObservableObject`
3. **IViewFor** — 标记接口，`IViewFor<TViewModel> { TViewModel? ViewModel { get; set; } }`。Demo 已有 `ViewLocator`，半存在
4. **MessageBus** — 最易。一组 `Subject` 就行，顺手就做，但最没独特性
5. **多链组合** — `ObserveChanged(x => x.A.B, x => x.C.D)` 的 CombineLatest 重载（等链式 API 稳定再做）

## 决策记录（已否决 / 缓做）

- **`ToProperty` / 只读派生属性**：字典兜底方案因开销（装箱、哈希、生命周期）否决；手动 `source.Subscribe(v => Prop = v)` 为默认做法。只读语义的排查价值（写入路径单点）真实存在，若重做走生成器路线：`[Derived] partial` 属性 → 生成 `BehaviorSubject<T>` 字段 + `SetObservableProperty`（与 `GetPropertyValue` 对称）。优先级低，排在路线图全部之后
- **线程 / 调度**：强依赖 Rx，不自建（见上节）