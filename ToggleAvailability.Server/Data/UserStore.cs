/*

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


    // ==================================================
    // Initialize
    // ==================================================

    static UserStore()
    {
        LoadUsers();
    }


    // ==================================================
    // Get Users
    // ==================================================

    /// <summary>
    /// Gets a clone of every user.
    ///
    /// TotalTimeInOffice is intentionally not treated as
    /// persistent history. Completed office time belongs
    /// to OfficeHistoryStore.
    /// </summary>
    public static List<User> GetUsers()
    {
        lock (_lock)
        {
            return _users
                .Select(CloneUser)
                .ToList();
        }
    }


    // ==================================================
    // Get User
    // ==================================================

    /// <summary>
    /// Gets a specific user.
    /// </summary>
    public static User? GetUser(
        int userId)
    {
        lock (_lock)
        {
            var user =
                FindUser(userId);


            return user is null
                ? null
                : CloneUser(user);
        }
    }


    // ==================================================
    // Get Next User ID
    // ==================================================

    public static int GetNextUserId()
    {
        lock (_lock)
        {
            int nextUserId =
                LoadNextUserId();


            SaveNextUserId(
                nextUserId + 1);


            return nextUserId;
        }
    }


    // ==================================================
    // Load Next User ID
    // ==================================================

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


    // ==================================================
    // Get Initial Next User ID
    // ==================================================

    private static int GetInitialNextUserId()
    {
        if (_users.Count == 0)
        {
            return 1;
        }


        return _users.Max(
                   x => x.UserId) + 1;
    }


    // ==================================================
    // Save Next User ID
    // ==================================================

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


    // ==================================================
    // Add User
    // ==================================================

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


    // ==================================================
    // Update User
    // ==================================================

    /// <summary>
    /// Updates a given user in the user list.
    ///
    /// The server is authoritative for office-time tracking.
    /// TotalTimeInOffice contains only completed office sessions.
    /// InOfficeStartTime contains only the currently active
    /// office session.
    ///
    /// A client-provided TotalTimeInOffice is intentionally
    /// ignored so that the same session cannot be added twice.
    /// </summary>
    /// <param name="user">
    /// The user containing the requested state change.
    /// </param>
    public static void UpdateUser(
        User user)
    {
        ArgumentNullException.ThrowIfNull(user);


        lock (_lock)
        {
            var existing =
                FindUser(user.UserId);


            if (existing is null)
            {
                return;
            }


            // --------------------------------------------------
            // Capture the old state before changing anything.
            // --------------------------------------------------

            Status previousStatus =
                existing.Status;


            bool wasInOffice =
                previousStatus == Status.InOffice &&
                existing.InOfficeStartTime.HasValue;


            bool isInOffice =
                user.Status == Status.InOffice;



            // --------------------------------------------------
            // Update normal user state.
            // --------------------------------------------------

            existing.Name =
                user.Name;

            existing.IsAvailable =
                user.IsAvailable;

            existing.Status =
                user.Status;

            existing.IsActiveUser =
                user.IsActiveUser;

            // --------------------------------------------------
            // DO NOT copy:
            //
            // existing.TotalTimeInOffice =
            //     user.TotalTimeInOffice;
            //
            // The server owns this value.
            // --------------------------------------------------

            existing.OutOfOfficeStartTime =
                user.OutOfOfficeStartTime;


            SaveUsers();
        }
    }


    // ==================================================
    // Deactivate User
    // ==================================================

    public static void DeleteUser(
        int userId)
    {
        lock (_lock)
        {
            var user =
                FindUser(userId);

            if (user is null)
            {
                return;
            }

            user.IsActiveUser =
                false;

            SaveUsers();
        }
    }


    // ==================================================
    // Replace Users
    // ==================================================

    public static void ReplaceUsers(
    List<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        lock (_lock)
        {
            var submittedUsers =
                users
                    .Select(CloneUser)
                    .ToList();

            var submittedIds =
                submittedUsers
                    .Select(x => x.UserId)
                    .ToHashSet();

            // --------------------------------------------------
            // Update users that still exist in the edit list.
            // --------------------------------------------------

            foreach (var submittedUser in submittedUsers)
            {
                var existingUser =
                    FindUser(
                        submittedUser.UserId);

                if (existingUser is null)
                {
                    _users.Add(
                        submittedUser);
                }
                else
                {
                    existingUser.Name =
                        submittedUser.Name;

                    existingUser.IsAvailable =
                        submittedUser.IsAvailable;

                    existingUser.Status =
                        submittedUser.Status;

                    existingUser.InOfficeStartTime =
                        submittedUser.InOfficeStartTime;

                    existingUser.TotalTimeInOffice =
                        submittedUser.TotalTimeInOffice;

                    existingUser.OutOfOfficeStartTime =
                        submittedUser.OutOfOfficeStartTime;

                    existingUser.IsActiveUser =
                        submittedUser.IsActiveUser;
                }
            }

            // --------------------------------------------------
            // Users that were removed from the edit list are
            // deactivated instead of deleted.
            // --------------------------------------------------

            foreach (var existingUser in _users)
            {
                if (!submittedIds.Contains(
                        existingUser.UserId))
                {
                    existingUser.IsActiveUser =
                        false;
                }
            }

            SaveUsers();
        }
    }


    // ==================================================
    // Load Users
    // ==================================================

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


                // ------------------------------------------
                // TotalTimeInOffice is not persistent
                // history.
                //
                // Reset it so an old value from users.json
                // cannot be added to the live session.
                // ------------------------------------------

                foreach (var user in _users)
                {
                    user.TotalTimeInOffice =
                        TimeSpan.Zero;
                }


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


    // ==================================================
    // Save Users
    // ==================================================

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


    // ==================================================
    // Clone User
    // ==================================================

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
                user.TotalTimeInOffice,

            OutOfOfficeStartTime =
                user.OutOfOfficeStartTime,

            IsActiveUser =
                user.IsActiveUser
        };
    }

    // ==================================================
    // Get Active Users
    // ==================================================

    /// <summary>
    /// Gets a clone of every active user.
    /// Inactive users remain in users.json but are not
    /// returned by this method.
    /// </summary>
    public static List<User> GetActiveUsers()
    {
        lock (_lock)
        {
            return _users
                .Where(x => x.IsActiveUser)
                .Select(CloneUser)
                .ToList();
        }
    }

    // ==================================================
    // Find User
    // ==================================================

    private static User? FindUser(
        int userId)
    {
        return _users.FirstOrDefault(
            x =>
                x.UserId == userId);
    }
}
*/