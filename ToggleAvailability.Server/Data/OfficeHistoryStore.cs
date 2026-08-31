using Microsoft.EntityFrameworkCore;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Data;

public class OfficeHistoryStore
{
    private readonly AppDbContext _db;


    public OfficeHistoryStore(
        AppDbContext db)
    {
        _db = db;
    }


    // ==================================================
    // Get User History
    // ==================================================

    /// <summary>
    /// Gets the office history for a specific user.
    /// </summary>
    public async Task<List<OfficeHistory>> GetUserHistoryAsync(
        int userId)
    {
        return await _db.OfficeHistories
            .AsNoTracking()
            .Include(x => x.OutOfOfficeEntries)
            .Where(x =>
                x.UserId == userId)
            .OrderByDescending(x =>
                x.Date)
            .ToListAsync();
    }


    // ==================================================
    // Get Completed Office Time
    // ==================================================

    /// <summary>
    /// Gets the total office time that has already been
    /// committed to the database for a user.
    ///
    /// This does NOT include a currently active session.
    /// </summary>
    public async Task<TimeSpan> GetTotalOfficeTimeAsync(
        int userId)
    {
        long ticks =
            await _db.OfficeHistories
                .Where(x =>
                    x.UserId == userId)
                .Select(x =>
                    x.TimeInOffice.Ticks)
                .SumAsync();

        return TimeSpan.FromTicks(
            ticks);
    }


    // ==================================================
    // Get Completed Office Time For Date
    // ==================================================

    /// <summary>
    /// Gets the amount of office time already committed
    /// to history for a specific user and date.
    /// </summary>
    public async Task<TimeSpan> GetOfficeTimeForDateAsync(
        int userId,
        DateOnly date)
    {
        var record =
            await _db.OfficeHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.Date == date);


        return record?.TimeInOffice
            ?? TimeSpan.Zero;
    }


    // ==================================================
    // Add Office Time
    // ==================================================

    /// <summary>
    /// Adds completed office time to a user's history.
    ///
    /// This method only records completed time. It does
    /// not modify the user's currently active session.
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


        var existing =
            await _db.OfficeHistories
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.Date == date);


        if (existing is null)
        {
            existing =
                new OfficeHistory
                {
                    UserId =
                        userId,

                    Date =
                        date,

                    TimeInOffice =
                        duration,

                    StartTime =
                        null
                };


            _db.OfficeHistories.Add(
                existing);
        }
        else
        {
            existing.TimeInOffice +=
                duration;
        }


        await _db.SaveChangesAsync();
    }


    // ==================================================
    // Add Out-of-Office Time
    // ==================================================

    /// <summary>
    /// Adds out-of-office time for a specific user,
    /// date, and reason.
    ///
    /// GoneForTheDay is intentionally ignored.
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


        var history =
            await _db.OfficeHistories
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.Date == date);


        if (history is null)
        {
            history =
                new OfficeHistory
                {
                    UserId =
                        userId,

                    Date =
                        date,

                    TimeInOffice =
                        TimeSpan.Zero
                };


            _db.OfficeHistories.Add(
                history);
        }


        var existing =
            await _db.OfficeHistoryOutOfOffice
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.Date == date &&
                        x.Status == reason);


        if (existing is null)
        {
            existing =
                new OfficeHistoryOutOfOffice
                {
                    UserId =
                        userId,

                    Date =
                        date,

                    Status =
                        reason,

                    Duration =
                        duration
                };


            _db.OfficeHistoryOutOfOffice.Add(
                existing);
        }
        else
        {
            existing.Duration +=
                duration;
        }


        await _db.SaveChangesAsync();
    }


    // ==================================================
    // Get Out-of-Office Time
    // ==================================================

    /// <summary>
    /// Gets all recorded out-of-office time for a user
    /// on a specific date.
    /// </summary>
    public async Task<Dictionary<Status, TimeSpan>>
        GetOutOfOfficeTimeAsync(
            int userId,
            DateOnly date)
    {
        return await _db.OfficeHistoryOutOfOffice
            .AsNoTracking()
            .Where(
                x =>
                    x.UserId == userId &&
                    x.Date == date)
            .ToDictionaryAsync(
                x => x.Status,
                x => x.Duration);
    }


    // ==================================================
    // Get Total Out-of-Office Time
    // ==================================================

    /// <summary>
    /// Gets the total amount of recorded out-of-office
    /// time for a user on a specific date.
    /// </summary>
    public async Task<TimeSpan>
        GetTotalOutOfOfficeTimeAsync(
            int userId,
            DateOnly date)
    {
        long ticks =
            await _db.OfficeHistoryOutOfOffice
                .Where(
                    x =>
                        x.UserId == userId &&
                        x.Date == date &&
                        x.Status != Status.GoneForTheDay)
                .Select(
                    x =>
                        x.Duration.Ticks)
                .SumAsync();


        return TimeSpan.FromTicks(
            ticks);
    }


    // ==================================================
    // Create Daily Record
    // ==================================================

    /// <summary>
    /// Creates a daily history record if one does
    /// not already exist.
    /// </summary>
    public async Task CreateDailyRecordAsync(
        int userId,
        DateOnly date,
        DateTime? startTime = null)
    {
        var existing =
            await _db.OfficeHistories
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.Date == date);


        if (existing is not null)
        {
            return;
        }


        _db.OfficeHistories.Add(
            new OfficeHistory
            {
                UserId =
                    userId,

                Date =
                    date,

                TimeInOffice =
                    TimeSpan.Zero,

                StartTime =
                    startTime
            });


        await _db.SaveChangesAsync();
    }


    // ==================================================
    // Get History For Date
    // ==================================================

    /// <summary>
    /// Gets a copy of an office history record for a
    /// specific user and date.
    /// </summary>
    public async Task<OfficeHistory?>
        GetUserHistoryForDateAsync(
            int userId,
            DateOnly date)
    {
        return await _db.OfficeHistories
            .AsNoTracking()
            .Include(x => x.OutOfOfficeEntries)
            .FirstOrDefaultAsync(
                x =>
                    x.UserId == userId &&
                    x.Date == date);
    }


    // ==================================================
    // Set Start Time
    // ==================================================

    /// <summary>
    /// Sets the first punch-in time for a user on a day.
    ///
    /// Once a start time exists, it is never overwritten.
    /// </summary>
    public async Task SetStartTimeAsync(
        int userId,
        DateOnly date,
        DateTime startTime)
    {
        var existing =
            await _db.OfficeHistories
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.Date == date);


        if (existing is null)
        {
            _db.OfficeHistories.Add(
                new OfficeHistory
                {
                    UserId =
                        userId,

                    Date =
                        date,

                    TimeInOffice =
                        TimeSpan.Zero,

                    StartTime =
                        startTime
                });


            await _db.SaveChangesAsync();

            return;
        }


        if (existing.StartTime is null)
        {
            existing.StartTime =
                startTime;


            await _db.SaveChangesAsync();
        }
    }
}