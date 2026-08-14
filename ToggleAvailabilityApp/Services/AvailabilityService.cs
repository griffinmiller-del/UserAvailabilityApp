using Microsoft.AspNetCore.SignalR.Client;

namespace ToggleAvailabilityApp.Services
{
    public class AvailabilityService : IAsyncDisposable
    {
        private readonly HubConnection _connection;

        public event Action<User>? UserUpdated;

        public bool IsConnected =>
            _connection.State ==
            HubConnectionState.Connected;

        public AvailabilityService()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(
                    "http://localhost:5000/availability")
                .WithAutomaticReconnect()
                .Build();

            // Receive updates from the server.
            //
            // This will fire when ANY client changes
            // a user's availability.
            _connection.On<User>(
                "UserUpdated",
                user =>
                {
                    UserUpdated?.Invoke(user);
                });

            _connection.Reconnecting += error =>
            {
                Console.WriteLine(
                    $"SignalR reconnecting: " +
                    $"{error?.Message}");

                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                Console.WriteLine(
                    $"SignalR reconnected: " +
                    $"{connectionId}");

                return Task.CompletedTask;
            };

            _connection.Closed += error =>
            {
                Console.WriteLine(
                    $"SignalR connection closed: " +
                    $"{error?.Message}");

                return Task.CompletedTask;
            };
        }

        public async Task ConnectAsync()
        {
            if (_connection.State ==
                HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                Console.WriteLine(
                    "Connecting to Availability Server...");

                await _connection.StartAsync();

                Console.WriteLine(
                    "Connected to Availability Server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Failed to connect: " +
                    $"{ex.Message}");

                throw;
            }
        }

        public async Task SetAvailabilityAsync(User user)
        {
            await _connection.InvokeAsync(
                "SetAvailability",
                user.UserId,
                user.IsAvailable,
                user.Status);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _connection.StopAsync();
            }
            catch
            {
                // Ignore connection errors while
                // the application is closing.
            }

            await _connection.DisposeAsync();
        }
    }
}