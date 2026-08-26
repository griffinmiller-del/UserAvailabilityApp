using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using ToggleAvailabilityApp;

namespace ToggleAvailabilityApp.Services;

public class AvailabilityService : IAsyncDisposable
{
    private readonly HubConnection _connection;

    private readonly string _serverUrl;


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
        // --------------------------------------------------
        // Load configuration
        // --------------------------------------------------

        IConfiguration configuration =
            new ConfigurationBuilder()
                .SetBasePath(
                    AppContext.BaseDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: true)
                .Build();


        // --------------------------------------------------
        // Get Availability Server URL
        // --------------------------------------------------

        _serverUrl =
            configuration[
                "AvailabilityServer:BaseUrl"]
            ?? throw new InvalidOperationException(
                "AvailabilityServer:BaseUrl is not configured.");


        _serverUrl =
            _serverUrl.TrimEnd('/');


        // --------------------------------------------------
        // Create SignalR connection
        // --------------------------------------------------

        _connection =
            new HubConnectionBuilder()
                .WithUrl(
                    $"{_serverUrl}/availability")
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


    /// <summary>
    /// Handles connecting to the server
    /// </summary>
    /// <returns></returns>
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
            $"{_serverUrl}/availability");


        try
        {
            await _connection.StartAsync();


            Console.WriteLine(
                "[WinForms] SignalR connected.");


            Console.WriteLine(
                $"[WinForms] Connection state: " +
                $"{_connection.State}");


            // --------------------------------------------------
            // Explicitly request the user list.
            //
            // OnConnectedAsync on the server already
            // sends it, but this guarantees that the
            // client gets it after the connection starts.
            // --------------------------------------------------

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


    /// <summary>
    /// Handles telling the server that the availability of a user has been changed
    /// </summary>
    /// <param name="user">The user object that has been changed</param>
    /// <returns></returns>
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


    /// <summary>
    /// Handles telling the server that a new user has been added
    /// </summary>
    /// <param name="name">The name of the new user</param>
    /// <returns></returns>
    public async Task AddUserAsync(
        string name)
    {
        EnsureConnected();


        await _connection.InvokeAsync(
            "AddUser",
            name);
    }


    /// <summary>
    /// Handles telling the server that a user has been updated
    /// </summary>
    /// <param name="userId">The id of the user that has been updated</param>
    /// <param name="name">The name of the updated user</param>
    /// <returns></returns>
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


    /// <summary>
    /// Handles telling the server that a user has been deleted
    /// </summary>
    /// <param name="userId">the id of the user that has been deleted</param>
    /// <returns></returns>
    public async Task DeleteUserAsync(
        int userId)
    {
        EnsureConnected();


        await _connection.InvokeAsync(
            "DeleteUser",
            userId);
    }


    /// <summary>
    /// Replaces the user list
    /// </summary>
    /// <param name="users">The list of users to replace the existing list with</param>
    /// <returns></returns>
    public async Task UpdateUserListAsync(
        List<User> users)
    {
        EnsureConnected();


        await _connection.InvokeAsync(
            "UpdateUserList",
            users);
    }

    /// <summary>
    /// Handles ensuring that the application is connected to the server
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
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