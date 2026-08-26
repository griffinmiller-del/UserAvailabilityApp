using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using ToggleAvailabilityBlazor.Models;
using ToggleAvailabilityBlazor.Services;

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
    // Injected Services
    // ==================================================

    [Inject]
    private OfficeHistoryGraphService GraphService { get; set; } = null!;


    // ==================================================
    // Fields
    // ==================================================

    private List<GraphPoint> _graphData = [];

    private int _hoveredGraphPoint = -1;


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
            GraphService.GetGraphPoints(
                History,
                SelectedRange,
                CustomStartDate,
                CustomEndDate);
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
                GraphService.GetToday();


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

            double width =
                OfficeHistoryGraphService.GraphWidth;

            double height =
                OfficeHistoryGraphService.GraphHeight;

            double left =
                OfficeHistoryGraphService.GraphLeft;

            double right =
                OfficeHistoryGraphService.GraphRight;

            double top =
                OfficeHistoryGraphService.GraphTop;

            double bottom =
                OfficeHistoryGraphService.GraphBottom;


            double graphWidth =
                width -
                left -
                right;


            double graphHeight =
                height -
                top -
                bottom;


            double maxHours =
                GraphService.GetMaxHours(
                    _graphData);


            double step =
                GraphService.GetGraphStep(
                    _graphData.Count,
                    graphWidth);


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
                    GraphService.GetGridHours(
                        maxHours,
                        i,
                        gridLines);


                double y =
                    GraphService.GetGraphY(
                        hours,
                        maxHours,
                        top,
                        graphHeight);


                // --------------------------------------------------
                // Grid Line
                // --------------------------------------------------

                builder.OpenElement(
                    0,
                    "line");


                builder.AddAttribute(
                    1,
                    "x1",
                    left);


                builder.AddAttribute(
                    2,
                    "x2",
                    width - right);


                builder.AddAttribute(
                    3,
                    "y1",
                    y);


                builder.AddAttribute(
                    4,
                    "y2",
                    y);


                builder.AddAttribute(
                    5,
                    "stroke",
                    GraphGridColor);


                builder.AddAttribute(
                    6,
                    "stroke-width",
                    "1");


                builder.AddAttribute(
                    7,
                    "stroke-opacity",
                    "0.8");


                builder.CloseElement();


                // --------------------------------------------------
                // Grid Label
                // --------------------------------------------------

                builder.OpenElement(
                    0,
                    "text");


                builder.AddAttribute(
                    1,
                    "x",
                    left - 10);


                builder.AddAttribute(
                    2,
                    "y",
                    y + 4);


                builder.AddAttribute(
                    3,
                    "text-anchor",
                    "end");


                builder.AddAttribute(
                    4,
                    "fill",
                    GraphTitleColor);


                builder.AddAttribute(
                    5,
                    "font-size",
                    "15");


                builder.AddContent(
                    6,
                    $"{hours:0}h");


                builder.CloseElement();
            }


            // ==================================================
            // Area
            // ==================================================

            string areaPath =
                GraphService.BuildAreaPath(
                    _graphData,
                    left,
                    top,
                    graphHeight,
                    step,
                    maxHours);


            builder.OpenElement(
                0,
                "path");


            builder.AddAttribute(
                1,
                "d",
                areaPath);


            builder.AddAttribute(
                2,
                "fill",
                GraphYellow);


            builder.AddAttribute(
                3,
                "fill-opacity",
                GraphAreaOpacity);


            builder.AddAttribute(
                4,
                "stroke",
                "none");


            builder.AddAttribute(
                5,
                "pointer-events",
                "none");


            builder.CloseElement();


            // ==================================================
            // Line
            // ==================================================

            string linePath =
                GraphService.BuildLinePath(
                    _graphData,
                    left,
                    top,
                    graphHeight,
                    step,
                    maxHours);


            builder.OpenElement(
                0,
                "path");


            builder.AddAttribute(
                1,
                "d",
                linePath);


            builder.AddAttribute(
                2,
                "fill",
                "none");


            builder.AddAttribute(
                3,
                "stroke",
                GraphYellow);


            builder.AddAttribute(
                4,
                "stroke-width",
                "3");


            builder.AddAttribute(
                5,
                "stroke-linecap",
                "round");


            builder.AddAttribute(
                6,
                "stroke-linejoin",
                "round");


            builder.AddAttribute(
                7,
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
                    GraphService.GetGraphX(
                        i,
                        left,
                        step);


                double y =
                    GraphService.GetGraphY(
                        point.Hours,
                        maxHours,
                        top,
                        graphHeight);


                int pointIndex =
                    i;


                // --------------------------------------------------
                // Point Group
                // --------------------------------------------------

                builder.OpenElement(
                    0,
                    "g");


                builder.AddAttribute(
                    1,
                    "class",
                    "graph-point-group");


                // --------------------------------------------------
                // Point
                // --------------------------------------------------

                builder.OpenElement(
                    0,
                    "circle");


                builder.AddAttribute(
                    1,
                    "class",
                    "graph-point");


                builder.AddAttribute(
                    2,
                    "cx",
                    x);


                builder.AddAttribute(
                    3,
                    "cy",
                    y);


                builder.AddAttribute(
                    4,
                    "r",
                    _hoveredGraphPoint == pointIndex
                        ? 9
                        : 5);


                builder.AddAttribute(
                    5,
                    "fill",
                    GraphYellow);


                builder.AddAttribute(
                    6,
                    "stroke",
                    GraphYellow);


                builder.AddAttribute(
                    7,
                    "stroke-width",
                    "2");


                builder.AddAttribute(
                    8,
                    "filter",
                    "drop-shadow(0 0 5px rgba(242, 201, 76, 0.35))");


                builder.AddAttribute(
                    9,
                    "style",
                    "transition: r 0.15s ease; cursor: pointer;");


                // ==================================================
                // Mouse Enter
                // ==================================================

                builder.AddAttribute(
                    10,
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
                    11,
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
                    0,
                    "title");


                builder.AddContent(
                    1,
                    $"{point.Date:MMMM d, yyyy} — " +
                    $"{GraphService.FormatGraphTime(point.Hours)}");


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
                if (!GraphService.ShouldShowDateLabel(
                    i,
                    _graphData.Count))
                {
                    continue;
                }


                double x =
                    GraphService.GetGraphX(
                        i,
                        left,
                        step);


                builder.OpenElement(
                    0,
                    "text");


                builder.AddAttribute(
                    1,
                    "x",
                    x);


                builder.AddAttribute(
                    2,
                    "y",
                    height - 20);


                builder.AddAttribute(
                    3,
                    "text-anchor",
                    "middle");


                builder.AddAttribute(
                    4,
                    "fill",
                    GraphTitleColor);


                builder.AddAttribute(
                    5,
                    "font-size",
                    "15");


                builder.AddContent(
                    6,
                    GraphService.FormatGraphDate(
                        _graphData[i].Date,
                        SelectedRange));


                builder.CloseElement();
            }


            builder.CloseElement();
        };
    }


    // ==================================================
    // Graph Description
    // ==================================================

    private string GetGraphDateDescription()
    {
        return GraphService.GetGraphDateDescription(
            SelectedRange,
            CustomStartDate,
            CustomEndDate);
    }
}