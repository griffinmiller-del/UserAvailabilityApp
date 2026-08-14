using System.Text.Json;
using ToggleAvailability.Server.Models;
using System.Text.Json.Serialization;

namespace ToggleAvailability.Server.Data;

public static class UserStore
{
    private static readonly object _lock = new();
    private static readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            WriteIndented = true,

            PropertyNameCaseInsensitive = true,

            Converters =
            {
            new JsonStringEnumConverter()
            }
        };
    private static readonly string _filePath =
        Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "users.json");

    private static List<User> _users = [];

    static UserStore()
    {
        LoadUsers();
    }

    public static List<User> GetUsers()
    {
        lock (_lock)
        {
            return _users
                .Select(CloneUser)
                .ToList();
        }
    }

    public static User? GetUser(int userId)
    {
        lock (_lock)
        {
            var user =
                _users.FirstOrDefault(
                    x => x.UserId == userId);

            return user is null
                ? null
                : CloneUser(user);
        }
    }

    public static void UpdateUser(User updatedUser)
    {
        lock (_lock)
        {
            var existingUser =
                _users.FirstOrDefault(
                    x => x.UserId ==
                         updatedUser.UserId);

            if (existingUser is null)
            {
                _users.Add(
                    CloneUser(updatedUser));
            }
            else
            {
                existingUser.Name =
                    updatedUser.Name;

                existingUser.IsAvailable =
                    updatedUser.IsAvailable;

                existingUser.Status =
                    updatedUser.Status;
            }

            SaveUsers();
        }
    }

    private static void LoadUsers()
    {
        if (!File.Exists(_filePath))
        {
            Console.WriteLine(
                $"Users file not found: {_filePath}");

            _users = [];

            return;
        }

        try
        {
            string json =
                File.ReadAllText(_filePath);

            _users =
                JsonSerializer.Deserialize<List<User>>(
                    json,
                    _jsonOptions)
                ?? [];

            Console.WriteLine(
                $"Loaded {_users.Count} users " +
                $"from users.json.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to load users.json: " +
                $"{ex.Message}");

            _users = [];
        }
    }

    private static void SaveUsers()
    {
        try
        {
            string json =
                JsonSerializer.Serialize(
                    _users,
                    _jsonOptions);

            File.WriteAllText(
                _filePath,
                json);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to save users.json: {ex.Message}");
        }
    }

    private static User CloneUser(User user)
    {
        return new User
        {
            UserId =
                user.UserId,

            Name =
                user.Name,

            IsAvailable =
                user.IsAvailable,

            Status =
                user.Status
        };
    }
}