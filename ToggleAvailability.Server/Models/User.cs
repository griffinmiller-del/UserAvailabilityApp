namespace ToggleAvailability.Server.Models;

public class User
{
    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }

    public Status Status { get; set; }

    public DateTime? InOfficeStartTime { get; set; }

    public TimeSpan TotalTimeInOffice { get; set; }

    public DateTime? OutOfOfficeStartTime { get; set; }
}