using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FanPulse.App.Localization;
using H.NotifyIcon;

namespace FanPulse.App.Tray;

/// <summary>
/// Headless moddaki tepsi simgesi: canlı özet tooltip'i, çift tıkla arayüz,
/// sağ tık menüsünde Aç / Duraklat / Çık.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly TaskbarIcon _icon;

    public TrayService(Action openGui, Action exitApp, Action<bool> togglePause)
    {
        var menu = new ContextMenu();

        var openItem = new MenuItem { Header = Loc.T("TrayOpen") };
        openItem.Click += (_, _) => openGui();
        menu.Items.Add(openItem);

        var pauseItem = new MenuItem { Header = Loc.T("TrayPause"), IsCheckable = true };
        pauseItem.Checked += (_, _) => togglePause(true);
        pauseItem.Unchecked += (_, _) => togglePause(false);
        menu.Items.Add(pauseItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = Loc.T("TrayExit") };
        exitItem.Click += (_, _) => exitApp();
        menu.Items.Add(exitItem);

        _icon = new TaskbarIcon
        {
            ToolTipText = "FanPulse",
            ContextMenu = menu,
            IconSource = BitmapFrame.Create(
                new Uri("pack://application:,,,/Assets/fanpulse.ico", UriKind.Absolute)),
        };

        _icon.TrayMouseDoubleClick += (_, _) => openGui();
        _icon.ForceCreate();
    }

    public void UpdateTooltip(string text) => _icon.ToolTipText = text;

    public void ShowNotice(string title, string message)
    {
        try
        {
            _icon.ShowNotification(title, message);
        }
        catch
        {
            // Bildirim gösterilemezse sessiz geç; kritik değil.
        }
    }

    public void Dispose() => _icon.Dispose();
}
