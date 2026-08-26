using System.Text.Json;
using System.Text.Json.Serialization;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Data;

public static class UserStore
{

    private static readonly string _userIdFilePath =
    Path.Combine(
        Directory.GetParent(
            AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
        "Data",
        "user-id.json");

    private static readonly string _filePath =
        Path.Combine(
            Directory.GetParent(
                AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
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
            var user = FindUser(userId);

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
            int nextUserId = LoadNextUserId();

            SaveNextUserId(
                nextUserId + 1);

            return nextUserId;
        }
    }

    // --------------------------------------------------
    // Load next user ID
    // --------------------------------------------------

    private static int LoadNextUserId()
    {
        try
        {
            if (File.Exists(_userIdFilePath))
            {
                string json =
                    File.ReadAllText(
                        _userIdFilePath);

                if (int.TryParse(
                    json,
                    out int nextUserId))
                {
                    return Math.Max(
                        nextUserId,
                        GetInitialNextUserId());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to load next user ID: " +
                $"{ex.Message}");
        }

        int initialNextUserId =
            GetInitialNextUserId();

        SaveNextUserId(
            initialNextUserId + 1);

        return initialNextUserId;
    }

    // --------------------------------------------------
    // Get initial next user ID
    // --------------------------------------------------

    private static int GetInitialNextUserId()
    {
        if (_users.Count == 0)
        {
            return 1;
        }

        return _users.Max(
                   x => x.UserId) + 1;
    }

    // --------------------------------------------------
    // Save next user ID
    // --------------------------------------------------

    private static void SaveNextUserId(
        int nextUserId)
    {
        try
        {
            File.WriteAllText(
                _userIdFilePath,
                nextUserId.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to save next user ID: " +
                $"{ex.Message}");

            throw;
        }
    }

    // --------------------------------------------------
    // Add user
    // --------------------------------------------------

    public static void AddUser(
        User user)
    {
        ArgumentNullException.ThrowIfNull(user);

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
        ArgumentNullException.ThrowIfNull(user);

        lock (_lock)
        {
            var existing = FindUser(user.UserId);

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

            // ----------------------------------------------
            // Office time tracking
            // ----------------------------------------------

            existing.InOfficeStartTime =
                user.InOfficeStartTime;

            existing.TotalTimeInOffice =
                user.TotalTimeInOffice;

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
            var user = FindUser(userId);
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
        ArgumentNullException.ThrowIfNull(users);

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
                user.Status,

            InOfficeStartTime =
                user.InOfficeStartTime,

            TotalTimeInOffice =
                user.TotalTimeInOffice
        };
    }

    private static User? FindUser(
    int userId)
    {
        return _users.FirstOrDefault(
            x =>
                x.UserId == userId);
    }
}