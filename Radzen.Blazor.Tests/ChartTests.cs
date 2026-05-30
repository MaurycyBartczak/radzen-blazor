using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace Radzen.Blazor.Tests;

public class ChartTests
{
    private readonly ITestOutputHelper output;
    public ChartTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact(Timeout = 30000)]
    public async Task Chart_Tooltip_Performance()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<Rect>("Radzen.createChart", _ => true).SetResult(new Rect {Left = 0, Top = 0, Width = 200, Height = 200});
        ctx.Services.AddScoped<TooltipService>();
        ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
        ctx.RenderComponent<RadzenChartTooltip>();

        var seriesData = Enumerable.Range(0, 5000).Select(i => new Point { X = i, Y = i });
        var chart = ctx.RenderComponent<RadzenChart>(chartParameters =>
            chartParameters
                .AddChildContent<RadzenLineSeries<Point>>(seriesParameters =>
                    seriesParameters
                        .Add(p => p.CategoryProperty, nameof(Point.X))
                        .Add(p => p.ValueProperty, nameof(Point.Y))
                        .Add(p => p.Data, seriesData))
                .AddChildContent<RadzenCategoryAxis>(axisParameters =>
                    axisParameters
                        .Add(p => p.Step, 100)
                        .Add(p => p.Formatter, x =>
                        {
                            Thread.Sleep(100);
                            return $"{x}";
                        })));

        var stopwatch = Stopwatch.StartNew();
        foreach (var invocation in Enumerable.Range(0, 10))
        {
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 80));
            Assert.Equal((invocation + 1) * 2, ctx.JSInterop.Invocations.Count(x => x.Identifier == "Radzen.openChartTooltip"));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(0, 0));
            Assert.Equal(invocation + 1, ctx.JSInterop.Invocations.Count(x => x.Identifier == "Radzen.closeTooltip"));
        }
        output.WriteLine($"Time took: {stopwatch.Elapsed}");
    }

    private class DualAxisItem
    {
        public string Month { get; set; }
        public double Revenue { get; set; }
        public double Rate { get; set; }
    }

    private static readonly DualAxisItem[] DualAxisData =
    {
        new() { Month = "Jan", Revenue = 1200, Rate = 12 },
        new() { Month = "Feb", Revenue = 2800, Rate = 18 },
        new() { Month = "Mar", Revenue = 4300, Rate = 26 },
    };

    private static TestContext CreateChartContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<Rect>("Radzen.createChart", _ => true)
            .SetResult(new Rect { Left = 0, Top = 0, Width = 600, Height = 400 });
        ctx.Services.AddScoped<TooltipService>();
        return ctx;
    }

    [Fact]
    public void Chart_Renders_Secondary_Value_Axis_For_Secondary_Series()
    {
        using var ctx = CreateChartContext();

        var chart = ctx.RenderComponent<RadzenChart>(parameters => parameters
            .AddChildContent<RadzenColumnSeries<DualAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(DualAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(DualAxisItem.Revenue))
                .Add(p => p.YAxis, AxisY.Primary)
                .Add(p => p.Data, DualAxisData))
            .AddChildContent<RadzenLineSeries<DualAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(DualAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(DualAxisItem.Rate))
                .Add(p => p.YAxis, AxisY.Secondary)
                .Add(p => p.Data, DualAxisData))
            .AddChildContent<RadzenValueAxis>(axis => axis
                .Add(p => p.Position, ValueAxisPosition.Left))
            .AddChildContent<RadzenValueAxis>(axis => axis
                .Add(p => p.Position, ValueAxisPosition.Right)));

        Assert.True(chart.Instance.HasSecondaryValueAxis());
        Assert.Equal(2, chart.FindAll(".rz-value-axis").Count);
        // The secondary axis ticks are anchored to the right of the plot area. The inline style is
        // required so the start anchor wins over the .rz-tick-text { text-anchor: end } stylesheet rule;
        // without it the labels render right-anchored and overlap the grid.
        Assert.Contains("text-anchor=\"start\"", chart.Markup);
        Assert.Contains("text-anchor: start", chart.Markup);
    }

    [Fact]
    public void Chart_GetValueScale_Returns_Secondary_Scale_For_Secondary_Series()
    {
        using var ctx = CreateChartContext();

        var chart = ctx.RenderComponent<RadzenChart>(parameters => parameters
            .AddChildContent<RadzenColumnSeries<DualAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(DualAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(DualAxisItem.Revenue))
                .Add(p => p.YAxis, AxisY.Primary)
                .Add(p => p.Data, DualAxisData))
            .AddChildContent<RadzenLineSeries<DualAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(DualAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(DualAxisItem.Rate))
                .Add(p => p.YAxis, AxisY.Secondary)
                .Add(p => p.Data, DualAxisData))
            .AddChildContent<RadzenValueAxis>(axis => axis
                .Add(p => p.Position, ValueAxisPosition.Right)));

        var instance = chart.Instance;
        var primarySeries = instance.Series.Single(s => s.YAxis == AxisY.Primary);
        var secondarySeries = instance.Series.Single(s => s.YAxis == AxisY.Secondary);

        Assert.Same(instance.ValueScale, instance.GetValueScale(primarySeries));
        Assert.Same(instance.SecondaryValueScale, instance.GetValueScale(secondarySeries));
        Assert.Same(instance.SecondaryValueAxis, instance.GetValueAxis(secondarySeries));
    }

    [Fact]
    public void Chart_Without_Secondary_Series_Renders_Single_Value_Axis()
    {
        using var ctx = CreateChartContext();

        var chart = ctx.RenderComponent<RadzenChart>(parameters => parameters
            .AddChildContent<RadzenColumnSeries<DualAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(DualAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(DualAxisItem.Revenue))
                .Add(p => p.Data, DualAxisData)));

        Assert.False(chart.Instance.HasSecondaryValueAxis());
        Assert.Equal(1, chart.FindAll(".rz-value-axis").Count);
    }

    private static readonly DualAxisItem[] TallBarData =
    {
        new() { Month = "Jan", Revenue = 9000, Rate = 40 },
        new() { Month = "Feb", Revenue = 8000, Rate = 45 },
        new() { Month = "Mar", Revenue = 9500, Rate = 50 },
    };

    [Fact]
    public void Line_Marker_On_Top_Of_Column_Wins_Hover_Selection()
    {
        using var ctx = CreateChartContext();

        var chart = ctx.RenderComponent<RadzenChart>(parameters => parameters
            .AddChildContent<RadzenColumnSeries<DualAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(DualAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(DualAxisItem.Revenue))
                .Add(p => p.YAxis, AxisY.Primary)
                .Add(p => p.Data, TallBarData))
            .AddChildContent<RadzenLineSeries<DualAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(DualAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(DualAxisItem.Rate))
                .Add(p => p.YAxis, AxisY.Secondary)
                .Add(p => p.Data, TallBarData))
            .AddChildContent<RadzenValueAxis>(a => a.Add(p => p.Position, ValueAxisPosition.Left).Add(p => p.Min, 0d).Add(p => p.Max, 10000d).Add(p => p.Step, 2000d))
            .AddChildContent<RadzenValueAxis>(a => a.Add(p => p.Position, ValueAxisPosition.Right).Add(p => p.Min, 0d).Add(p => p.Max, 100d).Add(p => p.Step, 20d)));

        var instance = chart.Instance;
        var line = instance.Series.Single(s => s.YAxis == AxisY.Secondary);
        var column = instance.Series.Single(s => s.YAxis == AxisY.Primary);

        // A line data point that sits well inside the (taller) column for that category.
        var item = TallBarData[2]; // Mar: column at ~95% height, line at 50%
        var linePoint = line.GetTooltipPosition(item);

        // Hovering the marker selects the line, even though the column also contains the point.
        var (hovered, _) = instance.FindClosestSeries(linePoint.X, linePoint.Y, 25);
        Assert.Same(line, hovered);

        // Hovering the bar away from the line (well below the marker) still selects the column.
        var (barHovered, _) = instance.FindClosestSeries(linePoint.X, linePoint.Y + 80, 25);
        Assert.Same(column, barHovered);
    }
}