using ToggleAvailabilityBlazor.Models;

namespace ToggleAvailabilityBlazor.Components.Graph;

public class OfficeHistoryGraphService
{
    // ==================================================
    // Graph Dimensions
    // ==================================================

    public const double GraphWidth = 1500;

    public const double GraphHeight = 350;

    public const double GraphLeft = 65;

    public const double GraphRight = 25;

    public const double GraphTop = 25;

    public const double GraphBottom = 55;


    // ==================================================
    // Graph Settings
    // ==================================================

    public const int GridLines = 6;


    // ==================================================
    // Get Today
    // ==================================================

    public DateOnly GetToday()
    {
        return DateOnly.FromDateTime(
            DateTime.Now);
    }


    // ==================================================
    // Get Range Start Date
    // ==================================================

    public DateOnly GetRangeStartDate(
        GraphRange range,
        DateOnly customStartDate)
    {
        DateOnly today =
            GetToday();


        return range switch
        {
            GraphRange.Week =>
                today.AddDays(-6),

            GraphRange.Month =>
                today.AddDays(-29),

            GraphRange.Year =>
                today.AddDays(-364),

            GraphRange.Custom =>
                customStartDate,

            _ =>
                today.AddDays(-6)
        };
    }


    // ==================================================
    // Get Range End Date
    // ==================================================

    public DateOnly GetRangeEndDate(
        GraphRange range,
        DateOnly customEndDate)
    {
        if (range == GraphRange.Custom)
        {
            return customEndDate;
        }


        return GetToday();
    }


    // ==================================================
    // Get Graph Points
    // ==================================================

    public List<GraphPoint> GetGraphPoints(
        List<OfficeHistory> history,
        GraphRange range,
        DateOnly customStartDate,
        DateOnly customEndDate)
    {
        DateOnly startDate =
            GetRangeStartDate(
                range,
                customStartDate);


        DateOnly endDate =
            GetRangeEndDate(
                range,
                customEndDate);


        if (endDate < startDate)
        {
            return [];
        }


        Dictionary<DateOnly, double> historyLookup =
            BuildHistoryLookup(
                history);


        var points =
            new List<GraphPoint>();


        for (
            DateOnly date = startDate;
            date <= endDate;
            date = date.AddDays(1))
        {
            historyLookup.TryGetValue(
                date,
                out double hours);


            points.Add(
                new GraphPoint
                {
                    Date =
                        date,

                    Hours =
                        hours
                });
        }


        return points;
    }


    // ==================================================
    // Build History Lookup
    // ==================================================

    public Dictionary<DateOnly, double> BuildHistoryLookup(
        List<OfficeHistory> history)
    {
        return history
            .GroupBy(
                x => x.Date)
            .ToDictionary(
                x => x.Key,
                x => x.Sum(
                    y =>
                        y.TimeInOffice.TotalHours));
    }


    // ==================================================
    // Get Maximum Graph Hours
    // ==================================================

    public double GetMaxHours(
        List<GraphPoint> graphData)
    {
        if (graphData.Count == 0)
        {
            return 8;
        }


        return Math.Max(
            8,
            Math.Ceiling(
                graphData.Max(
                    x => x.Hours)));
    }


    // ==================================================
    // Get Graph Width
    // ==================================================

    public double GetGraphWidth()
    {
        return
            GraphWidth -
            GraphLeft -
            GraphRight;
    }


    // ==================================================
    // Get Graph Height
    // ==================================================

    public double GetGraphHeight()
    {
        return
            GraphHeight -
            GraphTop -
            GraphBottom;
    }


    // ==================================================
    // Get Graph Step
    // ==================================================

    public double GetGraphStep(
        int pointCount,
        double graphWidth)
    {
        if (pointCount <= 1)
        {
            return 0;
        }


        return
            graphWidth /
            (pointCount - 1);
    }


    // ==================================================
    // Get Graph X Coordinate
    // ==================================================

    public double GetGraphX(
        int index,
        double left,
        double step)
    {
        return
            left +
            index *
            step;
    }


    // ==================================================
    // Get Graph Y Coordinate
    // ==================================================

    public double GetGraphY(
        double hours,
        double maxHours,
        double top,
        double graphHeight)
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
    // Get Grid Hours
    // ==================================================

    public double GetGridHours(
        double maxHours,
        int index,
        int gridLines)
    {
        return
            maxHours *
            index /
            gridLines;
    }


    // ==================================================
    // Get Grid Y Coordinate
    // ==================================================

    public double GetGridY(
        double maxHours,
        int index,
        int gridLines,
        double top,
        double graphHeight)
    {
        return
            top +
            graphHeight -
            (
                index *
                graphHeight /
                gridLines
            );
    }


    // ==================================================
    // Build Line Path
    // ==================================================

    public string BuildLinePath(
        List<GraphPoint> graphData,
        double left,
        double top,
        double graphHeight,
        double step,
        double maxHours)
    {
        if (graphData.Count == 0)
        {
            return "";
        }


        string path = "";


        for (
            int i = 0;
            i < graphData.Count;
            i++)
        {
            double x =
                GetGraphX(
                    i,
                    left,
                    step);


            double y =
                GetGraphY(
                    graphData[i].Hours,
                    maxHours,
                    top,
                    graphHeight);


            path +=
                i == 0
                    ? $"M {x:F2} {y:F2}"
                    : $" L {x:F2} {y:F2}";
        }


        return path;
    }


    // ==================================================
    // Build Area Path
    // ==================================================

    public string BuildAreaPath(
        List<GraphPoint> graphData,
        double left,
        double top,
        double graphHeight,
        double step,
        double maxHours)
    {
        if (graphData.Count == 0)
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
            $" L {GetGraphX(0, left, step):F2} " +
            $"{GetGraphY(
                graphData[0].Hours,
                maxHours,
                top,
                graphHeight):F2}";


        for (
            int i = 1;
            i < graphData.Count;
            i++)
        {
            path +=
                $" L {GetGraphX(
                    i,
                    left,
                    step):F2} " +
                $"{GetGraphY(
                    graphData[i].Hours,
                    maxHours,
                    top,
                    graphHeight):F2}";
        }


        double lastX =
            GetGraphX(
                graphData.Count - 1,
                left,
                step);


        path +=
            $" L {lastX:F2} " +
            $"{baseline:F2}";


        path +=
            " Z";


        return path;
    }


    // ==================================================
    // Should Show Date Label
    // ==================================================

    public bool ShouldShowDateLabel(
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

    public string FormatGraphTime(
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

    public string FormatGraphDate(
        DateOnly date,
        GraphRange selectedRange)
    {
        return selectedRange switch
        {
            GraphRange.Year =>
                date.ToString("MMM"),

            _ =>
                date.ToString("MMM d")
        };
    }


    // ==================================================
    // Get Graph Date Description
    // ==================================================

    public string GetGraphDateDescription(
        GraphRange selectedRange,
        DateOnly customStartDate,
        DateOnly customEndDate)
    {
        DateOnly today =
            GetToday();


        return selectedRange switch
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
                $"{customStartDate:MMM d, yyyy} – " +
                $"{customEndDate:MMM d, yyyy}",

            _ =>
                ""
        };
    }
}