using Microsoft.Extensions.Hosting;
using ToggleAvailability.Server.Data;

namespace ToggleAvailability.Server.Services;

public class OfficeHistoryMidnightService : BackgroundService
{
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
                PerformMidnightRollover();
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


    private static void PerformMidnightRollover()
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
            // ------------------------------------------
            // User is currently in the office
            // ------------------------------------------

            if (user.Status == Models.Status.InOffice &&
                user.InOfficeStartTime is not null)
            {
                DateTime startTime =
                    user.InOfficeStartTime.Value;

                TimeSpan duration =
                    midnight - startTime;

                if (duration > TimeSpan.Zero)
                {
                    // Add the time to yesterday.
                    OfficeHistoryStore.AddOfficeTime(
                        user.UserId,
                        previousDate,
                        duration);

                    // Add the same time to the user's
                    // lifetime total.
                    user.TotalTimeInOffice +=
                        duration;
                }

                // Reset the active timer to midnight.
                user.InOfficeStartTime =
                    midnight;

                UserStore.UpdateUser(user);
            }

            // ------------------------------------------
            // Create today's history record
            // ------------------------------------------

            OfficeHistoryStore.CreateDailyRecord(
                user.UserId,
                currentDate);
        }

        Console.WriteLine(
            $"Midnight rollover complete. " +
            $"New day: {currentDate}");
    }
}