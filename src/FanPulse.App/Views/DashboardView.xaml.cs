using System.Windows;
using System.Windows.Controls;
using FanPulse.App.ViewModels;
using ScottPlot;

namespace FanPulse.App.Views;

public partial class DashboardView : UserControl
{
    private static readonly Color PlotBg = Color.FromHex("#242731");
    private static readonly Color PlotDataBg = Color.FromHex("#1B1D23");
    private static readonly Color PlotAxis = Color.FromHex("#9AA0AE");
    private static readonly Color CpuColor = Color.FromHex("#4C8DFF");
    private static readonly Color GpuColor = Color.FromHex("#5CD68C");
    private static readonly Color SysColor = Color.FromHex("#E8B34C");

    private MainViewModel? _vm;

    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => StylePlot();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.ChartUpdated -= RenderChart;

        _vm = DataContext as MainViewModel;

        if (_vm is not null)
            _vm.ChartUpdated += RenderChart;
    }

    private void StylePlot()
    {
        var plt = TrendPlot.Plot;
        plt.FigureBackground.Color = PlotBg;
        plt.DataBackground.Color = PlotDataBg;
        plt.Axes.Color(PlotAxis);
        plt.Grid.MajorLineColor = PlotAxis.WithOpacity(.12);
        plt.Legend.BackgroundColor = PlotBg;
        plt.Legend.FontColor = PlotAxis;
        plt.Legend.OutlineColor = PlotAxis.WithOpacity(.3);
        plt.Legend.Alignment = Alignment.UpperLeft;
        TrendPlot.UserInputProcessor.IsEnabled = false;
        TrendPlot.Refresh();
    }

    private void RenderChart()
    {
        if (_vm is null)
            return;

        var plt = TrendPlot.Plot;
        plt.Clear();

        double yMin = 25;
        double yMax = 90;

        AddSeries(_vm.ChartCpu, "CPU", CpuColor);
        AddSeries(_vm.ChartGpu, "GPU", GpuColor);
        AddSeries(_vm.ChartSystem, "System", SysColor);

        plt.Axes.SetLimits(-(MainViewModel.ChartCapacity - 1) * 2, 0, yMin, yMax);
        TrendPlot.Refresh();

        void AddSeries(List<double> buffer, string label, Color color)
        {
            var xs = new List<double>();
            var ys = new List<double>();

            for (var i = 0; i < buffer.Count; i++)
            {
                if (double.IsNaN(buffer[i]))
                    continue;

                xs.Add(-(buffer.Count - 1 - i) * 2); // saniye cinsinden geçmiş
                ys.Add(buffer[i]);
            }

            if (ys.Count == 0)
                return;

            var scatter = plt.Add.Scatter(xs.ToArray(), ys.ToArray());
            scatter.LegendText = label;
            scatter.Color = color;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 0;

            yMin = Math.Min(yMin, ys.Min() - 5);
            yMax = Math.Max(yMax, ys.Max() + 5);
        }
    }
}
