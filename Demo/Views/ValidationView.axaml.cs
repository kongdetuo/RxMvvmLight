using Avalonia.Controls;
using Demo.ViewModels;

namespace Demo.Views;

public partial class ValidationView : ActivatableView
{
    public ValidationView()
    {
        InitializeComponent();
    }

    private void CulturePicker_CultureChanged(object? sender, Irihi.Lingua.CultureChangedEventArgs e)
    {
        ((ValidationViewModel) this.DataContext).Validator.RefreshMessage();
    }
}
