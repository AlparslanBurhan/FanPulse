using System.Windows;
using FanPulse.App.ViewModels;

namespace FanPulse.App.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _vm;

    public SettingsWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _vm.SaveApplyCommand.Execute(null);
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
