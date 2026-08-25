using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using ToggleAvailabilityBlazor.Models;

namespace ToggleAvailabilityBlazor.Components.Graph;

public partial class OfficeHistoryGraph : ComponentBase
{
    // ==================================================
    // Parameters
    // ==================================================

    [Parameter]
    public List<OfficeHistory> History { get; set; } = [];


    [Parameter]
    public GraphRange SelectedRange { get; set; } =
        GraphRange.Week;


    [Parameter]
    public EventCallback<GraphRange> SelectedRangeChanged { get; set; }


    [Parameter]
    public DateOnly CustomStartDate { get; set; } =
        DateOnly.FromDateTime(
            DateTime.Now.AddDays(-6));


    [Parameter]
    public EventCallback<DateOnly> CustomStartDateChanged { get; set; }


    [Parameter]
    public DateOnly CustomEndDate { get; set; } =
        DateOnly.FromDateTime(
            DateTime.Now);


    [Parameter]
    public EventCallback<DateOnly> CustomEndDateChanged { get; set; }


    // ==================================================
    // Fields
    // ==================================================

    private List<GraphPoint> _graphData = [];

    private int _hoveredGraphPoint = -1;


    // ==================================================
    // Constants
    // ==================================================

    private const double GraphWidth = 1500;

    private const double GraphHeight = 350;

    private const double GraphLeft = 65;

    private const double GraphRight = 25;

    private const double GraphTop = 25;

    private const double GraphBottom = 55;


    // ==================================================
    // Parameters Changed
    // ==================================================

    protected override void OnParametersSet()
    {
        UpdateGraphData();
    }


    // ==================================================
    // Update Graph
    // ==================================================

    private void UpdateGraphData()
    {
        _graphData =
            GetGraphPoints();
    }


    // ==================================================
    // Set Range
    // ==================================================

    private async Task SetRange(
        GraphRange newRange)
    {
        if (SelectedRange == newRange)
        {
            return;
        }


        if (newRange == GraphRange.Custom)
        {
            DateOnly today =
                DateOnly.FromDateTime(
                    DateTime.Now);


            DateOnly start =
                today.AddDays(-6);


            DateOnly end =
                today;


            await CustomStartDateChanged.InvokeAsync(
                start);

            await CustomEndDateChanged.InvokeAsync(
                end);
        }


        await SelectedRangeChanged.InvokeAsync(
            newRange);
    }


    // ==================================================
    // Custom Dates
    // ==================================================

    private async Task CustomDateChanged()
    {
        if (CustomEndDate < CustomStartDate)
        {
            return;
        }


        UpdateGraphData();


        await InvokeAsync(
            StateHasChanged);
    }


    // ==================================================
    // Range Start
    // ==================================================

    private DateOnly GetRangeStartDate(
        GraphRange range)
    {
        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Now);


        return range switch
        {
            GraphRange.Week =>
                today.AddDays(-6),

            GraphRange.Month =>
                today.AddDays(-29),

            GraphRange.Year =>
                today.AddDays(-364),

            GraphRange.Custom =>
                CustomStartDate,

            _ =>
                today.AddDays(-6)
        };
    }


    // ==================================================
    // Range End
    // ==================================================

    private DateOnly GetRangeEndDate(
        GraphRange range)
    {
        if (range == GraphRange.Custom)
        {
            return CustomEndDate;
        }


        return DateOnly.FromDateTime(
            DateTime.Now);
    }


    // ==================================================
    // Get Graph Points
    // ==================================================

    private List<GraphPoint> GetGraphPoints()
    {
        DateOnly startDate =
            GetRangeStartDate(
                SelectedRange);


        DateOnly endDate =
            GetRangeEndDate(
                SelectedRange);


        if (endDate < startDate)
        {
            return [];
        }


        var history =
            History
                .GroupBy(x => x.Date)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(
                        y => y.TimeInOffice.TotalHours));


        var points =
            new List<GraphPoint>();


        for (
            DateOnly date = startDate;
            date <= endDate;
            date = date.AddDays(1))
        {
            history.TryGetValue(
                date,
                out double hours);


            points.Add(
                new GraphPoint
                {
                    Date = date,
                    Hours = hours
                });
        }


        return points;
    }


    // ==================================================
    // Build Graph
    // ==================================================

    private RenderFragment BuildGraph()
    {
        return builder =>
        {
            if (_graphData.Count == 0)
            {
                return;
            }


            // ==================================================
            // Graph Colors
            // ==================================================

            const string GraphYellow =
                "#f2c94c";

            const string GraphTitleColor =
                "#ffffff";

            const string GraphGridColor =
                "#303030";


            const double GraphAreaOpacity =
                0.12;


            // ==================================================
            // Dimensions
            // ==================================================

            const double width =
                GraphWidth;

            const double height =
                GraphHeight;

            const double left =
                GraphLeft;

            const double right =
                GraphRight;

            const double top =
                GraphTop;

            const double bottom =
                GraphBottom;


            double graphWidth =
                width -
                left -
                right;


            double graphHeight =
                height -
                top -
                bottom;


            double maxHours =
                Math.Max(
                    8,
                    Math.Ceiling(
                        _graphData.Max(
                            x => x.Hours)));


            double step =
                _graphData.Count <= 1
                    ? 0
                    : graphWidth /
                      (_graphData.Count - 1);


            double GetX(
                int index)
            {
                return
                    left +
                    index *
                    step;
            }


            double GetY(
                double hours)
            {
                return
                    top +
                    graphHeight -
                    (
                        hours /
                        maxHours *
                        graphHeight
                    );
            }


            // ==================================================
            // Line Path
            // ==================================================

            string BuildLinePath()
            {
                string path = "";


                for (
                    int i = 0;
                    i < _graphData.Count;
                    i++)
                {
                    double x =
                        GetX(i);


                    double y =
                        GetY(
                            _graphData[i].Hours);


                    path +=
                        i == 0
                            ? $"M {x:F2} {y:F2}"
                            : $" L {x:F2} {y:F2}";
                }


                return path;
            }


            // ==================================================
            // Area Path
            // ==================================================

            string BuildAreaPath()
            {
                if (_graphData.Count == 0)
                {
                    return "";
                }


                double baseline =
                    top +
                    graphHeight;


                string path =
                    $"M {left:F2} " +
                    $"{baseline:F2}";


                path +=
                    $" L {GetX(0):F2} " +
                    $"{GetY(_graphData[0].Hours):F2}";


                for (
                    int i = 1;
                    i < _graphData.Count;
                    i++)
                {
                    path +=
                        $" L {GetX(i):F2} " +
                        $"{GetY(_graphData[i].Hours):F2}";
                }


                double lastX =
                    GetX(
                        _graphData.Count - 1);


                path +=
                    $" L {lastX:F2} " +
                    $"{baseline:F2}";


                path += " Z";


                return path;
            }


            // ==================================================
            // SVG
            // ==================================================

            builder.OpenElement(
                0,
                "svg");


            builder.AddAttribute(
                1,
                "class",
                "office-history-graph");


            builder.AddAttribute(
                2,
                "viewBox",
                $"0 0 {width} {height}");


            builder.AddAttribute(
                3,
                "preserveAspectRatio",
                "xMidYMid meet");


            // ==================================================
            // Grid
            // ==================================================

            const int gridLines = 6;


            for (
                int i = 0;
                i <= gridLines;
                i++)
            {
                double hours =
                    maxHours *
                    i /
                    gridLines;


                double y =
                    top +
                    graphHeight -
                    (
                        i *
                        graphHeight /
                        gridLines
                    );


                int gridSequence =
                    1000 +
                    i * 10;


                builder.OpenElement(
                    gridSequence,
                    "line");


                builder.AddAttribute(
                    gridSequence + 1,
                    "x1",
                    left);


                builder.AddAttribute(
                    gridSequence + 2,
                    "x2",
                    width - right);


                builder.AddAttribute(
                    gridSequence + 3,
                    "y1",
                    y);


                builder.AddAttribute(
                    gridSequence + 4,
                    "y2",
                    y);


                builder.AddAttribute(
                    gridSequence + 5,
                    "stroke",
                    GraphGridColor);


                builder.AddAttribute(
                    gridSequence + 6,
                    "stroke-width",
                    "1");


                builder.AddAttribute(
                    gridSequence + 7,
                    "stroke-opacity",
                    "0.8");


                builder.CloseElement();


                int labelSequence =
                    1100 +
                    i * 10;


                builder.OpenElement(
                    labelSequence,
                    "text");


                builder.AddAttribute(
                    labelSequence + 1,
                    "x",
                    left - 10);


                builder.AddAttribute(
                    labelSequence + 2,
                    "y",
                    y + 4);


                builder.AddAttribute(
                    labelSequence + 3,
                    "text-anchor",
                    "end");


                builder.AddAttribute(
                    labelSequence + 4,
                    "fill",
                    GraphTitleColor);


                builder.AddAttribute(
                    labelSequence + 5,
                    "font-size",
                    "15");


                builder.AddContent(
                    labelSequence + 6,
                    $"{hours:0}h");


                builder.CloseElement();
            }


            // ==================================================
            // Area
            // ==================================================

            builder.OpenElement(
                2000,
                "path");


            builder.AddAttribute(
                2001,
                "d",
                BuildAreaPath());


            builder.AddAttribute(
                2002,
                "fill",
                GraphYellow);


            builder.AddAttribute(
                2003,
                "fill-opacity",
                GraphAreaOpacity);


            builder.AddAttribute(
                2004,
                "stroke",
                "none");


            builder.AddAttribute(
                2005,
                "pointer-events",
                "none");


            builder.CloseElement();


            // ==================================================
            // Line
            // ==================================================

            builder.OpenElement(
                2100,
                "path");


            builder.AddAttribute(
                2101,
                "d",
                BuildLinePath());


            builder.AddAttribute(
                2102,
                "fill",
                "none");


            builder.AddAttribute(
                2103,
                "stroke",
                GraphYellow);


            builder.AddAttribute(
                2104,
                "stroke-width",
                "3");


            builder.AddAttribute(
                2105,
                "stroke-linecap",
                "round");


            builder.AddAttribute(
                2106,
                "stroke-linejoin",
                "round");


            builder.AddAttribute(
                2107,
                "filter",
                "drop-shadow(0 0 5px rgba(242, 201, 76, 0.25))");


            builder.CloseElement();


            // ==================================================
            // Points
            // ==================================================

            for (
                int i = 0;
                i < _graphData.Count;
                i++)
            {
                GraphPoint point =
                    _graphData[i];


                double x =
                    GetX(i);


                double y =
                    GetY(
                        point.Hours);


                int pointIndex =
                    i;


                int sequence =
                    3000 +
                    (i * 100);


                builder.OpenElement(
                    sequence,
                    "g");


                builder.AddAttribute(
                    sequence + 1,
                    "class",
                    "graph-point-group");


                builder.OpenElement(
                    sequence + 10,
                    "circle");


                builder.AddAttribute(
                    sequence + 11,
                    "class",
                    "graph-point");


                builder.AddAttribute(
                    sequence + 12,
                    "cx",
                    x);


                builder.AddAttribute(
                    sequence + 13,
                    "cy",
                    y);


                builder.AddAttribute(
                    sequence + 14,
                    "r",
                    _hoveredGraphPoint == pointIndex
                        ? 9
                        : 5);


                builder.AddAttribute(
                    sequence + 15,
                    "fill",
                    GraphYellow);


                builder.AddAttribute(
                    sequence + 16,
                    "stroke",
                    GraphYellow);


                builder.AddAttribute(
                    sequence + 17,
                    "stroke-width",
                    "2");


                builder.AddAttribute(
                    sequence + 18,
                    "filter",
                    "drop-shadow(0 0 5px rgba(242, 201, 76, 0.35))");


                builder.AddAttribute(
                    sequence + 19,
                    "style",
                    "transition: r 0.15s ease; cursor: pointer;");


                // ==================================================
                // Mouse Enter
                // ==================================================

                builder.AddAttribute(
                    sequence + 20,
                    "onmouseenter",
                    EventCallback.Factory.Create<MouseEventArgs>(
                        this,
                        () =>
                        {
                            _hoveredGraphPoint =
                                pointIndex;

                            StateHasChanged();
                        }));


                // ==================================================
                // Mouse Leave
                // ==================================================

                builder.AddAttribute(
                    sequence + 21,
                    "onmouseleave",
                    EventCallback.Factory.Create<MouseEventArgs>(
                        this,
                        () =>
                        {
                            _hoveredGraphPoint =
                                -1;

                            StateHasChanged();
                        }));


                // ==================================================
                // Tooltip
                // ==================================================

                builder.OpenElement(
                    sequence + 30,
                    "title");


                builder.AddContent(
                    sequence + 31,
                    $"{point.Date:MMMM d, yyyy} — " +
                    $"{FormatGraphTime(point.Hours)}");


                builder.CloseElement();


                builder.CloseElement();


                builder.CloseElement();
            }


            // ==================================================
            // Date Labels
            // ==================================================

            for (
                int i = 0;
                i < _graphData.Count;
                i++)
            {
                if (!ShouldShowDateLabel(
                    i,
                    _graphData.Count))
                {
                    continue;
                }


                double x =
                    GetX(i);


                int sequence =
                    5000 +
                    (i * 10);


                builder.OpenElement(
                    sequence,
                    "text");


                builder.AddAttribute(
                    sequence + 1,
                    "x",
                    x);


                builder.AddAttribute(
                    sequence + 2,
                    "y",
                    height - 20);


                builder.AddAttribute(
                    sequence + 3,
                    "text-anchor",
                    "middle");


                builder.AddAttribute(
                    sequence + 4,
                    "fill",
                    GraphTitleColor);


                builder.AddAttribute(
                    sequence + 5,
                    "font-size",
                    "15");


                builder.AddContent(
                    sequence + 6,
                    FormatGraphDate(
                        _graphData[i].Date));


                builder.CloseElement();
            }


            builder.CloseElement();
        };
    }


    // ==================================================
    // Date Labels
    // ==================================================

    private static bool ShouldShowDateLabel(
        int index,
        int pointCount)
    {
        if (pointCount <= 14)
        {
            return true;
        }


        if (pointCount <= 45)
        {
            return
                index == 0 ||
                index == pointCount - 1 ||
                index % 4 == 0;
        }


        int interval =
            Math.Max(
                1,
                pointCount / 12);


        return
            index == 0 ||
            index == pointCount - 1 ||
            index % interval == 0;
    }


    // ==================================================
    // Format Graph Time
    // ==================================================

    private static string FormatGraphTime(
        double hours)
    {
        TimeSpan time =
            TimeSpan.FromHours(
                hours);


        return
            $"{(int)time.TotalHours:00}:" +
            $"{time.Minutes:00}:" +
            $"{time.Seconds:00}";
    }


    // ==================================================
    // Format Graph Date
    // ==================================================

    private string FormatGraphDate(
        DateOnly date)
    {
        return SelectedRange switch
        {
            GraphRange.Year =>
                date.ToString("MMM"),

            _ =>
                date.ToString("MMM d")
        };
    }


    // ==================================================
    // Graph Description
    // ==================================================

    private string GetGraphDateDescription()
    {
        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Now);


        return SelectedRange switch
        {
            GraphRange.Week =>
                $"{today.AddDays(-6):MMM d} – " +
                $"{today:MMM d, yyyy}",

            GraphRange.Month =>
                $"{today.AddDays(-29):MMM d} – " +
                $"{today:MMM d, yyyy}",

            GraphRange.Year =>
                $"{today.AddDays(-364):MMM d, yyyy} – " +
                $"{today:MMM d, yyyy}",

            GraphRange.Custom =>
                $"{CustomStartDate:MMM d, yyyy} – " +
                $"{CustomEndDate:MMM d, yyyy}",

            _ =>
                ""
        };
    }
}