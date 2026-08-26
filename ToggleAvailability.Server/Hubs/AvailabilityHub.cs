using Microsoft.AspNetCore.SignalR;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Hubs;

public class AvailabilityHub : Hub
{
    // --------------------------------------------------
    // Client connected
    // --------------------------------------------------

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"Client connected: " +
            $"{Context.ConnectionId}");

        await SendUserListToCaller();

        await base.OnConnectedAsync();
    }

    // --------------------------------------------------
    // Client disconnected
    // --------------------------------------------------

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

    // --------------------------------------------------
    // Get users
    // --------------------------------------------------

    public async Task GetUsers()
    {
        await SendUserListToCaller();
    }

    // --------------------------------------------------
    // Get office history for user
    // --------------------------------------------------

    public Task<List<OfficeHistory>> GetUserHistory(
        int userId)
    {
        return Task.FromResult(
            OfficeHistoryStore.GetUserHistory(
                userId));
    }

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

    // --------------------------------------------------
    // Add user
    // --------------------------------------------------

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

    // --------------------------------------------------
    // Update user name
    // --------------------------------------------------

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

    // --------------------------------------------------
    // Delete user
    // --------------------------------------------------

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

    // --------------------------------------------------
    // Replace entire user list
    // --------------------------------------------------

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

    // --------------------------------------------------
    // Set availability
    // --------------------------------------------------

    public async Task SetAvailability(int userId, bool isAvailable, Status status)
    {
        User user = GetRequiredUser(userId);

        DateTime now = DateTime.Now;

        bool leavingOffice =
            user.Status == Status.InOffice &&
            status != Status.InOffice;

        bool returningToOffice =
            status == Status.InOffice &&
            user.Status != Status.InOffice;

        if (leavingOffice)
        {
            EndOfficeSession(user, now);
        }

        if (returningToOffice)
        {
            StartOfficeSession(user, now);
        }

        user.IsAvailable = isAvailable;

        user.Status = status;

        UserStore.UpdateUser(user);

        Console.WriteLine(
            $"[Server] User updated: " +
            $"{user.Name} | " +
            $"Status={user.Status} | " +
            $"Available={user.IsAvailable} | " +
            $"Start={user.InOfficeStartTime} | " +
            $"Total={user.TotalTimeInOffice}");

        await Clients.All.SendAsync("UserUpdated", user);
    }

    // --------------------------------------------------
    // Private helper methods
    // --------------------------------------------------
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

    private static string ValidateUserName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new HubException(
                "User name cannot be empty.");
        }

        return name.Trim();
    }

    private static void EnsureUniqueUserName(
    string name,
    int? excludedUserId = null)
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