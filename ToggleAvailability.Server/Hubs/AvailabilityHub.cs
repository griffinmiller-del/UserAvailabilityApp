using Microsoft.AspNetCore.SignalR;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Models;
using ToggleAvailability.Server.Services;

namespace ToggleAvailability.Server.Hubs;

public class AvailabilityHub : Hub
{
    private readonly AdminAuthenticationService _adminAuthenticationService;

    public AvailabilityHub(
        AdminAuthenticationService adminAuthenticationService)
    {
        _adminAuthenticationService =
            adminAuthenticationService;
    }

    // ==================================================
    // Connection
    // ==================================================

    /// <summary>
    /// Handles client connections.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"Client connected: " +
            $"{Context.ConnectionId}");

        await SendActiveUsersToCaller();

        await base.OnConnectedAsync();
    }


    /// <summary>
    /// Handles client disconnections.
    /// </summary>
    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        Console.WriteLine(
            $"Client disconnected: " +
            $"{Context.ConnectionId}");

        if (exception is not null)
        {
            Console.WriteLine(
                $"Disconnect reason: " +
                $"{exception.Message}");
        }

        await base.OnDisconnectedAsync(
            exception);
    }


    // ==================================================
    // Admin Authentication
    // ==================================================

    /// <summary>
    /// Verifies the administrator passcode for the
    /// current SignalR connection.
    ///
    /// A successful verification authorizes the
    /// connection to perform administrative operations.
    /// </summary>
    public Task<bool> VerifyAdminPasscode(
        string passcode)
    {
        if (string.IsNullOrEmpty(passcode))
        {
            return Task.FromResult(false);
        }


        bool isValid =
            _adminAuthenticationService
                .VerifyPasscode(passcode);


        if (isValid)
        {
            // --------------------------------------------------
            // Mark this SignalR connection as authenticated
            // for administrative operations.
            // --------------------------------------------------

            Context.Items["IsAdmin"] =
                true;

            Console.WriteLine(
                $"Admin authentication successful: " +
                $"{Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine(
                $"Admin authentication failed: " +
                $"{Context.ConnectionId}");
        }


        return Task.FromResult(
            isValid);
    }


    /// <summary>
    /// Determines whether the current SignalR connection
    /// has successfully authenticated as an administrator.
    /// </summary>
    private bool IsAdminAuthenticated()
    {
        return
            Context.Items.TryGetValue(
                "IsAdmin",
                out object? value) &&
            value is true;
    }


    /// <summary>
    /// Ensures that the current connection has administrator
    /// authorization.
    /// </summary>
    private void RequireAdminAuthentication()
    {
        if (!IsAdminAuthenticated())
        {
            throw new HubException(
                "Administrator authentication is required.");
        }
    }


    // ==================================================
    // Get Users
    // ==================================================

    /// <summary>
    /// Gets the active users.
    /// </summary>
    public async Task GetUsers()
    {
        await SendActiveUsersToCaller();
    }


    /// <summary>
    /// Gets the complete history of a user.
    /// </summary>
    public Task<List<OfficeHistory>> GetUserHistory(
        int userId)
    {
        return Task.FromResult(
            OfficeHistoryStore.GetUserHistory(
                userId));
    }


    // ==================================================
    // Add User
    // ==================================================

    /// <summary>
    /// Adds a new active user.
    ///
    /// Administrator authentication is required.
    /// </summary>
    public async Task AddUser(
        string name)
    {
        RequireAdminAuthentication();


        name =
            ValidateUserName(
                name);


        EnsureUniqueUserName(
            name);


        int userId =
            UserStore.GetNextUserId();


        var user =
            new User
            {
                UserId =
                    userId,

                Name =
                    name,

                IsAvailable =
                    false,

                Status =
                    Status.GoneForTheDay,

                IsActiveUser =
                    true,

                InOfficeStartTime =
                    null,

                OutOfOfficeStartTime =
                    null
            };


        UserStore.AddUser(
            user);


        Console.WriteLine(
            $"User added: " +
            $"{user.Name} ({user.UserId})");


        await BroadcastUserList();
    }


    // ==================================================
    // Update User
    // ==================================================

    /// <summary>
    /// Updates the name of an existing active user.
    ///
    /// Administrator authentication is required.
    /// </summary>
    public async Task UpdateUser(
        int userId,
        string name)
    {
        RequireAdminAuthentication();


        name =
            ValidateUserName(
                name);


        var user =
            GetRequiredUser(
                userId);


        if (!user.IsActiveUser)
        {
            throw new HubException(
                $"User {userId} is inactive.");
        }


        EnsureUniqueUserName(
            name,
            userId);


        user.Name =
            name;


        UserStore.UpdateUser(
            user);


        Console.WriteLine(
            $"User edited: " +
            $"{user.Name} ({user.UserId})");


        await BroadcastUserList();
    }


    // ==================================================
    // Update User List
    // ==================================================

    /// <summary>
    /// Replaces the active user list.
    ///
    /// Users removed from the submitted list are
    /// deactivated rather than deleted.
    ///
    /// Administrator authentication is required.
    /// </summary>
    public async Task UpdateUserList(
        List<User> users)
    {
        RequireAdminAuthentication();


        ArgumentNullException.ThrowIfNull(
            users);


        // --------------------------------------------------
        // Validate all submitted names before changing
        // anything in the database.
        //
        // Duplicate names are allowed only when at most
        // one of the users with that name is active.
        // Since this method receives the active user list,
        // duplicate active names are not allowed here.
        // --------------------------------------------------

        foreach (var user in users)
        {
            user.Name =
                ValidateUserName(
                    user.Name);
        }


        var duplicateName =
            users
                .GroupBy(
                    x => x.Name.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(
                    group => group.Count() > 1);


        if (duplicateName is not null)
        {
            throw new HubException(
                $"An active user named " +
                $"'{duplicateName.Key}' already exists.");
        }


        // --------------------------------------------------
        // The server is authoritative for these values.
        //
        // The edit form should not be able to modify
        // availability, status, office timers, or history.
        // --------------------------------------------------

        var existingUsers =
            UserStore.GetUsers();


        foreach (var submittedUser in users)
        {
            var existingUser =
                existingUsers.FirstOrDefault(
                    x =>
                        x.UserId ==
                        submittedUser.UserId);


            if (existingUser is null)
            {
                // --------------------------------------------------
                // New users should normally be created through
                // AddUser, but this also supports a new user
                // arriving through the edit list.
                // --------------------------------------------------

                submittedUser.IsAvailable =
                    false;

                submittedUser.Status =
                    Status.GoneForTheDay;

                submittedUser.InOfficeStartTime =
                    null;

                submittedUser.OutOfOfficeStartTime =
                    null;

                submittedUser.TotalTimeInOffice =
                    TimeSpan.Zero;

                submittedUser.IsActiveUser =
                    true;
            }
        }


        UserStore.ReplaceUsers(
            users);


        Console.WriteLine(
            $"Admin updated user list: " +
            $"{users.Count} active users.");


        await BroadcastUserList();
    }


    // ==================================================
    // Delete / Deactivate User
    // ==================================================

    /// <summary>
    /// Deactivates a user without removing them from
    /// users.json or their historical data.
    ///
    /// Administrator authentication is required.
    /// </summary>
    public async Task DeleteUser(
        int userId)
    {
        RequireAdminAuthentication();


        var user =
            GetRequiredUser(
                userId);


        // --------------------------------------------------
        // End any active office session before deactivating.
        // --------------------------------------------------

        if (user.InOfficeStartTime is not null)
        {
            EndOfficeSession(
                user,
                DateTime.Now);
        }


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


        UserStore.UpdateUser(
            user);


        Console.WriteLine(
            $"User deactivated: " +
            $"{user.Name} ({user.UserId})");


        await BroadcastUserList();
    }


    // ==================================================
    // Reactivate User
    // ==================================================

    /// <summary>
    /// Reactivates an existing inactive user.
    ///
    /// Administrator authentication is required.
    /// </summary>
    public async Task ReactivateUser(
        int userId)
    {
        RequireAdminAuthentication();


        var user =
            GetRequiredUser(
                userId);


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


        UserStore.UpdateUser(
            user);


        Console.WriteLine(
            $"User reactivated: " +
            $"{user.Name} ({user.UserId})");


        await BroadcastUserList();
    }


    // ==================================================
    // Availability
    // ==================================================

    public async Task SetAvailability(
        int userId,
        bool isAvailable,
        Status status)
    {
        User user =
            GetRequiredUser(
                userId);


        // --------------------------------------------------
        // Inactive users cannot change availability.
        // --------------------------------------------------

        if (!user.IsActiveUser)
        {
            throw new HubException(
                $"User {userId} is inactive.");
        }


        DateTime now =
            DateTime.Now;


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


        // --------------------------------------------------
        // Leaving the office
        // --------------------------------------------------

        if (wasInOffice &&
            isOutOfOffice)
        {
            EndOfficeSession(
                user,
                now);


            StartOutOfOfficeSession(
                user,
                status,
                now);
        }


        // --------------------------------------------------
        // Changing out-of-office reason
        // --------------------------------------------------

        else if (
            wasOutOfOffice &&
            isOutOfOffice &&
            previousStatus != status)
        {
            EndOutOfOfficeSession(
                user,
                now);


            StartOutOfOfficeSession(
                user,
                status,
                now);
        }


        // --------------------------------------------------
        // Returning to office
        // --------------------------------------------------

        else if (
            wasOutOfOffice &&
            isInOffice)
        {
            EndOutOfOfficeSession(
                user,
                now);


            StartOfficeSession(
                user,
                now);
        }


        user.IsAvailable =
            isAvailable;

        user.Status =
            status;


        UserStore.UpdateUser(
            user);


        Console.WriteLine(
            $"[Server] User updated: " +
            $"{user.Name} | " +
            $"Status={user.Status} | " +
            $"Available={user.IsAvailable} | " +
            $"InOfficeStart={user.InOfficeStartTime} | " +
            $"OutOfOfficeStart={user.OutOfOfficeStartTime} | " +
            $"Total={user.TotalTimeInOffice}");


        await Clients.All.SendAsync(
            "UserUpdated",
            user);
    }


    // ==================================================
    // Office History
    // ==================================================

    private static void RecordOfficeSession(
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


        while (current.Date < endTime.Date)
        {
            DateTime midnight =
                current.Date.AddDays(1);


            TimeSpan duration =
                midnight - current;


            OfficeHistoryStore.AddOfficeTime(
                user.UserId,
                DateOnly.FromDateTime(current),
                duration);


            current =
                midnight;
        }


        if (current < endTime)
        {
            TimeSpan duration =
                endTime - current;


            OfficeHistoryStore.AddOfficeTime(
                user.UserId,
                DateOnly.FromDateTime(current),
                duration);
        }
    }


    private static void EndOfficeSession(
        User user,
        DateTime endTime)
    {
        if (user.InOfficeStartTime is null)
        {
            return;
        }


        DateTime startTime =
            user.InOfficeStartTime.Value;


        TimeSpan duration =
            endTime - startTime;


        if (duration <= TimeSpan.Zero)
        {
            user.InOfficeStartTime =
                null;


            return;
        }


        user.TotalTimeInOffice +=
            duration;


        RecordOfficeSession(
            user,
            endTime);


        user.InOfficeStartTime =
            null;
    }


    private static void StartOfficeSession(
        User user,
        DateTime startTime)
    {
        user.InOfficeStartTime =
            startTime;


        OfficeHistoryStore.SetStartTime(
            user.UserId,
            DateOnly.FromDateTime(startTime),
            startTime);
    }


    private static void EndOutOfOfficeSession(
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


        RecordOutOfOfficeSession(
            user,
            reason,
            startTime,
            endTime);


        user.OutOfOfficeStartTime =
            null;
    }


    private static void RecordOutOfOfficeSession(
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


        while (current.Date < endTime.Date)
        {
            DateTime midnight =
                current.Date.AddDays(1);


            TimeSpan duration =
                midnight - current;


            if (duration > TimeSpan.Zero)
            {
                OfficeHistoryStore.AddOutOfOfficeTime(
                    user.UserId,
                    DateOnly.FromDateTime(current),
                    reason,
                    duration);
            }


            current =
                midnight;
        }


        if (current < endTime)
        {
            TimeSpan duration =
                endTime - current;


            if (duration > TimeSpan.Zero)
            {
                OfficeHistoryStore.AddOutOfOfficeTime(
                    user.UserId,
                    DateOnly.FromDateTime(current),
                    reason,
                    duration);
            }
        }
    }


    private static void StartOutOfOfficeSession(
        User user,
        Status status,
        DateTime startTime)
    {
        user.OutOfOfficeStartTime =
            startTime;
    }


    // ==================================================
    // Helpers
    // ==================================================

    private static User GetRequiredUser(
        int userId)
    {
        var user =
            UserStore.GetUser(
                userId);


        if (user is null)
        {
            throw new HubException(
                $"User {userId} was not found.");
        }


        return user;
    }


    private static string ValidateUserName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new HubException(
                "User name cannot be empty.");
        }


        return name.Trim();
    }


    /// <summary>
    /// Prevents duplicate names among active users.
    ///
    /// Inactive users are intentionally ignored, meaning
    /// an inactive John and an active John are allowed.
    /// </summary>
    private static void EnsureUniqueUserName(
        string name,
        int? excludedUserId = null)
    {
        bool exists =
            UserStore.GetUsers()
                .Any(
                    x =>
                        x.UserId != excludedUserId &&
                        x.IsActiveUser &&
                        string.Equals(
                            x.Name.Trim(),
                            name,
                            StringComparison.OrdinalIgnoreCase));


        if (exists)
        {
            throw new HubException(
                $"An active user named '{name}' already exists.");
        }
    }


    // ==================================================
    // User Lists
    // ==================================================

    /// <summary>
    /// Sends only active users to the requesting client.
    /// </summary>
    private async Task SendActiveUsersToCaller()
    {
        var users =
            UserStore.GetActiveUsers();


        Console.WriteLine(
            $"Sending {users.Count} active users to " +
            $"{Context.ConnectionId}");


        await Clients.Caller.SendAsync(
            "UserList",
            users);
    }


    /// <summary>
    /// Broadcasts the authoritative active-user list.
    ///
    /// Clients should treat this list as the complete
    /// set of users that should currently be displayed.
    /// </summary>
    private async Task BroadcastUserList()
    {
        var activeUsers =
            UserStore.GetActiveUsers();


        Console.WriteLine(
            $"Broadcasting {activeUsers.Count} active users.");


        await Clients.All.SendAsync(
            "UserList",
            activeUsers);
    }
}
