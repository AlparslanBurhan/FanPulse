using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FanPulse.App.ViewModels;
using FanPulse.Core.Config;
using ScottPlot;

namespace FanPulse.App.Views;

/// <summary>
/// Fan detay paneli. Eğri editörü: çift tık nokta ekler, sürükleme taşır,
/// sağ tık siler. Noktalar SelectedFan.CurvePoints üzerinde tutulur.
/// </summary>
public partial class FanDetailView : UserControl
{
    private const float HitRadiusPx = 16f;
    private const int MaxPoints = 8;

    private static readonly Color PlotBg = Color.FromHex("#2D3140");
    private static readonly Color PlotDataBg = Color.FromHex("#1B1D23");
    private static readonly Color PlotAxis = Color.FromHex("#9AA0AE");
    private static readonly Color CurveColor = Color.FromHex("#4C8DFF");

    private MainViewModel? _vm;
    private FanItemViewModel? _fan;
    private int _dragIndex = -1;
    private bool _plotStyled;

    // Scatter'ın bağlı olduğu diziler: sürükleme sırasında yerinde güncellenir,
    // yeniden inşa yalnızca bağlama/ekleme/silmede olur.
    private double[] _xs = [];
    private double[] _ys = [];

    public FanDetailView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        CurvePlot.MouseDown += OnPlotMouseDown;
        CurvePlot.MouseMove += OnPlotMouseMove;
        CurvePlot.MouseUp += OnPlotMouseUp;
        CurvePlot.MouseRightButtonUp += OnPlotRightClick;
        CurvePlot.MouseDoubleClick += OnPlotDoubleClick;
        CurvePlot.Loaded += (_, _) => { StylePlot(); RebuildPlot(); };
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = DataContext as MainViewModel;

        if (_vm is not null)
            _vm.PropertyChanged += OnVmPropertyChanged;

        BindFan();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedFan))
            BindFan();
    }

    private void BindFan()
    {
        if (_fan is not null)
            _fan.PropertyChanged -= OnFanPropertyChanged;

        _fan = _vm?.SelectedFan;

        if (_fan is not null)
            _fan.PropertyChanged += OnFanPropertyChanged;

        _dragIndex = -1;
        RebuildPlot();
    }

    private void OnFanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FanItemViewModel.IsCurve) && _fan is { IsCurve: true })
            RebuildPlot();
    }

    private void StylePlot()
    {
        if (_plotStyled)
            return;

        _plotStyled = true;

        var plt = CurvePlot.Plot;
        plt.FigureBackground.Color = PlotBg;
        plt.DataBackground.Color = PlotDataBg;
        plt.Axes.Color(PlotAxis);
        plt.Grid.MajorLineColor = PlotAxis.WithOpacity(.12);
        plt.Axes.Bottom.Label.Text = "°C";
        plt.Axes.Left.Label.Text = "%";
        CurvePlot.UserInputProcessor.IsEnabled = false;
    }

    /// <summary>Yalnızca bağlama, nokta ekleme/silme ve mod değişiminde çağrılır.</summary>
    private void RebuildPlot()
    {
        var plt = CurvePlot.Plot;
        plt.Clear();

        if (_fan is not null && _fan.CurvePoints.Count > 0)
        {
            _fan.CurvePoints.Sort((a, b) => a.Temp.CompareTo(b.Temp));

            _xs = _fan.CurvePoints.Select(p => (double)p.Temp).ToArray();
            _ys = _fan.CurvePoints.Select(p => (double)p.Percent).ToArray();

            var scatter = plt.Add.Scatter(_xs, _ys);
            scatter.Color = CurveColor;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 10;
        }

        plt.Axes.SetLimits(0, 100, 0, 105);
        CurvePlot.Refresh();
    }

    /// <summary>DPI ölçekli fare konumu → ScottPlot pikseli (tek doğruluk kaynağı).</summary>
    private Pixel ToPixel(MouseEventArgs e)
    {
        var pos = e.GetPosition(CurvePlot);
        var scale = CurvePlot.DisplayScale;
        return new Pixel((float)(pos.X * scale), (float)(pos.Y * scale));
    }

    private Coordinates GetCoordinates(MouseEventArgs e) =>
        CurvePlot.Plot.GetCoordinates(ToPixel(e));

    private int FindNearestPoint(MouseEventArgs e)
    {
        if (_fan is null)
            return -1;

        var mouse = ToPixel(e);

        for (var i = 0; i < _fan.CurvePoints.Count; i++)
        {
            var p = _fan.CurvePoints[i];
            var px = CurvePlot.Plot.GetPixel(new Coordinates(p.Temp, p.Percent));
            var dx = px.X - mouse.X;
            var dy = px.Y - mouse.Y;

            if (Math.Sqrt(dx * dx + dy * dy) <= HitRadiusPx)
                return i;
        }

        return -1;
    }

    private void OnPlotMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || _fan is not { IsCurve: true })
            return;

        _dragIndex = FindNearestPoint(e);
        if (_dragIndex >= 0)
            CurvePlot.CaptureMouse();
    }

    private void OnPlotMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragIndex < 0 || _fan is null || _dragIndex >= _xs.Length)
            return;

        var coords = GetCoordinates(e);
        var points = _fan.CurvePoints;

        var minTemp = _dragIndex > 0 ? points[_dragIndex - 1].Temp + 1 : 0;
        var maxTemp = _dragIndex < points.Count - 1 ? points[_dragIndex + 1].Temp - 1 : 100;

        var point = points[_dragIndex];
        point.Temp = Math.Clamp((float)Math.Round(coords.X), minTemp, maxTemp);
        point.Percent = Math.Clamp((float)Math.Round(coords.Y), 0, 100);

        // Kıskaç sıralamayı koruduğundan yeniden inşa gerekmez:
        // scatter'ın bağlı olduğu dizileri yerinde güncelle, sadece yeniden çiz.
        _xs[_dragIndex] = point.Temp;
        _ys[_dragIndex] = point.Percent;
        CurvePlot.Refresh();
    }

    private void OnPlotMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragIndex >= 0)
        {
            _dragIndex = -1;
            CurvePlot.ReleaseMouseCapture();
        }
    }

    private void OnPlotRightClick(object sender, MouseButtonEventArgs e)
    {
        if (_fan is not { IsCurve: true } || _fan.CurvePoints.Count <= 2)
            return;

        var index = FindNearestPoint(e);
        if (index < 0)
            return;

        _fan.CurvePoints.RemoveAt(index);
        RebuildPlot();
    }

    private void OnPlotDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left ||
            _fan is not { IsCurve: true } ||
            _fan.CurvePoints.Count >= MaxPoints ||
            FindNearestPoint(e) >= 0)
        {
            return;
        }

        var coords = GetCoordinates(e);
        _fan.CurvePoints.Add(new CurvePoint
        {
            Temp = Math.Clamp((float)Math.Round(coords.X), 0, 100),
            Percent = Math.Clamp((float)Math.Round(coords.Y), 0, 100),
        });

        RebuildPlot();
    }
}
