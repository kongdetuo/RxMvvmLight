using R3;
using RxMvvmLight;
using RxMvvmLight.Validation;

namespace RxMvvmLight.Tests;

public class ValidationTests
{
    [Fact]
    public void SyncRules_UpdateErrorsAndGate()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        bool isValid = true, isValidating = false;
        validator.IsValid.Subscribe(v => isValid = v);
        validator.IsValidating.Subscribe(v => isValidating = v);

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Must(n => !string.IsNullOrEmpty(n), "Name 不能为空")
            .Subscribe();

        name.OnNext("");
        Assert.True(validator.HasErrors);
        Assert.Equal(new[] { "Name 不能为空" }, validator.GetErrors("Name").Cast<string>());
        Assert.False(isValid);
        Assert.False(isValidating);

        name.OnNext("abc");
        Assert.False(validator.HasErrors);
        Assert.True(isValid);
        Assert.Empty(validator.GetErrors("Name").Cast<string>());
    }

    [Fact]
    public async Task Debounce_DefersEvaluation()
    {
        var validator = new Validator();
        var email = new BehaviorSubject<string>("");
        int evalCount = 0;
        bool isValidating = false;
        validator.IsValidating.Subscribe(v => isValidating = v);

        new PropertyValidationBuilder<string>(validator, "Email", email)
            .Debounce(300)
            .MustAsync(async e =>
            {
                evalCount++;
                await Task.Delay(30);
                return !string.IsNullOrEmpty(e);
            }, "Email 不能为空")
            .Subscribe();

        Assert.True(isValidating);
        await Task.Delay(80);
        Assert.Equal(0, evalCount);
        email.OnNext("");
        await Task.Delay(400);
        Assert.Equal(1, evalCount);
        Assert.False(isValidating);
        Assert.True(validator.HasErrors);
    }

    [Fact]
    public async Task AsyncRace_StaleResultsAreDropped()
    {
        var validator = new Validator();
        var email = new Subject<string>();
        bool isValid = true;
        validator.IsValid.Subscribe(v => isValid = v);

        new PropertyValidationBuilder<string>(validator, "Email", email)
            .MustAsync(async e =>
            {
                if (e == "first")
                {
                    await Task.Delay(300);
                    return false;
                }
                if (e == "second")
                {
                    await Task.Delay(50);
                    return true;
                }
                return false;
            }, "bad")
            .Subscribe();

        email.OnNext("first");
        await Task.Delay(30);
        email.OnNext("second");
        await Task.Delay(600);

        Assert.False(validator.HasErrors);
        Assert.True(isValid);
    }

    [Fact]
    public async Task MergeSameProperty_CombinesRules()
    {
        var validator = new Validator();
        var name = new BehaviorSubject<string>("");
        bool isValid = false;
        validator.IsValid.Subscribe(v => isValid = v);

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Must(n => !string.IsNullOrEmpty(n), "必填")
            .Subscribe();
        Assert.Single(validator.GetErrors("Name").Cast<string>());

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Must(n => n.Length >= 2, "至少2位")
            .Subscribe();

        name.OnNext("a");
        await Task.Delay(50);
        Assert.Equal(new[] { "至少2位" }, validator.GetErrors("Name").Cast<string>());

        name.OnNext("abc");
        await Task.Delay(50);
        Assert.Empty(validator.GetErrors("Name").Cast<string>());
        Assert.True(isValid);
    }

    [Fact]
    public async Task CommandComposition_CombineValidAndValidating()
    {
        var validator = new Validator();
        var name = new BehaviorSubject<string>("");

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Must(n => !string.IsNullOrEmpty(n), "必填")
            .Subscribe();

        var canSave = validator.IsValid.CombineLatest(validator.IsValidating, (v, a) => v && !a);
        bool canSaveNow = false;
        canSave.Subscribe(v => canSaveNow = v);

        Assert.False(canSaveNow);
        name.OnNext("ok");
        await Task.Delay(50);
        Assert.True(canSaveNow);
        name.OnNext("");
        await Task.Delay(50);
        Assert.False(canSaveNow);
    }

    [Fact]
    public async Task TokenCancellation_CancelsInFlightAsyncRule()
    {
        var validator = new Validator();
        var email = new Subject<string>();
        int canceled = 0;

        new PropertyValidationBuilder<string>(validator, "Email", email)
            .MustAsync(async (e, ct) =>
            {
                try
                {
                    await Task.Delay(300, ct);
                    return !string.IsNullOrEmpty(e);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref canceled);
                    return false;
                }
            }, "bad")
            .Subscribe();

        email.OnNext("a");
        await Task.Delay(50);
        email.OnNext("b");
        await Task.Delay(600);

        Assert.True(canceled >= 1);
        Assert.False(validator.HasErrors);
    }

    [Fact]
    public async Task ErrorsChanged_NotSpammedOnUnchangedErrors()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        int errorListChanges = 0;
        List<string>? lastErrors = null;
        validator.StateChanged.Subscribe(_ =>
        {
            var current = validator.GetErrors("Name").Cast<string>().ToList();
            if (lastErrors == null || !lastErrors.SequenceEqual(current))
            {
                errorListChanges++;
                lastErrors = current;
            }
        });

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Must(n => n.Length > 3, "太短")
            .Subscribe();

        name.OnNext("abc");
        await Task.Delay(30);
        var countAfterFirst = errorListChanges;

        name.OnNext("abc");
        await Task.Delay(30);
        Assert.Equal(countAfterFirst, errorListChanges);

        name.OnNext("abcd");
        await Task.Delay(30);
        Assert.True(errorListChanges > countAfterFirst);
        Assert.False(validator.HasErrors);
    }

    [Fact]
    public async Task OldError_KeptDuringRevalidation()
    {
        var validator = new Validator();
        var name = new Subject<string>();

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Debounce(200)
            .Must(n => n.Length >= 3, "太短")
            .Subscribe();

        name.OnNext("");
        await Task.Delay(400);
        Assert.True(validator.HasErrors);

        name.OnNext("ab");
        await Task.Delay(80);
        Assert.True(validator.HasErrors);

        await Task.Delay(400);
        Assert.True(validator.HasErrors);

        name.OnNext("abcd");
        await Task.Delay(400);
        Assert.False(validator.HasErrors);
    }

    [Fact]
    public async Task PerPropertyState_ReflectsLifecycle()
    {
        var validator = new Validator();
        var name = new BehaviorSubject<string>("");
        ValidationState? lastState = null;
        bool lastValidating = false;
        bool lastValid = false;
        validator.GetState("Name").Subscribe(s => lastState = s);
        validator.Validating("Name").Subscribe(v => lastValidating = v);
        validator.Valid("Name").Subscribe(v => lastValid = v);

        Assert.Equal(ValidationState.NotValidated, lastState);
        Assert.False(lastValidating);
        Assert.False(lastValid);

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Debounce(200)
            .Must(n => n.Length >= 3, "太短")
            .Subscribe();

        await Task.Delay(50);
        Assert.Equal(ValidationState.Validating, lastState);

        await Task.Delay(400);
        Assert.Equal(ValidationState.Invalid, lastState);

        name.OnNext("abc");
        await Task.Delay(400);
        Assert.Equal(ValidationState.Valid, lastState);
        Assert.False(lastValidating);
        Assert.True(lastValid);
    }

    [Fact]
    public async Task RuleException_FailsValidation_KeepsPipelineAlive()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        bool isValid = true;
        validator.IsValid.Subscribe(v => isValid = v);

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Must(n => n == "boom" ? throw new InvalidOperationException("boom") : n.Length >= 3, "太短")
            .Subscribe();

        name.OnNext("boom");
        await Task.Delay(50);
        Assert.True(validator.HasErrors);
        Assert.Contains("boom", validator.GetErrors("Name").Cast<string>());
        Assert.False(isValid);

        name.OnNext("abc");
        await Task.Delay(50);
        Assert.False(validator.HasErrors);
        Assert.True(isValid);

        name.OnNext("boom");
        await Task.Delay(50);
        Assert.True(validator.HasErrors);
    }

    [Fact]
    public void Sequential_Default_StopsOnFirstFailure()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        int evalCount = 0;

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Must(_ => { evalCount++; return false; }, "第一")
            .Must(_ => { evalCount++; return false; }, "第二")
            .Must(_ => { evalCount++; return false; }, "第三")
            .Subscribe();

        name.OnNext("");
        Assert.Equal(1, evalCount);
        Assert.Single(validator.GetErrors("Name").Cast<string>());
        Assert.Contains("第一", validator.GetErrors("Name").Cast<string>());
    }

    [Fact]
    public void Sequential_PassesThroughOnSuccess_StopsOnFailure()
    {
        var validator = new Validator();
        var name = new Subject<string>();

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Must(_ => true, "第一")
            .Must(_ => false, "第二")
            .Must(_ => false, "第三")
            .Subscribe();

        name.OnNext("");
        Assert.Single(validator.GetErrors("Name").Cast<string>());
        Assert.Contains("第二", validator.GetErrors("Name").Cast<string>());
    }

    [Fact]
    public void SequentialAll_RunsAllRulesInOrder()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        var order = new List<int>();

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Behavior(ValidationBehavior.CollectAll)
            .Must(_ => { order.Add(1); return false; }, "第一")
            .Must(_ => { order.Add(2); return false; }, "第二")
            .Must(_ => { order.Add(3); return false; }, "第三")
            .Subscribe();

        name.OnNext("");
        Assert.Equal(new[] { 1, 2, 3 }, order);
        Assert.Equal(3, validator.GetErrors("Name").Cast<string>().Count());
    }

    [Fact]
    public void Group_StopsAtNextGroupOnFailure()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        int group2Count = 0;

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Must(_ => false, "失败")
            .Behavior()
            .Must(_ => { group2Count++; return true; }, "不应执行")
            .Subscribe();

        name.OnNext("");
        Assert.Equal(0, group2Count);
        Assert.Single(validator.GetErrors("Name").Cast<string>());
    }

    [Fact]
    public async Task Sequential_MixedSyncAsync_StopsOnFirstFailure()
    {
        var validator = new Validator();
        var email = new Subject<string>();
        var order = new List<string>();

        new PropertyValidationBuilder<string>(validator, "Email", email)
            .Must(e => { order.Add("格式"); return false; }, "格式错")
            .MustAsync(async e => { await Task.Delay(10); order.Add("存在"); return true; }, "已存在")
            .Subscribe();

        email.OnNext("");
        await Task.Delay(50);
        Assert.Equal(new[] { "格式" }, order);
        Assert.Single(validator.GetErrors("Email").Cast<string>());
    }

    [Fact]
    public async Task Debounce_Cancellation_ResetsOnNewValue()
    {
        var validator = new Validator();
        var email = new Subject<string>();
        int evalCount = 0;

        new PropertyValidationBuilder<string>(validator, "Email", email)
            .Must(_ => true, "同步")
            .Debounce(300)
            .RegisterAsyncRule(async _ => { await Task.Delay(10); evalCount++; return Array.Empty<string>(); })
            .Subscribe();

        await Task.Delay(50);
        Assert.Equal(0, evalCount);
        email.OnNext("a");
        await Task.Delay(100);
        email.OnNext("b");
        await Task.Delay(400);
        Assert.Equal(1, evalCount);
    }

    [Fact]
    public async Task CollectAll_EmitsPartialResults()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        var rule2Start = new TaskCompletionSource();
        var rule2Proceed = new TaskCompletionSource();

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Behavior(ValidationBehavior.CollectAll)
            .Must(_ => false, "错误1")
            .RegisterAsyncRule(async _ =>
            {
                rule2Start.TrySetResult();
                await rule2Proceed.Task;
                return new[] { "错误2" };
            })
            .Subscribe();

        name.OnNext("");
        await rule2Start.Task;
        Assert.Single(validator.GetErrors("Name").Cast<string>());
        Assert.Contains("错误1", validator.GetErrors("Name").Cast<string>());

        rule2Proceed.TrySetResult();
        await Task.Delay(50);
        Assert.Equal(2, validator.GetErrors("Name").Cast<string>().Count());
        Assert.Contains("错误1", validator.GetErrors("Name").Cast<string>());
        Assert.Contains("错误2", validator.GetErrors("Name").Cast<string>());
    }

    [Fact]
    public void Required_NullOrEmpty_FailsValidation()
    {
        var validator = new Validator();
        var name = new Subject<string>();

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Required("姓名不能为空")
            .Subscribe();

        name.OnNext("");
        Assert.True(validator.HasErrors);
        Assert.Contains("姓名不能为空", validator.GetErrors("Name").Cast<string>());
    }

    [Fact]
    public void Required_NonEmpty_PassesValidation()
    {
        var validator = new Validator();
        var name = new Subject<string>();

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .Required("姓名不能为空")
            .Must(n => n.Length >= 3, "至少3个字符")
            .Subscribe();

        name.OnNext("abc");
        Assert.False(validator.HasErrors);
    }

    [Fact]
    public void When_ConditionTrue_RulesExecute()
    {
        var validator = new Validator();
        var name = new Subject<string>();

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .When(_ => true)
            .Must(n => n.Length >= 3, "至少3个字符")
            .Subscribe();

        name.OnNext("x");
        Assert.True(validator.HasErrors);
        Assert.Contains("至少3个字符", validator.GetErrors("Name").Cast<string>());
    }

    [Fact]
    public void When_ConditionFalse_RulesSkipped()
    {
        var validator = new Validator();
        var name = new Subject<string>();

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .When(_ => false)
            .Must(n => n.Length >= 3, "至少3个字符")
            .Subscribe();

        name.OnNext("x");
        Assert.False(validator.HasErrors);
    }

    [Fact]
    public void When_ChangesCondition_Reevaluates()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        bool condition = false;

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .When(_ => condition)
            .Must(n => n.Length >= 3, "至少3个字符")
            .Subscribe();

        name.OnNext("x");
        Assert.False(validator.HasErrors);

        condition = true;
        name.OnNext("y");
        Assert.True(validator.HasErrors);
        Assert.Contains("至少3个字符", validator.GetErrors("Name").Cast<string>());
    }

    [Fact]
    public void When_ConsecutiveWhen_NestedConditions()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        bool conditionA = true;
        bool conditionB = true;

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .When(_ => conditionA)
            .When(_ => conditionB)
            .Must(n => n.Length >= 3, "至少3个字符")
            .Subscribe();

        name.OnNext("x");
        Assert.True(validator.HasErrors);

        conditionA = false;
        name.OnNext("y");
        Assert.False(validator.HasErrors);

        conditionA = true;
        conditionB = false;
        name.OnNext("z");
        Assert.False(validator.HasErrors);
    }

    [Fact]
    public async Task DependsOn_TriggersReevaluation()
    {
        var validator = new Validator();
        var name = new Subject<string>();
        var refresh = new BehaviorSubject<Unit>(Unit.Default);

        new PropertyValidationBuilder<string>(validator, "Name", name)
            .DependsOn(refresh)
            .Must(n => n.Length >= 3, "至少3个字符")
            .Subscribe();

        name.OnNext("x");
        Assert.True(validator.HasErrors);

        name.OnNext("abc");
        refresh.OnNext(Unit.Default);
        await Task.Delay(50);
        Assert.False(validator.HasErrors);
    }
}
