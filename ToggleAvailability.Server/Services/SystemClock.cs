namespace ToggleAvailability.Server.Services;

public class SystemClock : IClock
{
    public DateTime Now =>
        DateTime.Now;
}