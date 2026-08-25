using Microsoft.Extensions.Hosting;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Services;

public class OfficeHistoryStartupService : IHostedService
{
    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        Console.WriteLine(
            "Initializing today's office history...");

        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Now);

        var users =
            UserStore.GetUsers();

        foreach (var user in users)
        {
            OfficeHistory? todayRecord =
                OfficeHistoryStore.GetUserHistoryForDate(
                    user.UserId,
                    today);

            if (todayRecord is null)
            {
                // ==========================================
                // No history exists for today.
                //
                // The server is starting at the beginning
                // of a new day, so nobody has clocked in yet.
                // ==========================================

                user.TotalTimeInOffice =
                    TimeSpan.Zero;

                user.InOfficeStartTime =
                    null;

                OfficeHistoryStore.CreateDailyRecord(
                    user.UserId,
                    today);
            }
            else
            {
                // ==========================================
                // A record already exists for today.
                // Restore today's accumulated state.
                // ==========================================

                user.TotalTimeInOffice =
                    todayRecord.TimeInOffice;

                user.InOfficeStartTime =
                    todayRecord.StartTime;
            }

            UserStore.UpdateUser(user);
        }

        Console.WriteLine(
            $"Today's office history initialized: {today}");

        return Task.CompletedTask;
    }


    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}