using R3;
using RxMvvmLight;
using RxMvvmLight.Validation;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Demo.ViewModels
{
    public partial class ValidationViewModel : ValidationObservableObject, IActivatable
    {
        public ViewModelActivator Activator { get; } = new();

        public string Username { get => field; set => this.SetProperty(ref field, value); } = "";

        public string Email { get => field; set => this.SetProperty(ref field, value); } = "";

        public bool IsEmailChecking { get => field; set => this.SetProperty(ref field, value); }

        public string? Submission { get => field; set => this.SetProperty(ref field, value); }

        public string Password { get => field; set => this.SetProperty(ref field, value); } = "";
        public string ValidPassword { get => field; set => this.SetProperty(ref field, value); } = "";

        public RxCommand Submit { get; }

        public ValidationViewModel()
        {
            this.RuleFor(Username)
                .When(x=>x?.Length > 0)
                .Debounce(10000)
                .Length(3,16, "长度必须在 3 - 16 字符之间")
                .Subscribe();

            this.RuleFor(ValidPassword)
                .DependsOn(this.GetObservable(Password))
                .Required()
                .When(x => x?.Length > 0)
                .Debounce(300)
                .Must(x => x == Password, "两次输入不一致")
                .Subscribe();




            //this.RuleFor(Email)
            //    .When(x=>x?.Length > 0)
            //    .Must(p=> !string.IsNullOrWhiteSpace(p), "邮箱不可为空")
            //    .Must(p => string.IsNullOrWhiteSpace(p) || p.Contains('@'), "邮箱格式不正确")
            //    .Behavior()
            //    .Debounce(300)
            //    .MustAsync(async (email, ct) =>
            //    {
            //        if (string.IsNullOrWhiteSpace(email))
            //            return true;
            //        await Task.Delay(800, ct);
            //        return !email.StartsWith("taken", StringComparison.OrdinalIgnoreCase);
            //    }, "该邮箱已被占用")
            //    .Subscribe();


            Validator.Validating(nameof(Email))
                .Subscribe(v => IsEmailChecking = v)
                .DisposeWith(Activator);




            Submit = RxCommand.Create(() =>
                Submission = $"已提交 {Username} / {Email}（{DateTime.Now:HH:mm:ss}）",
                Validator.IsValid);
        }
    }
}