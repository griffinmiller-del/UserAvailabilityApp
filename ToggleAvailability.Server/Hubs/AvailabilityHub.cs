using Microsoft.AspNetCore.SignalR;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Hubs;

public class AvailabilityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"Client connected: {Context.ConnectionId}");

        var users =
            UserStore.GetUsers();

        Console.WriteLine(
            $"Sending {users.Count} users to " +
            $"{Context.ConnectionId}");

        await Clients.Caller.SendAsync(
            "UserList",
            users);

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

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SetAvailability(
        int userId,
        bool isAvailable,
        Status status)
    {
        var user =
            UserStore.GetUser(userId);

        if (user is null)
        {
            Console.WriteLine(
                $"User {userId} was not found.");

            return;
        }

        user.IsAvailable =
            isAvailable;
        
        user.Status =
            status;

        UserStore.UpdateUser(user);

        Console.WriteLine(
            $"User updated: " +
            $"{user.Name} ({user.UserId}) - " +
            $"{user.Status} - " +
            $"{(user.IsAvailable
                ? "Available"
                : "Unavailable")}");

        await Clients.All.SendAsync(
            "UserUpdated",
            user);
    }
}