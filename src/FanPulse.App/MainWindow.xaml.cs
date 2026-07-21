using System.Windows;
using FanPulse.App.ViewModels;
using FanPulse.App.Views;

namespace FanPulse.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Common.DarkTitleBar.Apply(this);
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        new SettingsWindow(vm) { Owner = this }.ShowDialog();
    }
}
