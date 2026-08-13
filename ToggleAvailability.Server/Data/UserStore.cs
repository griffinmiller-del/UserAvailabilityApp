using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Data;

public static class UserStore
{
    private static readonly object _lock = new();

    private static readonly List<User> _users =
    [
        new User
        {
            UserId = 1,
            Name = "Bob",
            IsAvailable = true
        },

        new User
        {
            UserId = 2,
            Name = "Rob",
            IsAvailable = false
        },

        new User
        {
            UserId = 3,
            Name = "John",
            IsAvailable = true
        },

        new User
        {
            UserId = 4,
            Name = "Jane",
            IsAvailable = false
        },

        new User
        {
            UserId = 5,
            Name = "Joe",
            IsAvailable = true
        },

        new User
        {
            UserId = 6,
            Name = "Frank",
            IsAvailable = false
        }
    ];

    public static List<User> GetUsers()
    {
        lock (_lock)
        {
            return _users
                .Select(user => new User
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    IsAvailable = user.IsAvailable
                })
                .ToList();
        }
    }

    public static User? GetUser(int userId)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(
                user => user.UserId == userId);

            if (user is null)
                return null;

            return new User
            {
                UserId = user.UserId,
                Name = user.Name,
                IsAvailable = user.IsAvailable
            };
        }
    }

    public static bool SetAvailability(
        int userId,
        bool isAvailable)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(
                user => user.UserId == userId);

            if (user is null)
                return false;

            user.IsAvailable = isAvailable;

            return true;
        }
    }
}