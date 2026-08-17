using System.Text.Json;
using System.Text.Json.Serialization;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Data;

public static class UserStore
{
    private static readonly string _filePath =
        Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "users.json");

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

    private static List<User> _users = [];

    private static readonly object _lock = new();

    // --------------------------------------------------
    // Initialize
    // --------------------------------------------------

    static UserStore()
    {
        LoadUsers();
    }

    // --------------------------------------------------
    // Get all users
    // --------------------------------------------------

    public static List<User> GetUsers()
    {
        lock (_lock)
        {
            return _users
                .Select(CloneUser)
                .ToList();
        }
    }

    // --------------------------------------------------
    // Get individual user
    // --------------------------------------------------

    public static User? GetUser(
        int userId)
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

    // --------------------------------------------------
    // Get next user ID
    // --------------------------------------------------

    public static int GetNextUserId()
    {
        lock (_lock)
        {
            if (_users.Count == 0)
            {
                return 1;
            }

            return _users.Max(
                       x => x.UserId) + 1;
        }
    }

    // --------------------------------------------------
    // Add user
    // --------------------------------------------------

    public static void AddUser(
        User user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(
                nameof(user));
        }

        lock (_lock)
        {
            _users.Add(
                CloneUser(user));

            SaveUsers();
        }
    }

    // --------------------------------------------------
    // Update user
    // --------------------------------------------------

    public static void UpdateUser(
        User user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(
                nameof(user));
        }

        lock (_lock)
        {
            var existing =
                _users.FirstOrDefault(
                    x =>
                        x.UserId ==
                        user.UserId);

            if (existing is null)
            {
                return;
            }

            existing.Name =
                user.Name;

            existing.IsAvailable =
                user.IsAvailable;

            existing.Status =
                user.Status;

            SaveUsers();
        }
    }

    // --------------------------------------------------
    // Delete user
    // --------------------------------------------------

    public static void DeleteUser(
        int userId)
    {
        lock (_lock)
        {
            var user =
                _users.FirstOrDefault(
                    x =>
                        x.UserId ==
                        userId);

            if (user is null)
            {
                return;
            }

            _users.Remove(
                user);

            SaveUsers();
        }
    }

    // --------------------------------------------------
    // Replace entire user list
    // --------------------------------------------------

    public static void ReplaceUsers(
        List<User> users)
    {
        if (users is null)
        {
            throw new ArgumentNullException(
                nameof(users));
        }

        lock (_lock)
        {
            _users =
                users
                    .Select(CloneUser)
                    .ToList();

            SaveUsers();
        }
    }

    // --------------------------------------------------
    // Load users.json
    // --------------------------------------------------

    private static void LoadUsers()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine(
                    $"Users file not found: " +
                    $"{_filePath}");

                _users = [];

                return;
            }

            try
            {
                string json =
                    File.ReadAllText(
                        _filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _users = [];

                    Console.WriteLine(
                        "users.json is empty.");

                    return;
                }

                _users =
                    JsonSerializer.Deserialize<List<User>>(
                        json,
                        _jsonOptions)
                    ?? [];

                Console.WriteLine(
                    $"Loaded {_users.Count} users " +
                    $"from users.json.");

                foreach (var user in _users)
                {
                    Console.WriteLine(
                        $"  {user.UserId}: " +
                        $"{user.Name} - " +
                        $"{user.Status} - " +
                        $"{user.IsAvailable}");
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"Failed to deserialize users.json: " +
                    $"{ex.Message}");

                _users = [];
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Failed to load users.json: " +
                    $"{ex.Message}");

                _users = [];
            }
        }
    }

    // --------------------------------------------------
    // Save users.json
    // --------------------------------------------------

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

            Console.WriteLine(
                $"Saved {_users.Count} users " +
                $"to {_filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to save users.json: " +
                $"{ex.Message}");

            throw;
        }
    }

    // --------------------------------------------------
    // Clone user
    // --------------------------------------------------

    private static User CloneUser(
        User user)
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