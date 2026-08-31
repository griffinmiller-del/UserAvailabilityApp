using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Hubs;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Services;

public class OfficeHistoryMidnightService : BackgroundService
{
    private readonly IHubContext<AvailabilityHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;


    public OfficeHistoryMidnightService(
        IHubContext<AvailabilityHub> hubContext,
        IServiceScopeFactory scopeFactory)
    {
        _hubContext =
            hubContext;

        _scopeFactory =
            scopeFactory;
    }


    // ==================================================
    // Execute
    // ==================================================

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


    // ==================================================
    // Perform Midnight Rollover
    // ==================================================

    private async Task PerformMidnightRollover()
    {
        // --------------------------------------------------
        // Create a scoped lifetime for EF Core services.
        // --------------------------------------------------

        using IServiceScope scope =
            _scopeFactory.CreateScope();


        var userService =
            scope.ServiceProvider
                .GetRequiredService<UserService>();


        var officeHistoryStore =
            scope.ServiceProvider
                .GetRequiredService<OfficeHistoryStore>();


        // --------------------------------------------------
        // Midnight is the exact boundary between the
        // previous day and the new day.
        // --------------------------------------------------

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


        // --------------------------------------------------
        // Get all users from the database.
        // --------------------------------------------------

        var users =
            await userService.GetUsersAsync();


        foreach (var user in users)
        {
            // ==================================================
            // USER IS STILL IN THE OFFICE
            // ==================================================

            if (user.Status == Status.InOffice &&
                user.InOfficeStartTime is not null)
            {
                DateTime startTime =
                    user.InOfficeStartTime.Value;


                // --------------------------------------------------
                // Record only the portion of the session that
                // belongs to the previous day.
                // --------------------------------------------------

                DateTime effectiveStart =
                    startTime < midnight
                        ? startTime
                        : midnight;


                TimeSpan duration =
                    midnight - effectiveStart;


                if (duration > TimeSpan.Zero)
                {
                    await officeHistoryStore
                        .AddOfficeTimeAsync(
                            user.UserId,
                            previousDate,
                            duration);
                }


                // ==================================================
                // START NEW DAY
                // ==================================================

                // The user never left the office.
                // Their new session begins at midnight.

                user.InOfficeStartTime =
                    midnight;


                user.OutOfOfficeStartTime =
                    null;


                user.TotalTimeInOffice =
                    TimeSpan.Zero;


                // --------------------------------------------------
                // Create today's history record.
                // --------------------------------------------------

                await officeHistoryStore
                    .CreateDailyRecordAsync(
                        user.UserId,
                        currentDate,
                        midnight);


                // --------------------------------------------------
                // Persist the new live session state.
                // --------------------------------------------------

                await userService
                    .UpdateUserAsync(
                        user);
            }

            // ==================================================
            // USER IS NOT IN THE OFFICE
            // ==================================================

            else
            {
                user.TotalTimeInOffice =
                    TimeSpan.Zero;


                user.InOfficeStartTime =
                    null;


                // --------------------------------------------------
                // No active out-of-office session should carry
                // into the new day.
                // --------------------------------------------------

                user.OutOfOfficeStartTime =
                    null;


                // --------------------------------------------------
                // Create an empty record for the new day.
                // --------------------------------------------------

                await officeHistoryStore
                    .CreateDailyRecordAsync(
                        user.UserId,
                        currentDate);


                // --------------------------------------------------
                // Persist the reset state.
                // --------------------------------------------------

                await userService
                    .UpdateUserAsync(
                        user);
            }


            // ==================================================
            // NOTIFY BLAZOR
            // ==================================================

            await _hubContext.Clients.All.SendAsync(
                "UserUpdated",
                user);
        }


        Console.WriteLine(
            $"Midnight rollover complete. " +
            $"New day: {currentDate}");
    }
}