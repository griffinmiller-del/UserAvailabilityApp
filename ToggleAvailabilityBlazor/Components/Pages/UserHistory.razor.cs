using Microsoft.AspNetCore.Components;
using ToggleAvailabilityBlazor.Models;
using ToggleAvailabilityBlazor.Services;

namespace ToggleAvailabilityBlazor.Components.Pages;

public partial class UserHistory
{
    [Parameter]
    public int UserId { get; set; }


    private User? _user;

    private List<OfficeHistory> _history = [];

    private List<OfficeHistory> _filteredHistory = [];

    private bool _loading = true;


    private DateOnly? _fromDate;

    private DateOnly? _toDate;


    // ==================================================
    // Expanded Entries
    // ==================================================

    private HashSet<DateOnly> _expandedDates = [];


    // ==================================================
    // Initialization
    // ==================================================

    protected override async Task OnInitializedAsync()
    {
        _user =
            AvailabilityService.Users
                .FirstOrDefault(
                    x =>
                        x.UserId == UserId);


        if (_user is not null)
        {
            _history =
                await AvailabilityService
                    .GetUserHistoryAsync(
                        UserId);


            _filteredHistory =
                _history
                    .OrderByDescending(
                        x =>
                            x.Date)
                    .ToList();
        }


        _loading = false;
    }


    // ==================================================
    // Toggle Expanded Entry
    // ==================================================

    private void ToggleExpanded(
        DateOnly date)
    {
        if (_expandedDates.Contains(date))
        {
            _expandedDates.Remove(date);
        }
        else
        {
            _expandedDates.Add(date);
        }
    }


    // ==================================================
    // Get Total Out of Office
    // ==================================================

    private static TimeSpan GetTotalOutOfOffice(
        OfficeHistory record)
    {
        if (record.OutOfOfficeEntries is null ||
            record.OutOfOfficeEntries.Count == 0)
        {
            return TimeSpan.Zero;
        }


        return record.OutOfOfficeEntries
            .Where(
                x =>
                    x.Status != Status.GoneForTheDay)
            .Select(
                x =>
                    x.Duration)
            .Aggregate(
                TimeSpan.Zero,
                (
                    total,
                    duration) =>
                    total + duration);
    }


    // ==================================================
    // Get Out of Office Entries
    // ==================================================

    private static IEnumerable<OfficeHistoryOutOfOffice>
        GetOutOfOfficeEntries(
            OfficeHistory record)
    {
        if (record.OutOfOfficeEntries is null)
        {
            return [];
        }


        return record.OutOfOfficeEntries
            .Where(
                x =>
                    x.Status != Status.GoneForTheDay)
            .OrderBy(
                x =>
                    x.Status);
    }


    // ==================================================
    // Get Out of Office Time For Status
    // ==================================================

    private static TimeSpan GetOutOfOfficeTime(
        OfficeHistory record,
        Status status)
    {
        if (record.OutOfOfficeEntries is null)
        {
            return TimeSpan.Zero;
        }


        return record.OutOfOfficeEntries
            .Where(
                x =>
                    x.Status == status)
            .Select(
                x =>
                    x.Duration)
            .Aggregate(
                TimeSpan.Zero,
                (
                    total,
                    duration) =>
                    total + duration);
    }


    // ==================================================
    // Search
    // ==================================================

    private void SearchHistory()
    {
        IEnumerable<OfficeHistory> results =
            _history;


        // ----------------------------------------------
        // From date
        // ----------------------------------------------

        if (_fromDate.HasValue)
        {
            results =
                results.Where(
                    x =>
                        x.Date >=
                        _fromDate.Value);
        }


        // ----------------------------------------------
        // To date
        // ----------------------------------------------

        if (_toDate.HasValue)
        {
            results =
                results.Where(
                    x =>
                        x.Date <=
                        _toDate.Value);
        }


        // ----------------------------------------------
        // Validate reversed range
        // ----------------------------------------------

        if (_fromDate.HasValue &&
            _toDate.HasValue &&
            _fromDate.Value > _toDate.Value)
        {
            _filteredHistory = [];

            _expandedDates.Clear();

            return;
        }


        _filteredHistory =
            results
                .OrderByDescending(
                    x =>
                        x.Date)
                .ToList();


        _expandedDates.Clear();
    }


    // ==================================================
    // Clear Search
    // ==================================================

    private void ClearSearch()
    {
        _fromDate = null;

        _toDate = null;

        _expandedDates.Clear();


        _filteredHistory =
            _history
                .OrderByDescending(
                    x =>
                        x.Date)
                .ToList();
    }


    // ==================================================
    // Format Duration
    // ==================================================

    private static string FormatDuration(
        TimeSpan duration)
    {
        int totalMinutes =
            (int)duration.TotalMinutes;


        int hours =
            totalMinutes / 60;


        int minutes =
            totalMinutes % 60;


        return
            $"{hours:00}:{minutes:00}";
    }


    // ==================================================
    // Format Status
    // ==================================================

    private static string FormatStatus(
        Status status)
    {
        return status switch
        {
            Status.GoneForTheDay =>
                "Gone for the Day",

            Status.ClientMeeting =>
                "Client Meeting",

            Status.InOffice =>
                "In Office",

            _ =>
                status.ToString()
        };
    }
}
