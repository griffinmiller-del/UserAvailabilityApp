using Microsoft.AspNetCore.SignalR.Client;
using ToggleAvailabilityApp;

namespace ToggleAvailabilityApp.Services;

public class AvailabilityService : IAsyncDisposable
{
    private readonly HubConnection _connection;

    // --------------------------------------------------
    // Events
    // --------------------------------------------------

    public event Action<List<User>>? UserListReceived;

    public event Action<List<User>>? UserListUpdated;

    public event Action<User>? UserUpdated;

    // --------------------------------------------------
    // Constructor
    // --------------------------------------------------

    public AvailabilityService()
    {
        _connection =
            new HubConnectionBuilder()
                .WithUrl(
                    "http://localhost:5000/availability")
                .WithAutomaticReconnect()
                .Build();

        // --------------------------------------------------
        // Initial user list
        // --------------------------------------------------

        _connection.On<List<User>>(
            "UserList",
            users =>
            {
                Console.WriteLine(
                    $"[WinForms] Received UserList: " +
                    $"{users.Count} users.");

                UserListReceived?.Invoke(
                    users);
            });

        // --------------------------------------------------
        // Updated complete user list
        // --------------------------------------------------

        _connection.On<List<User>>(
            "UserListUpdated",
            users =>
            {
                Console.WriteLine(
                    $"[WinForms] Received UserListUpdated: " +
                    $"{users.Count} users.");

                UserListUpdated?.Invoke(
                    users);
            });

        // --------------------------------------------------
        // Individual user update
        // --------------------------------------------------

        _connection.On<User>(
            "UserUpdated",
            user =>
            {
                Console.WriteLine(
                    $"[WinForms] Received UserUpdated: " +
                    $"{user.UserId} - " +
                    $"{user.Name} - " +
                    $"{user.Status} - " +
                    $"{user.IsAvailable}");

                UserUpdated?.Invoke(
                    user);
            });

        // --------------------------------------------------
        // Reconnecting
        // --------------------------------------------------

        _connection.Reconnecting +=
            exception =>
            {
                Console.WriteLine(
                    "[WinForms] SignalR reconnecting...");

                if (exception is not null)
                {
                    Console.WriteLine(
                        $"[WinForms] Reconnect reason: " +
                        $"{exception.Message}");
                }

                return Task.CompletedTask;
            };

        // --------------------------------------------------
        // Reconnected
        // --------------------------------------------------

        _connection.Reconnected +=
            connectionId =>
            {
                Console.WriteLine(
                    "[WinForms] SignalR reconnected.");

                Console.WriteLine(
                    $"[WinForms] Connection ID: " +
                    $"{connectionId}");

                return Task.CompletedTask;
            };

        // --------------------------------------------------
        // Closed
        // --------------------------------------------------

        _connection.Closed +=
            exception =>
            {
                Console.WriteLine(
                    "[WinForms] SignalR connection closed.");

                if (exception is not null)
                {
                    Console.WriteLine(
                        $"[WinForms] Close reason: " +
                        $"{exception.Message}");
                }

                return Task.CompletedTask;
            };
    }

    // --------------------------------------------------
    // Connection state
    // --------------------------------------------------

    public bool IsConnected =>
        _connection.State ==
        HubConnectionState.Connected;

    // --------------------------------------------------
    // Connect
    // --------------------------------------------------

    public async Task ConnectAsync()
    {
        if (_connection.State ==
            HubConnectionState.Connected)
        {
            Console.WriteLine(
                "[WinForms] Already connected.");

            return;
        }

        Console.WriteLine(
            "[WinForms] Connecting to:");

        Console.WriteLine(
            "https://localhost:7035/availability");

        try
        {
            await _connection.StartAsync();

            Console.WriteLine(
                "[WinForms] SignalR connected.");

            Console.WriteLine(
                $"[WinForms] Connection state: " +
                $"{_connection.State}");

            // Explicitly request the user list.
            //
            // OnConnectedAsync on the server already
            // sends it, but this guarantees that the
            // client gets it after the connection starts.
            await _connection.InvokeAsync(
                "GetUsers");

            Console.WriteLine(
                "[WinForms] Requested user list.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "[WinForms] SignalR connection FAILED.");

            Console.WriteLine(
                $"Exception type: " +
                $"{ex.GetType().FullName}");

            Console.WriteLine(
                $"Message: " +
                $"{ex.Message}");

            if (ex.InnerException is not null)
            {
                Console.WriteLine(
                    $"Inner exception: " +
                    $"{ex.InnerException.Message}");
            }

            throw;
        }
    }

    // --------------------------------------------------
    // Set availability
    // --------------------------------------------------

    public async Task SetAvailabilityAsync(
        User user)
    {
        EnsureConnected();

        await _connection.InvokeAsync(
            "SetAvailability",
            user.UserId,
            user.IsAvailable,
            user.Status);
    }

    // --------------------------------------------------
    // Add user
    // --------------------------------------------------

    public async Task AddUserAsync(
        string name)
    {
        EnsureConnected();

        await _connection.InvokeAsync(
            "AddUser",
            name);
    }

    // --------------------------------------------------
    // Update user
    // --------------------------------------------------

    public async Task UpdateUserAsync(
        int userId,
        string name)
    {
        EnsureConnected();

        await _connection.InvokeAsync(
            "UpdateUser",
            userId,
            name);
    }

    // --------------------------------------------------
    // Delete user
    // --------------------------------------------------

    public async Task DeleteUserAsync(
        int userId)
    {
        EnsureConnected();

        await _connection.InvokeAsync(
            "DeleteUser",
            userId);
    }

    // --------------------------------------------------
    // Replace complete user list
    // --------------------------------------------------

    public async Task UpdateUserListAsync(
        List<User> users)
    {
        EnsureConnected();

        await _connection.InvokeAsync(
            "UpdateUserList",
            users);
    }

    // --------------------------------------------------
    // Ensure connected
    // --------------------------------------------------

    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException(
                "The Availability Server is not connected.");
        }
    }

    // --------------------------------------------------
    // Dispose
    // --------------------------------------------------

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "[WinForms] Error disposing SignalR " +
                $"connection: {ex.Message}");
        }
    }
}