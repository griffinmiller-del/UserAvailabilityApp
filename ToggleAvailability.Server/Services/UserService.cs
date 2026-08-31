using Microsoft.EntityFrameworkCore;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Services;

public class UserService
{
    private readonly AppDbContext _db;

    private readonly OfficeHistoryStore
        _officeHistoryStore;


    public UserService(
        AppDbContext db,
        OfficeHistoryStore officeHistoryStore)
    {
        _db =
            db;

        _officeHistoryStore =
            officeHistoryStore;
    }


    // ==================================================
    // Get Users
    // ==================================================

    public async Task<List<User>> GetUsersAsync()
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(x => x.UserId)
            .ToListAsync();
    }


    // ==================================================
    // Get Active Users
    // ==================================================

    public async Task<List<User>> GetActiveUsersAsync()
    {
        var users =
            await _db.Users
                .AsNoTracking()
                .Where(x => x.IsActiveUser)
                .OrderBy(x => x.UserId)
                .ToListAsync();


        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Now);


        foreach (var user in users)
        {
            user.TotalTimeInOffice =
                await _officeHistoryStore
                    .GetOfficeTimeForDateAsync(
                        user.UserId,
                        today);
        }


        return users;
    }


    // ==================================================
    // Get User
    // ==================================================

    public async Task<User?> GetUserAsync(
        int userId)
    {
        return await _db.Users
            .FirstOrDefaultAsync(
                x =>
                    x.UserId == userId);
    }


    // ==================================================
    // Add User
    // ==================================================

    public async Task AddUserAsync(
        User user)
    {
        ArgumentNullException.ThrowIfNull(
            user);


        _db.Users.Add(
            user);


        await _db.SaveChangesAsync();
    }


    // ==================================================
    // Update User
    // ==================================================

    public async Task UpdateUserAsync(
        User user)
    {
        ArgumentNullException.ThrowIfNull(
            user);


        var existingUser =
            await _db.Users
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId ==
                        user.UserId);


        if (existingUser is null)
        {
            throw new InvalidOperationException(
                $"User {user.UserId} was not found.");
        }


        existingUser.Name =
            user.Name;

        existingUser.IsAvailable =
            user.IsAvailable;

        existingUser.Status =
            user.Status;

        existingUser.InOfficeStartTime =
            user.InOfficeStartTime;

        existingUser.OutOfOfficeStartTime =
            user.OutOfOfficeStartTime;

        existingUser.IsActiveUser =
            user.IsActiveUser;


        await _db.SaveChangesAsync();
    }


    // ==================================================
    // Deactivate User
    // ==================================================

    public async Task<User> DeactivateUserAsync(
        int userId)
    {
        var user =
            await _db.Users
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId);


        if (user is null)
        {
            throw new InvalidOperationException(
                $"User {userId} was not found.");
        }


        DateTime now =
            DateTime.Now;


        // --------------------------------------------------
        // End active office session.
        // --------------------------------------------------

        if (user.InOfficeStartTime is not null)
        {
            await EndOfficeSessionAsync(
                user,
                now);
        }


        // --------------------------------------------------
        // End active out-of-office session.
        // --------------------------------------------------

        if (user.OutOfOfficeStartTime is not null)
        {
            await EndOutOfOfficeSessionAsync(
                user,
                now);
        }


        // --------------------------------------------------
        // Deactivate.
        // --------------------------------------------------

        user.IsActiveUser =
            false;

        user.IsAvailable =
            false;

        user.Status =
            Status.GoneForTheDay;

        user.InOfficeStartTime =
            null;

        user.OutOfOfficeStartTime =
            null;


        await _db.SaveChangesAsync();


        return user;
    }


    // ==================================================
    // Reactivate User
    // ==================================================

    public async Task ReactivateUserAsync(
        int userId)
    {
        var user =
            await _db.Users
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId);


        if (user is null)
        {
            throw new InvalidOperationException(
                $"User {userId} was not found.");
        }


        user.IsActiveUser =
            true;

        user.IsAvailable =
            false;

        user.Status =
            Status.GoneForTheDay;

        user.InOfficeStartTime =
            null;

        user.OutOfOfficeStartTime =
            null;


        await _db.SaveChangesAsync();
    }


    // ==================================================
    // Replace Users
    // ==================================================

    public async Task ReplaceUsersAsync(
        List<User> users)
    {
        ArgumentNullException.ThrowIfNull(
            users);


        var existingUsers =
            await _db.Users
                .ToListAsync();


        var submittedIds =
            users
                .Select(x => x.UserId)
                .ToHashSet();


        foreach (var submittedUser in users)
        {
            var existingUser =
                existingUsers.FirstOrDefault(
                    x =>
                        x.UserId ==
                        submittedUser.UserId);


            if (existingUser is null)
            {
                var newUser =
                    new User
                    {
                        UserId =
                            submittedUser.UserId,

                        Name =
                            submittedUser.Name,

                        IsAvailable =
                            false,

                        Status =
                            Status.GoneForTheDay,

                        InOfficeStartTime =
                            null,

                        OutOfOfficeStartTime =
                            null,

                        IsActiveUser =
                            true
                    };


                _db.Users.Add(
                    newUser);


                continue;
            }


            existingUser.Name =
                submittedUser.Name;

            existingUser.IsActiveUser =
                submittedUser.IsActiveUser;
        }


        foreach (var existingUser in existingUsers)
        {
            if (!submittedIds.Contains(
                    existingUser.UserId))
            {
                existingUser.IsActiveUser =
                    false;

                existingUser.IsAvailable =
                    false;

                existingUser.Status =
                    Status.GoneForTheDay;

                existingUser.InOfficeStartTime =
                    null;

                existingUser.OutOfOfficeStartTime =
                    null;
            }
        }


        await _db.SaveChangesAsync();
    }


    // ==================================================
    // Get Next User ID
    // ==================================================

    public async Task<int> GetNextUserIdAsync()
    {
        int maxUserId =
            await _db.Users
                .Select(x => (int?)x.UserId)
                .MaxAsync()
                ?? 0;


        return maxUserId + 1;
    }


    // ==================================================
    // User Name Validation
    // ==================================================

    public async Task<bool> ActiveUserNameExistsAsync(
        string name,
        int? excludedUserId = null)
    {
        return await _db.Users
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.IsActiveUser &&
                    x.UserId != excludedUserId &&
                    x.Name.Trim() ==
                        name.Trim());
    }


    // ==================================================
    // Set Availability
    // ==================================================

    public async Task<User> SetAvailabilityAsync(
        int userId,
        bool isAvailable,
        Status status)
    {
        var user =
            await _db.Users
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId);


        if (user is null)
        {
            throw new InvalidOperationException(
                $"User {userId} was not found.");
        }


        if (!user.IsActiveUser)
        {
            throw new InvalidOperationException(
                $"User {userId} is inactive.");
        }


        DateTime now =
            DateTime.Now;


        DateOnly today =
            DateOnly.FromDateTime(now);


        Status previousStatus =
            user.Status;


        bool wasInOffice =
            previousStatus == Status.InOffice;


        bool isInOffice =
            status == Status.InOffice;


        bool wasOutOfOffice =
            !wasInOffice;


        bool isOutOfOffice =
            !isInOffice;


        // ==================================================
        // IMPORTANT
        //
        // TotalTimeInOffice is NOT used to calculate history.
        //
        // OfficeHistory is the authoritative source for all
        // completed office time.
        //
        // Load today's completed time before changing state.
        // ==================================================

        user.TotalTimeInOffice =
            await _officeHistoryStore
                .GetOfficeTimeForDateAsync(
                    user.UserId,
                    today);


        // ==================================================
        // Leaving office
        // ==================================================

        if (wasInOffice &&
            isOutOfOffice)
        {
            await EndOfficeSessionAsync(
                user,
                now);


            StartOutOfOfficeSession(
                user,
                status,
                now);
        }


        // ==================================================
        // Changing out-of-office reason
        // ==================================================

        else if (
            wasOutOfOffice &&
            isOutOfOffice &&
            previousStatus != status)
        {
            await EndOutOfOfficeSessionAsync(
                user,
                now);


            StartOutOfOfficeSession(
                user,
                status,
                now);
        }


        // ==================================================
        // Returning to office
        // ==================================================

        else if (
            wasOutOfOffice &&
            isInOffice)
        {
            await EndOutOfOfficeSessionAsync(
                user,
                now);


            await StartOfficeSessionAsync(
                user,
                now);
        }


        // ==================================================
        // Update current state
        // ==================================================

        user.IsAvailable =
            isAvailable;

        user.Status =
            status;


        // ==================================================
        // Recalculate completed office time
        //
        // EndOfficeSessionAsync may have just written
        // additional time to OfficeHistory.
        //
        // Reload it so TotalTimeInOffice is always accurate.
        // ==================================================

        user.TotalTimeInOffice =
            await _officeHistoryStore
                .GetOfficeTimeForDateAsync(
                    user.UserId,
                    today);


        await _db.SaveChangesAsync();


        return user;
    }




    // ==================================================
    // Record Office Session
    // ==================================================

    private async Task RecordOfficeSessionAsync(
        User user,
        DateTime endTime)
    {
        if (user.InOfficeStartTime is null)
        {
            return;
        }


        DateTime startTime =
            user.InOfficeStartTime.Value;


        if (endTime <= startTime)
        {
            return;
        }


        DateTime current =
            startTime;


        // --------------------------------------------------
        // Split the session across midnight.
        // --------------------------------------------------

        while (current.Date < endTime.Date)
        {
            DateTime midnight =
                current.Date.AddDays(1);


            TimeSpan duration =
                midnight - current;


            await _officeHistoryStore
                .AddOfficeTimeAsync(
                    user.UserId,
                    DateOnly.FromDateTime(current),
                    duration);


            current =
                midnight;
        }


        // --------------------------------------------------
        // Record remaining time on final day.
        // --------------------------------------------------

        if (current < endTime)
        {
            TimeSpan duration =
                endTime - current;


            await _officeHistoryStore
                .AddOfficeTimeAsync(
                    user.UserId,
                    DateOnly.FromDateTime(current),
                    duration);
        }
    }
    // ==================================================
    // End Office Session
    // ==================================================

    private async Task EndOfficeSessionAsync(
        User user,
        DateTime endTime)
    {
        if (user.InOfficeStartTime is null)
        {
            return;
        }


        DateTime startTime =
            user.InOfficeStartTime.Value;


        if (endTime <= startTime)
        {
            user.InOfficeStartTime =
                null;

            return;
        }


        // --------------------------------------------------
        // Record the completed office session exactly once.
        //
        // RecordOfficeSessionAsync handles splitting the
        // session across midnight.
        // --------------------------------------------------

        await RecordOfficeSessionAsync(
            user,
            endTime);


        // --------------------------------------------------
        // The active office session has now been committed
        // to OfficeHistory.
        // --------------------------------------------------

        user.InOfficeStartTime =
            null;
    }


    // ==================================================
    // Start Office Session
    // ==================================================

    private async Task StartOfficeSessionAsync(
        User user,
        DateTime startTime)
    {
        DateOnly date =
            DateOnly.FromDateTime(
                startTime);


        // --------------------------------------------------
        // TotalTimeInOffice represents COMPLETED office
        // time only.
        //
        // The active session is represented by
        // InOfficeStartTime and is added by Blazor's
        // UserDisplayService.
        // --------------------------------------------------

        user.TotalTimeInOffice =
            await _officeHistoryStore
                .GetOfficeTimeForDateAsync(
                    user.UserId,
                    date);


        // --------------------------------------------------
        // Start the new active office session.
        // --------------------------------------------------

        user.InOfficeStartTime =
            startTime;


        // --------------------------------------------------
        // Preserve the first clock-in time for the day.
        // --------------------------------------------------

        await _officeHistoryStore
            .SetStartTimeAsync(
                user.UserId,
                date,
                startTime);
    }


    // ==================================================
    // End Out-of-Office Session
    // ==================================================

    private async Task EndOutOfOfficeSessionAsync(
        User user,
        DateTime endTime)
    {
        if (user.OutOfOfficeStartTime is null)
        {
            return;
        }


        DateTime startTime =
            user.OutOfOfficeStartTime.Value;


        Status reason =
            user.Status;


        // --------------------------------------------------
        // GoneForTheDay is not a timed out-of-office
        // session.
        // --------------------------------------------------

        if (reason ==
            Status.GoneForTheDay)
        {
            user.OutOfOfficeStartTime =
                null;

            return;
        }


        if (endTime <= startTime)
        {
            user.OutOfOfficeStartTime =
                null;

            return;
        }


        // --------------------------------------------------
        // Record the completed out-of-office session.
        //
        // RecordOutOfOfficeSessionAsync handles splitting
        // the session across midnight.
        // --------------------------------------------------

        await RecordOutOfOfficeSessionAsync(
            user,
            reason,
            startTime,
            endTime);


        // --------------------------------------------------
        // The active out-of-office session has now been
        // committed to OfficeHistoryOutOfOffice.
        // --------------------------------------------------

        user.OutOfOfficeStartTime =
            null;
    }


    // ==================================================
    // Record Out-of-Office Session
    // ==================================================

    private async Task RecordOutOfOfficeSessionAsync(
        User user,
        Status reason,
        DateTime startTime,
        DateTime endTime)
    {
        if (reason ==
            Status.GoneForTheDay)
        {
            return;
        }


        DateTime current =
            startTime;


        // --------------------------------------------------
        // Split across midnight.
        // --------------------------------------------------

        while (current.Date < endTime.Date)
        {
            DateTime midnight =
                current.Date.AddDays(1);


            TimeSpan duration =
                midnight - current;


            if (duration > TimeSpan.Zero)
            {
                await _officeHistoryStore
                    .AddOutOfOfficeTimeAsync(
                        user.UserId,
                        DateOnly.FromDateTime(
                            current),
                        reason,
                        duration);
            }


            current =
                midnight;
        }


        // --------------------------------------------------
        // Record remaining time on the final day.
        // --------------------------------------------------

        if (current < endTime)
        {
            TimeSpan duration =
                endTime - current;


            if (duration > TimeSpan.Zero)
            {
                await _officeHistoryStore
                    .AddOutOfOfficeTimeAsync(
                        user.UserId,
                        DateOnly.FromDateTime(
                            current),
                        reason,
                        duration);
            }
        }
    }


    // ==================================================
    // Start Out-of-Office Session
    // ==================================================

    private void StartOutOfOfficeSession(
        User user,
        Status status,
        DateTime startTime)
    {
        // --------------------------------------------------
        // GoneForTheDay does not create a timed
        // out-of-office entry.
        // --------------------------------------------------

        if (status ==
            Status.GoneForTheDay)
        {
            user.OutOfOfficeStartTime =
                null;

            return;
        }


        user.OutOfOfficeStartTime =
            startTime;
    }

}
