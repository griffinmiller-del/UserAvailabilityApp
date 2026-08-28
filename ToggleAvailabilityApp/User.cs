using System.Diagnostics.Eventing.Reader;

namespace ToggleAvailabilityApp;

public class User
{
    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }

    public Status Status { get; set; }

    public DateTime? InOfficeStartTime { get; set; }

    public TimeSpan TotalTimeInOffice { get; set; }

    public DateTime? OutOfOfficeStartTime { get; set; }
    
    public bool IsActiveUser { get; set; }
    

    public User(int userId, string name, Status status, bool isAvailable = false)
    {
        UserId = userId;
        Name = name;
        IsAvailable = isAvailable;
        Status = status;
        IsActiveUser = true;
    }

    public override string ToString()
    {
        return Name;
    }
}
