using Irihi.Lingua;
using R3;
using RxMvvmLight;
using RxMvvmLight.Validation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Demo.ViewModels;

public partial class ValidationViewModel : ValidationObservableObject, IActivatable
{
    public ViewModelActivator Activator { get; } = new();

    /// <summary>
    /// 普通检查：Required + MinLength，实时验证
    /// </summary>
    public string Name
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = "";

    /// <summary>
    /// 可空检查：有值时才验证格式
    /// </summary>
    public string? Nickname
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    /// <summary>
    /// 条件防抖：输入内容后才开始 300ms 防抖验证
    /// </summary>
    public string Email
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = "";

    /// <summary>
    /// 可空条件防抖 + 手动触发：异步查重，支持手动触发按钮
    /// </summary>
    public string Phone
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = "";

    public bool IsPhoneChecking
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    public string? Result { get; set => this.SetProperty(ref field, value); }

    public RxCommand Submit { get; }

    /// <summary>
    /// 手动触发 Phone 的验证管线
    /// </summary>
    public RxCommand ValidatePhone { get; }


    public IList<ILinguaManager> Managers { get; } =
    [
        LanguageManager.Instance,
    ];
    public LinguaCulture Culture { get; set => this.SetProperty(ref field, value); }



    public void Foo<T>(T value)
    {

    }

    public ValidationViewModel()
    {
        this.Foo(() => 1);

        //LanguageManager.Instance.CultureChanges.ToObservable().Subscribe(_ =>
        //{
        //    // 这个不行，这个会先于具体资源触发, 还是 CultureChanged 事件靠谱
        //    this.Validator.Update();
        //});

        MessageConverter.Register<IObservable<string?>>(x =>
        {
            return ((LinguaObservableString)x).CurrentValue!;
        });

        // 1. 普通检查：同步规则，实时验证
        this.RuleFor(Name)
            .Required(LanguageManager.Instance.required)
            .MinLength(3, LanguageManager.Instance.user_name_min_length)
            .Subscribe();

        // 2. 可空检查：有值时才验证格式
        this.RuleFor(Nickname)
            .When(n => n?.Length > 0)
            .Must(n => n?.Length <= 12, ()=>$"昵称最多12个字符 当前字符数{Nickname?.Length ?? 0}")
            .Subscribe();

        // 3. 条件防抖：输入内容后才开始防抖
        this.RuleFor(Email)
            .When(e => e?.Length > 0)
            .Debounce(300)
            .Email("邮箱格式不正确")
            .Subscribe();

        // 手动触发按钮
        ValidatePhone = RxCommand.Create(() => { });

        // 4. 可空条件防抖 + 手动触发
        this.RuleFor(ValidatePhone.IsRunning.Select(p => !p).Select(p => this.Phone), nameof(Phone))
            .When(p => p?.Length > 0)
            .Matches(@"^1[3-9]\d{9}$", "手机号格式不正确")
            .Debounce(300)
            .MustAsync(async (phone, ct) =>
            {
                await Task.Delay(500, ct); // 模拟远程查重
                return phone != "13800000000";
            }, "该手机号已被注册")
            .Subscribe();

        // 异步验证 loading 状态
        Validator.Validating(nameof(Phone))
            .Subscribe(v => IsPhoneChecking = v)
            .DisposeWith(Activator);



        // 提交按钮：所有验证通过才可用
        Submit = RxCommand.Create(
            () => Result = $"已提交 {Name} / {Email}（{DateTime.Now:HH:mm:ss}）",
            Validator.IsValid);
    }
}
