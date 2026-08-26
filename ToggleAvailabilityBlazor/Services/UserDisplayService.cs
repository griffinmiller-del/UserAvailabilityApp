using ToggleAvailabilityBlazor.Models;

namespace ToggleAvailabilityBlazor.Services;

public class UserDisplayService
{
    /// <summary>
    /// Gets the total time in office for a specific user
    /// </summary>
    /// <param name="user">The user to get the time in office for</param>
    /// <returns>The time the user has been in office, as a string</returns>
    public string GetTimeInOffice(User user)
    {
        TimeSpan total =
            user.TotalTimeInOffice;

        if (user.Status == Status.InOffice &&
            user.InOfficeStartTime is not null)
        {
            TimeSpan currentSession =
                DateTime.Now -
                user.InOfficeStartTime.Value;

            if (currentSession > TimeSpan.Zero)
            {
                total += currentSession;
            }
        }

        if (total < TimeSpan.Zero)
        {
            total = TimeSpan.Zero;
        }

        return
            $"{(int)total.TotalHours:00}:" +
            $"{total.Minutes:00}:" +
            $"{total.Seconds:00}";
    }

    /// <summary>
    /// Gets the text correlating to each status
    /// </summary>
    /// <param name="user">the user to get the status text of</param>
    /// <returns>The status as a string</returns>
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


    public bool IsUserAvailable(User user)
    {
        return
            user.IsAvailable ||
            user.Status == Status.InOffice;
    }
}