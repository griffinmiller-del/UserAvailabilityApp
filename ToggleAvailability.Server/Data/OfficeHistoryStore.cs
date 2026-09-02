using Microsoft.EntityFrameworkCore;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Data;

public class OfficeHistoryStore
{
    private readonly AppDbContext _db;


    public OfficeHistoryStore(
        AppDbContext db)
    {
        _db =
            db;
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
        return await _db.OfficeHistories
            .AsNoTracking()
            .Include(x =>
                x.OutOfOfficeEntries)
            .Where(x =>
                x.UserId == userId)
            .OrderByDescending(x =>
                x.Date)
            .ToListAsync();
    }


    /// <summary>
    /// Gets the total office time recorded
    /// for a specific user.
    ///
    /// This does not include a currently active session.
    /// </summary>
    public async Task<TimeSpan>
        GetTotalOfficeTimeAsync(
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


    /// <summary>
    /// Gets the office time recorded for a specific
    /// user and date.
    /// </summary>
    public async Task<TimeSpan>
        GetOfficeTimeForDateAsync(
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
    // Office Time
    // ==================================================

    /// <summary>
    /// Adds office time to a user's history.
    ///
    /// This method performs persistence only.
    /// Validation of the duration belongs to the
    /// service layer.
    /// </summary>
    public async Task AddOfficeTimeAsync(
        int userId,
        DateOnly date,
        TimeSpan duration)
    {
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
    // Out Of Office Time
    // ==================================================

    /// <summary>
    /// Adds out-of-office time for a specific
    /// user, date, and reason.
    ///
    /// This method performs persistence only.
    /// Validation of the reason and duration belongs
    /// to the service layer.
    /// </summary>
    public async Task AddOutOfOfficeTimeAsync(
        int userId,
        DateOnly date,
        Status reason,
        TimeSpan duration)
    {
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


    /// <summary>
    /// Gets all recorded out-of-office entries
    /// for a user on a specific date.
    /// </summary>
    public async Task<List<OfficeHistoryOutOfOffice>>
        GetOutOfOfficeEntriesAsync(
            int userId,
            DateOnly date)
    {
        return await _db.OfficeHistoryOutOfOffice
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.Date == date)
            .ToListAsync();
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


    /// <summary>
    /// Gets an office history record for a
    /// specific user and date.
    /// </summary>
    public async Task<OfficeHistory?>
        GetUserHistoryForDateAsync(
            int userId,
            DateOnly date)
    {
        return await _db.OfficeHistories
            .AsNoTracking()
            .Include(x =>
                x.OutOfOfficeEntries)
            .FirstOrDefaultAsync(
                x =>
                    x.UserId == userId &&
                    x.Date == date);
    }


    /// <summary>
    /// Sets the first punch-in time for a user
    /// on a specific day.
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
