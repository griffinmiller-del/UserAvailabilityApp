namespace ToggleAvailabilityBlazor.Services;

using ToggleAvailabilityBlazor.Models;

public class UserDisplayService
{
    /// <summary>
    /// Gets the total time the user has spent in the office.
    ///
    /// TotalTimeInOffice contains all completed office
    /// sessions.
    ///
    /// If the user is currently in the office, the active
    /// session is calculated from InOfficeStartTime and
    /// temporarily added to the completed total for display.
    ///
    /// The active session is NOT written back to
    /// TotalTimeInOffice here.
    /// </summary>
    public string GetTimeInOffice(User user)
    {
        TimeSpan total =
            user.TotalTimeInOffice;


        // --------------------------------------------------
        // Add the currently active session for display only.
        // --------------------------------------------------

        if (user.Status == Status.InOffice &&
            user.InOfficeStartTime.HasValue)
        {
            TimeSpan currentSession =
                DateTime.Now -
                user.InOfficeStartTime.Value;


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