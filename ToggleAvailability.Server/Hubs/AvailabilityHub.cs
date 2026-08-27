using Microsoft.AspNetCore.SignalR;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Hubs;

public class AvailabilityHub : Hub
{
    /// <summary>
    /// Handles client connections
    /// </summary>
    /// <returns></returns>
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"Client connected: " +
            $"{Context.ConnectionId}");

        await SendUserListToCaller();

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Handles client disconnections
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
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

    
    /// <summary>
    /// Gets list of users
    /// </summary>
    /// <returns></returns>
    public async Task GetUsers()
    {
        await SendUserListToCaller();
    }

    /// <summary>
    /// Gets the complete history of a user
    /// </summary>
    /// <param name="userId">The user id of the user to get the history of</param>
    /// <returns></returns>
    public Task<List<OfficeHistory>> GetUserHistory(
        int userId)
    {
        return Task.FromResult(
            OfficeHistoryStore.GetUserHistory(
                userId));
    }

    /// <summary>
    /// Records a completed office session
    /// </summary>
    /// <param name="user">The user to create the office session for</param>
    /// <param name="endTime">The time the office session ended</param>
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

    /// <summary>
    /// Adds a user to the list of users
    /// </summary>
    /// <param name="name">The name of the new user to be added to the list</param>
    /// <returns></returns>
    public async Task AddUser(
        string name)
    {
        name = ValidateUserName(name);

        // Prevent duplicate names.
        EnsureUniqueUserName(name);

        int userId =
            UserStore.GetNextUserId();

        var user = new User
        {
            UserId = userId,

            Name = name,

            IsAvailable = true,

            Status = Status.InOffice,

            InOfficeStartTime = DateTime.Now
        };

        UserStore.AddUser(
            user);

        Console.WriteLine(
            $"User added: " +
            $"{user.Name} ({user.UserId})");

        await BroadcastUserList();
    }

    /// <summary>
    /// Update the name of a user
    /// </summary>
    /// <param name="userId">The user id of the user to update the name of</param>
    /// <param name="name">the new name to assign to the user</param>
    /// <returns></returns>
    public async Task UpdateUser(
        int userId,
        string name)
    {
        name = ValidateUserName(name);

        var user = GetRequiredUser(userId);

        // Prevent duplicate names.
        EnsureUniqueUserName(name, userId);

        user.Name =
            name;

        UserStore.UpdateUser(
            user);

        Console.WriteLine(
            $"User edited: " +
            $"{user.Name} ({user.UserId})");

        await BroadcastUserList();
    }

    /// <summary>
    /// Deletes a user from the user list
    /// </summary>
    /// <param name="userId">The user id of the user to delete</param>
    /// <returns></returns>
    public async Task DeleteUser(
        int userId)
    {
        var user = GetRequiredUser(userId);

        UserStore.DeleteUser(
            userId);

        Console.WriteLine(
            $"User deleted: " +
            $"{user.Name} ({user.UserId})");

        await BroadcastUserList();
    }

    /// <summary>
    /// Replaces the entire user list
    /// </summary>
    /// <param name="users">The list of users to replace the user list with</param>
    /// <returns></returns>
    /// <exception cref="HubException"></exception>
    public async Task UpdateUserList(
        List<User> users)
    {
        if (users is null)
        {
            throw new HubException(
                "User list cannot be null.");
        }

        foreach (var user in users)
        {
            user.Name = ValidateUserName(user.Name);
        }

        // Duplicate IDs.
        bool duplicateIds =
            users
                .GroupBy(x => x.UserId)
                .Any(x => x.Count() > 1);

        if (duplicateIds)
        {
            throw new HubException(
                "The user list contains duplicate IDs.");
        }

        // Duplicate names.
        bool duplicateNames =
            users
                .GroupBy(
                    x => x.Name.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Any(x => x.Count() > 1);

        if (duplicateNames)
        {
            throw new HubException(
                "The user list contains duplicate names.");
        }

        UserStore.ReplaceUsers(
            users);

        Console.WriteLine(
            $"User list replaced. " +
            $"{users.Count} users.");

        await BroadcastUserList();
    }

    /// <summary>
    /// Sets the availability status of a user
    /// </summary>
    /// <param name="userId">The user to modify the availability of</param>
    /// <param name="isAvailable">whether or not the user is available</param>
    /// <param name="status">The status to assign to the user</param>
    /// <returns></returns>

    public async Task SetAvailability(
    int userId,
    bool isAvailable,
    Status status)
    {
        User user =
            GetRequiredUser(userId);

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

        // ==================================================
        // Leaving the office
        // ==================================================

        if (wasInOffice && isOutOfOffice)
        {
            EndOfficeSession(
                user,
                now);

            StartOutOfOfficeSession(
                user,
                status,
                now);
        }

        // ==================================================
        // Changing from one out-of-office reason to another
        // ==================================================

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

        // ==================================================
        // Returning to the office
        // ==================================================

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

    /// <summary>
    /// Ends the current out-of-office period and records
    /// the elapsed time under its reason.
    /// </summary>
    /// <param name="user">
    /// The user whose out-of-office period is ending
    /// </param>
    /// <param name="endTime">
    /// The time the out-of-office period ended
    /// </param>
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

        // OutForTheDay is intentionally not recorded.
        if (reason == Status.GoneForTheDay)
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

    /// <summary>
    /// Records an out-of-office session across one or more dates.
    /// </summary>
    private static void RecordOutOfOfficeSession(
        User user,
        Status reason,
        DateTime startTime,
        DateTime endTime)
    {
        if (reason == Status.GoneForTheDay)
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


    /// <summary>
    /// Starts tracking an out-of-office period.
    /// </summary>
    /// <param name="user">
    /// The user starting the out-of-office period
    /// </param>
    /// <param name="status">
    /// The reason the user is out
    /// </param>
    /// <param name="startTime">
    /// The time the out-of-office period started
    /// </param>
    private static void StartOutOfOfficeSession(
        User user,
        Status status,
        DateTime startTime)
    {
        user.OutOfOfficeStartTime =
            startTime;
    }

    // --------------------------------------------------
    // Private helper methods
    // --------------------------------------------------

    /// <summary>
    /// Gets a required user
    /// </summary>
    /// <param name="userId">The user id of the user being searched for</param>
    /// <returns>The user being searched for with a matching user id</returns>
    /// <exception cref="HubException"></exception>
    private static User GetRequiredUser(int userId)
    {
        var user = UserStore.GetUser(userId);

        if (user is null)
        {
            throw new HubException(
                $"User {userId} was not found.");
        }

        return user;
    }

    /// <summary>
    /// Validates the user name
    /// </summary>
    /// <param name="name">The name of the user that is being validated</param>
    /// <returns>A normalized user name</returns>
    /// <exception cref="HubException"></exception>
    private static string ValidateUserName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new HubException(
                "User name cannot be empty.");
        }

        return name.Trim();
    }

    /// <summary>
    /// Ensures that the name of the user is unique
    /// </summary>
    /// <param name="name">The name of the user that is being checked for uniqueness</param>
    /// <param name="excludedUserId">The user id of the user to be checkedf or uniqueness</param>
    /// <exception cref="HubException"></exception>
    private static void EnsureUniqueUserName(string name, int? excludedUserId = null)
    {
        bool exists =
            UserStore.GetUsers()
                .Any(x =>
                    x.UserId != excludedUserId &&
                    string.Equals(
                        x.Name.Trim(),
                        name,
                        StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new HubException(
                $"A user named '{name}' already exists.");
        }
    }

    /// <summary>
    /// Ends an office session of a specific user
    /// </summary>
    /// <param name="user">The user to end the session of</param>
    /// <param name="endTime">The time that the session ended</param>
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
            user.InOfficeStartTime = null;
            return;
        }

        user.TotalTimeInOffice += duration;

        RecordOfficeSession(
            user,
            endTime);

        user.InOfficeStartTime = null;
    }

    /// <summary>
    /// Starts an in-office session for a specific user
    /// </summary>
    /// <param name="user">The user to start the session of</param>
    /// <param name="startTime">The time that the session started</param>
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

    /// <summary>
    /// Sends the user listen to the client that is requesting it
    /// </summary>
    /// <returns></returns>
    private async Task SendUserListToCaller()
    {
        var users =
            UserStore.GetUsers();

        Console.WriteLine(
            $"Sending {users.Count} users to " +
            $"{Context.ConnectionId}");

        await Clients.Caller.SendAsync(
            "UserList",
            users);
    }

    /// <summary>
    /// Broadcasts the user list to all connected clients
    /// </summary>
    /// <returns></returns>
    private async Task BroadcastUserList()
    {
        var users = UserStore.GetUsers();

        Console.WriteLine(
            $"Broadcasting updated user list: " +
            $"{users.Count} users.");

        await Clients.All.SendAsync(
            "UserList",
            users);
    }
}