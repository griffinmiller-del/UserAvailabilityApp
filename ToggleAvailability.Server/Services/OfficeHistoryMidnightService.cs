using Microsoft.AspNetCore.SignalR;
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


    // ==================================================
    // Execute
    // ==================================================

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        Console.WriteLine(
            "Office history midnight service started.");


        while (
            !stoppingToken.IsCancellationRequested)
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
        // ----------------------------------------------
        // Midnight represents the exact boundary between
        // the previous day and the new day.
        // ----------------------------------------------

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
            // ==================================================
            // USER IS STILL IN THE OFFICE
            // ==================================================

            if (user.Status == Models.Status.InOffice &&
                user.InOfficeStartTime is not null)
            {
                DateTime startTime =
                    user.InOfficeStartTime.Value;


                // ----------------------------------------------
                // Calculate all office time from the current
                // day's start until exactly midnight.
                // ----------------------------------------------

                TimeSpan duration =
                    midnight - startTime;


                if (duration > TimeSpan.Zero)
                {
                    // ------------------------------------------
                    // Finish the previous day's history.
                    //
                    // The end of this period is midnight.
                    // OfficeHistory does not need an EndTime
                    // because TimeInOffice represents the
                    // complete duration for the day.
                    // ------------------------------------------

                    OfficeHistoryStore.AddOfficeTime(
                        user.UserId,
                        previousDate,
                        duration);
                }


                // ==================================================
                // START NEW DAY
                // ==================================================

                // Reset the live daily counter.
                user.TotalTimeInOffice =
                    TimeSpan.Zero;


                // ----------------------------------------------
                // IMPORTANT:
                //
                // The user never actually left the office.
                // Their new day's timer therefore starts
                // exactly at midnight.
                // ----------------------------------------------

                user.InOfficeStartTime =
                    midnight;


                UserStore.UpdateUser(
                    user);


                // ----------------------------------------------
                // Create today's history record with midnight
                // as the starting time.
                // ----------------------------------------------

                OfficeHistoryStore.CreateDailyRecord(
                    user.UserId,
                    currentDate,
                    midnight);
            }

            // ==================================================
            // USER IS NOT IN THE OFFICE
            // ==================================================

            else
            {
                // ----------------------------------------------
                // There is no active office session to carry
                // into the new day.
                // ----------------------------------------------

                user.TotalTimeInOffice =
                    TimeSpan.Zero;


                user.InOfficeStartTime =
                    null;


                UserStore.UpdateUser(
                    user);


                // ----------------------------------------------
                // Still create an empty history record for
                // the new day.
                // ----------------------------------------------

                OfficeHistoryStore.CreateDailyRecord(
                    user.UserId,
                    currentDate);
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