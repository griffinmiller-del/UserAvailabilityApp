namespace ToggleAvailabilityApp;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; }
    public bool IsAvailable { get; set; }

    public User(int userId, string name, bool isAvailable = false)
    {
        UserId = userId;
        Name = name;
        IsAvailable = isAvailable;
    }
}
