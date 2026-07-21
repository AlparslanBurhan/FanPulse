using System.Windows;
using FanPulse.App.ViewModels;

namespace FanPulse.App.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _vm;

    // Diyalog sözleşmesi: Kaydet = taahhüt, Kapat/X = hiçbir şey olmadı.
    // Bindings canlı Config'e yazdığından, iptal durumunda anlık görüntü geri yüklenir.
    private readonly (bool Startup, bool Tray, float Floor, string Lang) _original;
    private bool _saved;

    public SettingsWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        _original = (vm.Config.ApplyOnStartup, vm.Config.ShowTrayIcon,
                     vm.Config.MinSpeedFloor, vm.Config.Language);
        InitializeComponent();
        Common.DarkTitleBar.Apply(this);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _saved = true;
        _vm.SaveApplyCommand.Execute(null);
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        if (!_saved)
        {
            (_vm.Config.ApplyOnStartup, _vm.Config.ShowTrayIcon,
             _vm.Config.MinSpeedFloor, _vm.Config.Language) = _original;
        }

        base.OnClosed(e);
    }
}
