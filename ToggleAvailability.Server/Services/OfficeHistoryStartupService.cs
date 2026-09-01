using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Services;

public class OfficeHistoryStartupService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;


    public OfficeHistoryStartupService(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory =
            scopeFactory;
    }


    // ==================================================
    // Start
    // ==================================================

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        Console.WriteLine(
            "Initializing today's office history...");


        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Now);


        using IServiceScope scope =
            _scopeFactory.CreateScope();


        var userService =
            scope.ServiceProvider
                .GetRequiredService<UserService>();


        var officeHistoryStore =
            scope.ServiceProvider
                .GetRequiredService<Data.OfficeHistoryStore>();


        var users =
            await userService.GetUsersAsync();


        foreach (var user in users)
        {
            if (!user.IsActiveUser)
            {
                continue;
            }
            OfficeHistory? todayRecord =
                await officeHistoryStore
                    .GetUserHistoryForDateAsync(
                        user.UserId,
                        today);


            if (todayRecord is null)
            {
                // ==========================================
                // No history exists for today.
                //
                // Create an empty record for the new day.
                // ==========================================

                await officeHistoryStore
                    .CreateDailyRecordAsync(
                        user.UserId,
                        today);


                user.InOfficeStartTime =
                    null;

                user.OutOfOfficeStartTime =
                    null;
            }
            else
            {
                if (user.Status == Status.InOffice)
                {
                    // --------------------------------------------------
                    // IMPORTANT:
                    //
                    // StartTime is the first clock-in of the day.
                    // It is NOT necessarily the start of the currently
                    // active office session.
                    //
                    // Therefore it cannot safely be used here to restore
                    // the current session after a restart.
                    // --------------------------------------------------

                    user.InOfficeStartTime =
                        null;
                }
                else
                {
                    user.InOfficeStartTime =
                        null;
                }

                user.OutOfOfficeStartTime =
                    null;
            }


            await userService.UpdateUserAsync(
                user);
        }


        Console.WriteLine(
            $"Today's office history initialized: {today}");
    }


    // ==================================================
    // Stop
    // ==================================================

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}