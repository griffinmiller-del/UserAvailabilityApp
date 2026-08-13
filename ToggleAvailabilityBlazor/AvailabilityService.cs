using Microsoft.AspNetCore.SignalR.Client;
using ToggleAvailabilityBlazor.Models;

namespace ToggleAvailabilityBlazor.Services;

public class AvailabilityService
{
    private readonly HubConnection _connection;

    public List<User> Users { get; private set; } = [];

    public bool IsConnected =>
        _connection.State == HubConnectionState.Connected;

    public event Func<User, Task>? UserUpdated;

    public AvailabilityService()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/availability")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<List<User>>(
            "UserList",
            users =>
            {
                Console.WriteLine(
                    $"[Blazor] Received UserList: {users.Count} users.");

                Users = users
                    .Select(CloneUser)
                    .ToList();

                return Task.CompletedTask;
            });

        _connection.On<User>(
            "UserUpdated",
            async user =>
            {
                Console.WriteLine(
                    $"[Blazor] Received UserUpdated: " +
                    $"{user.UserId} - {user.Name} - " +
                    $"{user.IsAvailable}");

                UpdateUser(user);

                if (UserUpdated is not null)
                {
                    await UserUpdated.Invoke(user);
                }
            });

        _connection.Reconnecting += error =>
        {
            Console.WriteLine(
                $"[Blazor] Reconnecting: {error?.Message}");

            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            Console.WriteLine(
                $"[Blazor] Reconnected: {connectionId}");

            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            Console.WriteLine(
                $"[Blazor] Connection closed: {error?.Message}");

            return Task.CompletedTask;
        };
    }

    private void UpdateUser(User user)
    {
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
        }

        Users = updatedUsers;
    }

    private static User CloneUser(User user)
    {
        return new User
        {
            UserId = user.UserId,
            Name = user.Name,
            IsAvailable = user.IsAvailable
        };
    }

    public async Task ConnectAsync()
    {
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
}