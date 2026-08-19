namespace ToggleAvailabilityApp;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; }
    public bool IsAvailable { get; set; }

    public Status Status { get; set; }
    public DateTime? InOfficeStartTime { get; set; }

    public User(int userId, string name, Status status, bool isAvailable = false)
    {
        UserId = userId;
        Name = name;
        IsAvailable = isAvailable;
        Status = status;
    }

    public override string ToString()
    {
        return Name;
    }
}
