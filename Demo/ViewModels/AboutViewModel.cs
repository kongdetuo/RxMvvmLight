using RxMvvmLight;
using System.Collections.ObjectModel;

namespace Demo.ViewModels
{
    public partial class AboutViewModel : ObservableObject, IActivatable
    {
        private ViewModelActivator? activator;

        public ViewModelActivator Activator => activator ??= new();

        public Interaction<string, bool> Confirm { get; } = new();

        public ObservableCollection<string> Log { get; } = new();

        public RxCommand AskConfirm { get; }

        public AboutViewModel()
        {
            AskConfirm = RxCommand.Create(async () =>
            {
                Log.Add("VM: 发起确认请求…");
                var ok = await Confirm.Handle("确认继续这个操作？");
                Log.Add($"VM: 收到结果 = {ok}");
            });

            this.WhenFirstActivated(() => Log.Add("首次激活：加载关于信息"));
            this.WhenActivated(() => Log.Add("激活（每次进入页面）"));
            this.WhenDeactivated(() => Log.Add("失活（离开页面）"));
        }
    }
}