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

    /// <summary>
    /// Raised whenever the server sends the authoritative
    /// active-user list.
    /// </summary>
    public event Action<List<User>>? UserListReceived;

    /// <summary>
    /// Raised whenever the server sends an individual
    /// user update.
    /// </summary>
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
        // Authoritative user list
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


/// <summary>
/// Handles connecting to the server.
/// </summary>
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


            // --------------------------------------------------
            // Verify that the connection is actually active
            // before continuing.
            // --------------------------------------------------

            if (_connection.State !=
                HubConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    "The Availability Server connection " +
                    "was not active after connecting.");
            }


            Console.WriteLine(
                "[WinForms] SignalR connected.");

            Console.WriteLine(
                $"[WinForms] Connection state: " +
                $"{_connection.State}");


            // --------------------------------------------------
            // DO NOT call GetUsers here.
            //
            // AvailabilityHub.OnConnectedAsync() already calls:
            //
            //     SendActiveUsersToCaller();
            //
            // which sends the UserList event to this client.
            // --------------------------------------------------

            Console.WriteLine(
                "[WinForms] Waiting for initial user list " +
                "from server.");
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
    // Availability
    // --------------------------------------------------

    /// <summary>
    /// Tells the server that a user's availability changed.
    /// </summary>
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
    // Add User
    // --------------------------------------------------

    /// <summary>
    /// Tells the server to add a new user.
    ///
    /// The server is responsible for assigning the user ID
    /// and broadcasting the resulting user list.
    /// </summary>
    public async Task AddUserAsync(
        string name)
    {
        EnsureConnected();

        await _connection.InvokeAsync(
            "AddUser",
            name);
    }


    // --------------------------------------------------
    // Update User
    // --------------------------------------------------

    /// <summary>
    /// Tells the server to update an existing user's name.
    /// </summary>
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
    // Delete User
    // --------------------------------------------------

    /// <summary>
    /// Tells the server to deactivate a user.
    ///
    /// The application does not remove the user locally.
    /// The server broadcasts the resulting authoritative
    /// user list to every connected client.
    /// </summary>
    public async Task DeleteUserAsync(
        int userId)
    {
        EnsureConnected();

        await _connection.InvokeAsync(
            "DeleteUser",
            userId);
    }


    // --------------------------------------------------
    // Connection validation
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

/// <summary>
/// Sends the administrator passcode to the server for
/// verification.
///
/// The passcode is not stored by the client.
/// </summary>
public async Task<bool> AuthenticateAdminAsync(
    string passcode)
    {
        EnsureConnected();

        try
        {
            bool authenticated =
                await _connection.InvokeAsync<bool>(
                    "VerifyAdminPasscode",
                    passcode);

            Console.WriteLine(
                authenticated
                    ? "[WinForms] Admin authentication successful."
                    : "[WinForms] Admin authentication failed.");

            return authenticated;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "[WinForms] Admin authentication error: " +
                $"{ex.Message}");

            return false;
        }
    }

}