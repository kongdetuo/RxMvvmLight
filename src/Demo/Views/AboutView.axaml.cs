using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Demo.ViewModels;
using System;
using System.Threading.Tasks;

namespace Demo.Views
{
    public partial class AboutView : ActivatableView
    {
        private IDisposable? handlerRegistration;

        public AboutView()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            handlerRegistration?.Dispose();
            handlerRegistration = (DataContext as AboutViewModel)?.Confirm.RegisterHandler(ShowConfirmAsync);
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            handlerRegistration?.Dispose();
            handlerRegistration = null;
        }

        private async Task<bool> ShowConfirmAsync(string message)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var yesButton = new Button { Content = "确定" };
            var noButton = new Button { Content = "取消" };

            var window = new Window
            {
                Width = 360,
                Height = 180,
                CanResize = false,
                ShowInTaskbar = false,
                Title = "确认",
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 8,
                            Children = { yesButton, noButton },
                        },
                    },
                },
            };

            yesButton.Click += (_, _) =>
            {
                tcs.TrySetResult(true);
                window.Close();
            };
            noButton.Click += (_, _) =>
            {
                tcs.TrySetResult(false);
                window.Close();
            };

            var owner = TopLevel.GetTopLevel(this) as Window;
            await window.ShowDialog(owner!);
            return tcs.Task.IsCompleted ? await tcs.Task : false;
        }
    }
}