using Microsoft.AspNetCore.SignalR;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Models;
using ToggleAvailability.Server.Services;

namespace ToggleAvailability.Server.Hubs;

public class AvailabilityHub : Hub
{
    private readonly AdminAuthenticationService
        _adminAuthenticationService;

    private readonly UserService
        _userService;

    private readonly OfficeHistoryStore
        _officeHistoryStore;


    public AvailabilityHub(
        AdminAuthenticationService adminAuthenticationService,
        UserService userService,
        OfficeHistoryStore officeHistoryStore)
    {
        _adminAuthenticationService =
            adminAuthenticationService;

        _userService =
            userService;

        _officeHistoryStore =
            officeHistoryStore;
    }


    // ==================================================
    // Connection
    // ==================================================

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"Client connected: " +
            $"{Context.ConnectionId}");


        await SendUsersToCaller();


        await base.OnConnectedAsync();
    }


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


    private bool IsAdminAuthenticated()
    {
        return
            Context.Items.TryGetValue(
                "IsAdmin",
                out object? value) &&
            value is true;
    }


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

    public async Task GetUsers()
    {
        await SendUsersToCaller();
    }

    // ==================================================
    // Get User
    // ==================================================

    public async Task<User?> GetUser(
        int userId)
    {
        return await _userService
            .GetUserAsync(userId);
    }

    // ==================================================
    // Get Inactive Users
    // ==================================================

    public async Task<List<User>> GetInactiveUsers()
    {
        var users =
            await _userService
                .GetInactiveUsersAsync();

        Console.WriteLine(
            $"Sending {users.Count} inactive users to " +
            $"{Context.ConnectionId}");

        return users;
    }

    // ==================================================
    // Get User History
    // ==================================================

    /// <summary>
    /// Gets the complete history of a user from the
    /// database.
    /// </summary>
    public async Task<List<OfficeHistory>> GetUserHistory(
        int userId)
    {
        return await _officeHistoryStore
            .GetUserHistoryAsync(
                userId);
    }


    // ==================================================
    // Add User
    // ==================================================

    public async Task AddUser(
        string name)
    {
        RequireAdminAuthentication();


        name =
            ValidateUserName(
                name);


        bool nameExists =
            await _userService
                .ActiveUserNameExistsAsync(
                    name);


        if (nameExists)
        {
            throw new HubException(
                $"An active user named '{name}' already exists.");
        }


        var user =
            new User
            {
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


        await _userService.AddUserAsync(
            user);


        Console.WriteLine(
            $"User added: " +
            $"{user.Name} ({user.UserId})");


        await BroadcastUserList();
    }


    // ==================================================
    // Update User
    // ==================================================

    public async Task UpdateUser(
        int userId,
        string name)
    {
        RequireAdminAuthentication();


        name =
            ValidateUserName(
                name);


        var user =
            await _userService.GetUserAsync(
                userId);


        if (user is null)
        {
            throw new HubException(
                $"User {userId} was not found.");
        }


        if (!user.IsActiveUser)
        {
            throw new HubException(
                $"User {userId} is inactive.");
        }


        bool nameExists =
            await _userService
                .ActiveUserNameExistsAsync(
                    name,
                    userId);


        if (nameExists)
        {
            throw new HubException(
                $"An active user named '{name}' already exists.");
        }


        user.Name =
            name;


        await _userService.UpdateUserAsync(
            user);


        Console.WriteLine(
            $"User edited: " +
            $"{user.Name} ({user.UserId})");


        await BroadcastUserList();
    }


    // ==================================================
    // Update User List
    // ==================================================

    public async Task UpdateUserList(
        List<User> users)
    {
        RequireAdminAuthentication();


        ArgumentNullException.ThrowIfNull(
            users);


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


        await _userService.ReplaceUsersAsync(
            users);


        Console.WriteLine(
            $"Admin updated user list: " +
            $"{users.Count} active users.");


        await BroadcastUserList();
    }


    // ==================================================
    // Delete / Deactivate User
    // ==================================================

    public async Task DeleteUser(
        int userId)
    {
        RequireAdminAuthentication();


        try
        {
            User user =
                await _userService
                    .DeactivateUserAsync(
                        userId);


            Console.WriteLine(
                $"User deactivated: " +
                $"{user.Name} ({user.UserId})");


            await BroadcastUserList();
        }
        catch (InvalidOperationException ex)
        {
            throw new HubException(
                ex.Message);
        }
    }


    // ==================================================
    // Reactivate User
    // ==================================================

    public async Task ReactivateUser(
        int userId)
    {
        RequireAdminAuthentication();


        try
        {
            var user =
                await _userService
                    .GetUserAsync(
                        userId);


            if (user is null)
            {
                throw new HubException(
                    $"User {userId} was not found.");
            }


            await _userService
                .ReactivateUserAsync(
                    userId);


            Console.WriteLine(
                $"User reactivated: " +
                $"{user.Name} ({user.UserId})");


            await BroadcastUserList();
        }
        catch (InvalidOperationException ex)
        {
            throw new HubException(
                ex.Message);
        }
    }


    // ==================================================
    // Availability
    // ==================================================

    public async Task SetAvailability(
        int userId,
        bool isAvailable,
        Status status)
    {
        try
        {
            User user =
                await _userService
                    .SetAvailabilityAsync(
                        userId,
                        isAvailable,
                        status);


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
        catch (InvalidOperationException ex)
        {
            throw new HubException(
                ex.Message);
        }
    }


    // ==================================================
    // Helpers
    // ==================================================

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


    // ==================================================
    // User Lists
    // ==================================================

    private async Task SendUsersToCaller()
    {

            var users =
                await _userService
                    .GetActiveUsersAsync();

        Console.WriteLine(
            $"Sending {users.Count} active users to " +
            $"{Context.ConnectionId}");


        await Clients.Caller.SendAsync(
            "UserList",
            users);
    }


    private async Task BroadcastUserList()
    {
        var activeUsers =
            await _userService
                .GetActiveUsersAsync();


        Console.WriteLine(
            $"Broadcasting {activeUsers.Count} active users.");


        await Clients.All.SendAsync(
            "UserList",
            activeUsers);
    }
}