namespace ToggleAvailability.Server.Services;

public interface IClock
{
    DateTime Now { get; }
}