using RxMvvmLight;

namespace Demo.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly HomeViewModel home = new();
        private readonly AboutViewModel about = new();
        private readonly ValidationViewModel validation = new();

        public ObservableObject? Current { get; set => this.SetProperty(ref field, value); }

        public RxCommand GoHome { get; }

        public RxCommand GoAbout { get; }

        public RxCommand GoValidation { get; }

        public MainWindowViewModel()
        {
            Current = home;
            GoHome = RxCommand.Create(() => Current = home);
            GoAbout = RxCommand.Create(() => Current = about);
            GoValidation = RxCommand.Create(() => Current = validation);
        }
    }
}