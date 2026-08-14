using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Data;

public static class UserStore
{
    private static readonly List<User> _users =
    [
        new User
        {
            UserId = 1,
            Name = "Bob",
            IsAvailable = true,
            Status = Status.InOffice
        },

        new User
        {
            UserId = 2,
            Name = "Rob",
            IsAvailable = false,
            Status = Status.Break
        },

        new User
        {
            UserId = 3,
            Name = "John",
            IsAvailable = true,
            Status = Status.InOffice
        },

        new User
        {
            UserId = 4,
            Name = "Jane",
            IsAvailable = false,
            Status = Status.Meeting
        },

        new User
        {
            UserId = 5,
            Name = "Joe",
            IsAvailable = true,
            Status = Status.InOffice
        },

        new User
        {
            UserId = 6,
            Name = "Frank",
            IsAvailable = false,
            Status = Status.OtherSide
        }
    ];

    public static List<User> GetUsers()
    {
        return _users;
    }

    public static User? GetUser(int userId)
    {
        return _users.FirstOrDefault(
            x => x.UserId == userId);
    }
}