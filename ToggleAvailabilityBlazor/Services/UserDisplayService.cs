using ToggleAvailabilityBlazor.Models;

namespace ToggleAvailabilityBlazor.Services;

public class UserDisplayService
{
    // ==================================================
    // Get Time In Office
    // ==================================================

    /// <summary>
    /// Gets the total time the user has spent in the
    /// office today, including the currently active
    /// office session.
    ///
    /// TotalTimeInOffice contains completed office time
    /// for the current day.
    ///
    /// If the user is currently in the office, the active
    /// portion of the session is added starting at the
    /// later of:
    ///
    ///     - midnight today
    ///     - the session start time
    ///
    /// This prevents time from a previous day from being
    /// counted toward today's total.
    /// </summary>
    public string GetTimeInOffice(User user)
    {
        TimeSpan total =
            user.TotalTimeInOffice;


        // --------------------------------------------------
        // Add currently active office session.
        // --------------------------------------------------

        if (user.Status == Status.InOffice &&
            user.InOfficeStartTime.HasValue)
        {
            DateTime now =
                DateTime.Now;


            DateTime todayMidnight =
                now.Date;


            DateTime sessionStart =
                user.InOfficeStartTime.Value;


            DateTime effectiveStart =
                sessionStart < todayMidnight
                    ? todayMidnight
                    : sessionStart;


            TimeSpan currentSession =
                now - effectiveStart;


            if (currentSession > TimeSpan.Zero)
            {
                total +=
                    currentSession;
            }
        }


        // --------------------------------------------------
        // Prevent negative values.
        // --------------------------------------------------

        if (total < TimeSpan.Zero)
        {
            total =
                TimeSpan.Zero;
        }


        // --------------------------------------------------
        // Format HH:MM:SS.
        // --------------------------------------------------

        return
            $"{(int)total.TotalHours:00}:" +
            $"{total.Minutes:00}:" +
            $"{total.Seconds:00}";
    }


    // ==================================================
    // Get Status Text
    // ==================================================

    /// <summary>
    /// Gets the display text for a user's current status.
    /// </summary>
    public string GetStatusText(User user)
    {
        return user.Status switch
        {
            Status.InOffice =>
                "In Office",

            Status.PTO =>
                "PTO",

            Status.Lunch =>
                "Lunch",

            Status.WFH =>
                "WFH",

            Status.GoneForTheDay =>
                "Gone for the Day",

            Status.ClientMeeting =>
                "Client Meeting",

            Status.Conference =>
                "Conference",

            Status.Appointment =>
                "Appointment",

            Status.Chicago =>
                "Chicago",

            Status.NewYork =>
                "New York",

            Status.Colombia =>
                "Colombia",

            Status.Peru =>
                "Peru",

            Status.Philippines =>
                "Philippines",

            Status.Italy =>
                "Italy",

            _ =>
                "Unknown"
        };
    }


    // ==================================================
    // Is User Available
    // ==================================================

    /// <summary>
    /// Determines whether the user should be considered
    /// available.
    /// </summary>
    public bool IsUserAvailable(User user)
    {
        return
            user.IsAvailable ||
            user.Status == Status.InOffice;
    }
}