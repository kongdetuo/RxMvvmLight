using R3;
using RxMvvmLight;
using RxMvvmLight.Validation;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Demo.ViewModels
{
    public partial class HomeViewModel : ValidationObservableObject, IActivatable
    {
        private ViewModelActivator? activator;

        public ViewModelActivator Activator => activator ??= new();

        public string Greeting { get; set => this.SetProperty(ref field, value); } = "Welcome to Avalonia!";

        public ObservableCollection<string> Log { get; } = new();

        public int Ticks { get; set => this.SetProperty(ref field, value); }

        public HomeViewModel()
        {
            this.ObserveChanged(x => x.Greeting)
                .Subscribe(x =>
                {
                    Debug.WriteLine(x);
                });

            //this.RuleFor(Greeting)
            //    .Debounce(300)
            //    .Must(p => p.Length > 0, "不可为空")
            //    .Subscribe();

            //this.WhenFirstActivated(() => Log.Add("首次激活：开始加载首页数据"));
            //this.WhenActivated(() => Log.Add("激活（每次进入页面）"));
            //this.WhenDeactivated(() => Log.Add("失活（离开页面）"));

            //Observable.Interval(TimeSpan.FromSeconds(1))
            //    .Subscribe(_ => Ticks++)
            //    .DisposeWhenDeactivated(Activator);
        }
    }
}