using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Services;

public class OfficeHistoryService
{
    private readonly OfficeHistoryStore _officeHistoryStore;


    public OfficeHistoryService(
        OfficeHistoryStore officeHistoryStore)
    {
        _officeHistoryStore =
            officeHistoryStore;
    }


    // ==================================================
    // History
    // ==================================================

    /// <summary>
    /// Gets the office history for a specific user.
    /// </summary>
    public async Task<List<OfficeHistory>>
        GetUserHistoryAsync(
            int userId)
    {
        return await _officeHistoryStore
            .GetUserHistoryAsync(
                userId);
    }


    /// <summary>
    /// Gets the office history for a specific
    /// user and date.
    /// </summary>
    public async Task<OfficeHistory?>
        GetUserHistoryForDateAsync(
            int userId,
            DateOnly date)
    {
        return await _officeHistoryStore
            .GetUserHistoryForDateAsync(
                userId,
                date);
    }


    /// <summary>
    /// Gets the total recorded office time
    /// for a specific user.
    ///
    /// This does not include a currently active session.
    /// </summary>
    public async Task<TimeSpan>
        GetTotalOfficeTimeAsync(
            int userId)
    {
        return await _officeHistoryStore
            .GetTotalOfficeTimeAsync(
                userId);
    }


    /// <summary>
    /// Gets the recorded office time for a
    /// specific user and date.
    /// </summary>
    public async Task<TimeSpan>
        GetOfficeTimeForDateAsync(
            int userId,
            DateOnly date)
    {
        return await _officeHistoryStore
            .GetOfficeTimeForDateAsync(
                userId,
                date);
    }


    // ==================================================
    // Office Time
    // ==================================================

    /// <summary>
    /// Adds office time to a user's history.
    ///
    /// Durations that are zero or negative are ignored.
    /// </summary>
    public async Task AddOfficeTimeAsync(
        int userId,
        DateOnly date,
        TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }


        await _officeHistoryStore
            .AddOfficeTimeAsync(
                userId,
                date,
                duration);
    }


    // ==================================================
    // Out Of Office Time
    // ==================================================

    /// <summary>
    /// Adds out-of-office time for a specific
    /// user, date, and reason.
    ///
    /// Zero or negative durations are ignored.
    ///
    /// GoneForTheDay is not recorded as a timed
    /// out-of-office entry.
    /// </summary>
    public async Task AddOutOfOfficeTimeAsync(
        int userId,
        DateOnly date,
        Status reason,
        TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }


        if (reason == Status.GoneForTheDay)
        {
            return;
        }


        await _officeHistoryStore
            .AddOutOfOfficeTimeAsync(
                userId,
                date,
                reason,
                duration);
    }


    /// <summary>
    /// Gets the recorded out-of-office time grouped
    /// by reason for a specific user and date.
    /// </summary>
    public async Task<Dictionary<Status, TimeSpan>>
        GetOutOfOfficeTimeAsync(
            int userId,
            DateOnly date)
    {
        var entries =
            await _officeHistoryStore
                .GetOutOfOfficeEntriesAsync(
                    userId,
                    date);


        return entries.ToDictionary(
            x => x.Status,
            x => x.Duration);
    }


    /// <summary>
    /// Gets the total timed out-of-office duration
    /// for a specific user and date.
    ///
    /// GoneForTheDay is excluded because it represents
    /// a state rather than a timed out-of-office period.
    /// </summary>
    public async Task<TimeSpan>
        GetTotalOutOfOfficeTimeAsync(
            int userId,
            DateOnly date)
    {
        var entries =
            await _officeHistoryStore
                .GetOutOfOfficeEntriesAsync(
                    userId,
                    date);


        TimeSpan total =
            TimeSpan.Zero;


        foreach (var entry in entries)
        {
            if (entry.Status ==
                Status.GoneForTheDay)
            {
                continue;
            }


            total +=
                entry.Duration;
        }


        return total;
    }


    // ==================================================
    // Daily Records
    // ==================================================

    /// <summary>
    /// Creates a daily history record if one
    /// does not already exist.
    /// </summary>
    public async Task CreateDailyRecordAsync(
        int userId,
        DateOnly date,
        DateTime? startTime = null)
    {
        await _officeHistoryStore
            .CreateDailyRecordAsync(
                userId,
                date,
                startTime);
    }


    /// <summary>
    /// Sets the first punch-in time for a user
    /// on a specific day.
    ///
    /// An existing start time is never overwritten.
    /// </summary>
    public async Task SetStartTimeAsync(
        int userId,
        DateOnly date,
        DateTime startTime)
    {
        await _officeHistoryStore
            .SetStartTimeAsync(
                userId,
                date,
                startTime);
    }
}
