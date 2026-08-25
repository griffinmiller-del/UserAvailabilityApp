using ToggleAvailabilityBlazor.Models;

namespace ToggleAvailabilityBlazor.Services;

public class UserDisplayService
{
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