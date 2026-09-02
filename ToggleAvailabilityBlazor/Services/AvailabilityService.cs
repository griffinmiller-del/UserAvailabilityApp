using Microsoft.AspNetCore.SignalR.Client;
using ToggleAvailabilityBlazor.Models;

namespace ToggleAvailabilityBlazor.Services;

public class AvailabilityService : IAsyncDisposable
{
    // ==================================================
    // Fields
    // ==================================================

    private readonly IConfiguration _configuration;
    private readonly HubConnection _connection;

    private readonly SemaphoreSlim _connectionLock =
        new(1, 1);

    private bool _disposed;


    // ==================================================
    // State
    // ==================================================

    public List<User> InactiveUsers { get; private set; } = [];

    public List<User> Users { get; private set; } = [];


    public bool IsConnected =>
        !_disposed &&
        _connection.State ==
            HubConnectionState.Connected;


    // ==================================================
    // Events
    // ==================================================

    public event Func<Task>? UsersChanged;

    public event Func<User, Task>? UserUpdated;
    // ==================================================
    // Constructor
    // ==================================================

    public AvailabilityService(
        IConfiguration configuration)
    {
        _configuration = configuration;

        string serverUrl =
            _configuration["AvailabilityServer:BaseUrl"]
            ?? throw new InvalidOperationException(
                "AvailabilityServer:BaseUrl is not configured.");


        _connection =
            new HubConnectionBuilder()
                .WithUrl(
                    $"{serverUrl}/availability")
                .WithAutomaticReconnect()
                .Build();


        // ==================================================
        // User List
        // ==================================================

        _connection.On<List<User>>(
            "UserList",
            async users =>
            {
                if (_disposed)
                {
                    return;
                }


                Console.WriteLine(
                    $"[Blazor] Received UserList: " +
                    $"{users.Count} users.");


                Users =
                    users
                        .Select(CloneUser)
                        .ToList();


                await NotifyUsersChangedAsync();
            });


        // ==================================================
        // User Updated
        // ==================================================

        _connection.On<User>(
            "UserUpdated",
            async user =>
            {
                if (_disposed)
                {
                    return;
                }


                Console.WriteLine(
                    $"[Blazor] Received UserUpdated: " +
                    $"{user.UserId} - " +
                    $"{user.Name} - " +
                    $"{user.IsAvailable} - " +
                    $"{user.Status}");


                UpdateUser(user);

                await NotifyUserUpdatedAsync(user);

                await NotifyUsersChangedAsync();
            });


        // ==================================================
        // Reconnecting
        // ==================================================

        _connection.Reconnecting += error =>
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }


            Console.WriteLine(
                $"[Blazor] Reconnecting: " +
                $"{error?.Message}");


            return Task.CompletedTask;
        };


        // ==================================================
        // Reconnected
        // ==================================================

        _connection.Reconnected += connectionId =>
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }


            Console.WriteLine(
                $"[Blazor] Reconnected: " +
                $"{connectionId}");


            return Task.CompletedTask;
        };


        // ==================================================
        // Closed
        // ==================================================

        _connection.Closed += error =>
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }


            Console.WriteLine(
                $"[Blazor] Connection closed: " +
                $"{error?.Message}");


            return Task.CompletedTask;
        };
    }


    // ==================================================
    // Notify User Updated
    // ==================================================

    private async Task NotifyUserUpdatedAsync(
        User user)
    {
        if (_disposed)
        {
            return;
        }


        Func<User, Task>? handler =
            UserUpdated;


        if (handler is null)
        {
            return;
        }


        try
        {
            await handler.Invoke(user);
        }
        catch (ObjectDisposedException)
        {
            // A Blazor circuit may have been disposed
            // while an event was being delivered.
        }
        catch (InvalidOperationException)
        {
            // A renderer may have disappeared during
            // a circuit refresh/disconnect.
        }
    }

    // ==================================================
    // Notify Components
    // ==================================================

    private async Task NotifyUsersChangedAsync()
    {
        if (_disposed)
        {
            return;
        }


        Func<Task>? handler =
            UsersChanged;


        if (handler is null)
        {
            return;
        }


        try
        {
            await handler.Invoke();
        }
        catch (ObjectDisposedException)
        {
            // A Blazor circuit may have been disposed
            // while an event was being delivered.
        }
        catch (InvalidOperationException)
        {
            // A renderer may have disappeared during
            // a circuit refresh/disconnect.
        }
    }


    /// <summary>
    /// Handles getting the history of a user
    /// </summary>
    /// <param name="userId">The id of the user to get the history of</param>
    /// <returns></returns>
    public async Task<List<OfficeHistory>>
        GetUserHistory(
            int userId)
    {
        if (_disposed ||
            _connection.State !=
                HubConnectionState.Connected)
        {
            return [];
        }


        return await _connection.InvokeAsync<
            List<OfficeHistory>>(
            "GetUserHistory",
            userId);
    }


    /// <summary>
    /// Updates an existing user in the active user list.
    ///
    /// The server's UserList is authoritative for which users
    /// are active. Therefore, UserUpdated must never add a new
    /// user to the local list.
    ///
    /// If a user becomes inactive, the server will send an updated
    /// UserList and the user will be removed from Users there.
    /// </summary>
    /// <param name="user">The user to update</param>
    private void UpdateUser(User user)
    {
        if (_disposed)
        {
            return;
        }


        var updatedUsers =
            Users
                .Select(CloneUser)
                .ToList();


        var existingUser =
            updatedUsers.FirstOrDefault(
                x =>
                    x.UserId ==
                    user.UserId);


        // --------------------------------------------------
        // The server's active UserList is authoritative.
        //
        // Do not add a user here if it isn't already in
        // the active list.
        //
        // A newly active user will arrive through UserList.
        // --------------------------------------------------

        if (existingUser is null)
        {
            return;
        }


        // --------------------------------------------------
        // Update the existing user.
        // --------------------------------------------------

        existingUser.Name =
            user.Name;

        existingUser.IsAvailable =
            user.IsAvailable;

        existingUser.Status =
            user.Status;

        existingUser.InOfficeStartTime =
            user.InOfficeStartTime;

        existingUser.TotalTimeInOffice =
            user.TotalTimeInOffice;

        existingUser.OutOfOfficeStartTime =
            user.OutOfOfficeStartTime;

        existingUser.IsActiveUser =
            user.IsActiveUser;


        Users =
            updatedUsers;
    }


    /// <summary>
    /// Creates a clone of a user.
    /// </summary>
    /// <param name="user">The user object to clone</param>
    /// <returns>The cloned user object</returns>
    private static User CloneUser(
        User user)
    {
        return new User
        {
            UserId =
                user.UserId,

            Name =
                user.Name,

            IsAvailable =
                user.IsAvailable,

            Status =
                user.Status,

            InOfficeStartTime =
                user.InOfficeStartTime,

            TotalTimeInOffice =
                user.TotalTimeInOffice,

            OutOfOfficeStartTime =
                user.OutOfOfficeStartTime,

            IsActiveUser =
                user.IsActiveUser
        };
    }


    /// <summary>
    /// Handles connecting to the server hub
    /// </summary>
    /// <returns></returns>
    public async Task ConnectAsync()
    {
        if (_disposed)
        {
            return;
        }


        if (_connection.State ==
            HubConnectionState.Connected)
        {
            return;
        }


        await _connectionLock.WaitAsync();

        try
        {
            if (_disposed)
            {
                return;
            }


            if (_connection.State ==
                HubConnectionState.Connected)
            {
                return;
            }


            Console.WriteLine(
                "[Blazor] Connecting to SignalR...");


            await _connection.StartAsync();


            Console.WriteLine(
                "[Blazor] SignalR connected.");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Handles getting the user history
    /// </summary>
    /// <param name="userId">The user id of the user to get the history of</param>
    /// <returns></returns>
    public async Task<List<OfficeHistory>> GetUserHistoryAsync(int userId)
    {
        return await _connection.InvokeAsync<
            List<OfficeHistory>>(
            "GetUserHistory",
            userId);
    }

    /// <summary>
    /// Get the list of inactive users from the server
    /// </summary>
    /// <returns></returns>
    public async Task<List<User>> GetInactiveUsersAsync()
    {
        if (_disposed ||
            _connection.State !=
                HubConnectionState.Connected)
        {
            return [];
        }

        var users =
            await _connection.InvokeAsync<List<User>>(
                "GetInactiveUsers");

        InactiveUsers =
            users
                .Select(CloneUser)
                .ToList();

        return InactiveUsers;
    }

    /// <summary>
    /// Gets a specific user from the server
    /// </summary>
    /// <param name="userId">the user id of the user to get</param>
    /// <returns></returns>
    public async Task<User?> GetUserAsync(
        int userId)
    {
        if (_disposed ||
            _connection.State !=
                HubConnectionState.Connected)
        {
            return null;
        }

        return await _connection.InvokeAsync<User?>(
            "GetUser",
            userId);
    }

    /// <summary>
    /// Handles disposing the connection
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        UsersChanged = null;

        InactiveUsers = [];

        try
        {
            await _connection.StopAsync();
        }
        catch
        {
            // Connection may already be closed.
        }

        try
        {
            await _connection.DisposeAsync();
        }
        catch
        {
            // Connection may already be disposed.
        }

        _connectionLock.Dispose();
    }
}