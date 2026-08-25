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


    // ==================================================
    // Get User History
    // ==================================================

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


    // ==================================================
    // Update User
    // ==================================================

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
                x => x.UserId == user.UserId);


        if (existingUser is null)
        {
            updatedUsers.Add(
                CloneUser(user));
        }
        else
        {
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
        }


        Users = updatedUsers;
    }


    // ==================================================
    // Clone User
    // ==================================================

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
                user.TotalTimeInOffice
        };
    }


    // ==================================================
    // Connect
    // ==================================================

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
    public async Task<List<OfficeHistory>>
    GetUserHistoryAsync(
    int userId)
    {
        return await _connection.InvokeAsync<
            List<OfficeHistory>>(
            "GetUserHistory",
            userId);
    }

    // ==================================================
    // Dispose
    // ==================================================


    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }


        _disposed = true;


        UsersChanged = null;


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