using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Hubs;

namespace ToggleAvailability.Server.Services;

public class OfficeHistoryMidnightService : BackgroundService
{
    private readonly IHubContext<AvailabilityHub> _hubContext;


    public OfficeHistoryMidnightService(
        IHubContext<AvailabilityHub> hubContext)
    {
        _hubContext =
            hubContext;
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        Console.WriteLine(
            "Office history midnight service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime now =
                DateTime.Now;

            DateTime nextMidnight =
                now.Date.AddDays(1);

            TimeSpan delay =
                nextMidnight - now;

            Console.WriteLine(
                $"Next office history rollover: " +
                $"{nextMidnight:yyyy-MM-dd HH:mm:ss}");

            try
            {
                await Task.Delay(
                    delay,
                    stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await PerformMidnightRollover();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error during office history " +
                    $"midnight rollover: {ex}");
            }
        }

        Console.WriteLine(
            "Office history midnight service stopped.");
    }


    private async Task PerformMidnightRollover()
    {
        DateTime midnight =
            DateTime.Now.Date;

        DateOnly previousDate =
            DateOnly.FromDateTime(
                midnight.AddDays(-1));

        DateOnly currentDate =
            DateOnly.FromDateTime(
                midnight);

        Console.WriteLine(
            $"Performing midnight rollover: " +
            $"{previousDate} -> {currentDate}");

        var users =
            UserStore.GetUsers();

        foreach (var user in users)
        {
            // ==========================================
            // User is currently in the office
            // ==========================================

            if (user.Status == Models.Status.InOffice &&
                user.InOfficeStartTime is not null)
            {
                DateTime startTime =
                    user.InOfficeStartTime.Value;

                TimeSpan duration =
                    midnight - startTime;

                if (duration > TimeSpan.Zero)
                {
                    // Finish yesterday's time.
                    OfficeHistoryStore.AddOfficeTime(
                        user.UserId,
                        previousDate,
                        duration);
                }

                // ======================================
                // RESET DAILY TIMER
                // ======================================

                user.TotalTimeInOffice =
                    TimeSpan.Zero;

                // Start today's timer at midnight.
                user.InOfficeStartTime =
                    midnight;

                UserStore.UpdateUser(user);
            }
            else
            {
                // User isn't in the office.
                // Make sure their daily timer is zero.

                user.TotalTimeInOffice =
                    TimeSpan.Zero;

                UserStore.UpdateUser(user);
            }

            // ==========================================
            // Create today's history record
            // ==========================================

            OfficeHistoryStore.CreateDailyRecord(
                user.UserId,
                currentDate);


            // ==========================================
            // Notify Blazor clients
            // ==========================================

            await _hubContext.Clients.All.SendAsync(
                "UserUpdated",
                user);
        }

        Console.WriteLine(
            $"Midnight rollover complete. " +
            $"New day: {currentDate}");
    }
}