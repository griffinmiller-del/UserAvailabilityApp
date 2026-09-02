using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Services;

public class OfficeHistoryInitializer
{
    private readonly UserService _userService;
    private readonly OfficeHistoryStore _officeHistoryStore;
    private readonly IClock _clock;


    public OfficeHistoryInitializer(
        UserService userService,
        OfficeHistoryStore officeHistoryStore,
        IClock clock)
    {
        _userService =
            userService;

        _officeHistoryStore =
            officeHistoryStore;

        _clock =
            clock;
    }


    // ==================================================
    // Initialize
    // ==================================================

    public async Task InitializeAsync(
        CancellationToken cancellationToken)
    {
        DateOnly today =
            DateOnly.FromDateTime(
                _clock.Now);


        var users =
            await _userService.GetUsersAsync();


        foreach (var user in users)
        {
            if (!user.IsActiveUser)
            {
                continue;
            }


            OfficeHistory? todayRecord =
                await _officeHistoryStore
                    .GetUserHistoryForDateAsync(
                        user.UserId,
                        today);


            // ==================================================
            // NO HISTORY EXISTS FOR TODAY
            // ==================================================

            if (todayRecord is null)
            {
                await _officeHistoryStore
                    .CreateDailyRecordAsync(
                        user.UserId,
                        today);


                user.InOfficeStartTime =
                    null;

                user.OutOfOfficeStartTime =
                    null;
            }

            // ==================================================
            // HISTORY ALREADY EXISTS FOR TODAY
            // ==================================================

            else
            {
                // --------------------------------------------------
                // StartTime is the first clock-in of the day.
                //
                // It is NOT necessarily the start of the currently
                // active office session.
                //
                // Therefore it cannot safely be used to restore
                // the current session after a restart.
                // --------------------------------------------------

                user.InOfficeStartTime =
                    null;

                user.OutOfOfficeStartTime =
                    null;
            }


            await _userService
                .UpdateUserAsync(
                    user);
        }
    }
}