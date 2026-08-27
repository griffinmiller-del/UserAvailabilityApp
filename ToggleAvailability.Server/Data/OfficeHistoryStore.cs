using System.Text.Json;
using System.Text.Json.Serialization;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Data;

public static class OfficeHistoryStore
{
    private static readonly string _filePath =
        Path.Combine(
            Directory.GetParent(
                AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
            "Data",
            "office-history.json");


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


    private static List<OfficeHistory> _history = [];


    private static readonly object _lock = new();


    // ==================================================
    // Initialize
    // ==================================================

    static OfficeHistoryStore()
    {
        Load();
    }


    // ==================================================
    // Get User History
    // ==================================================

    /// <summary>
    /// Gets the office history for a specific user.
    /// </summary>
    public static List<OfficeHistory> GetUserHistory(
        int userId)
    {
        lock (_lock)
        {
            return _history
                .Where(x =>
                    x.UserId == userId)
                .OrderByDescending(x =>
                    x.Date)
                .Select(Clone)
                .ToList();
        }
    }


    // ==================================================
    // Get Completed Office Time
    // ==================================================

    /// <summary>
    /// Gets the total office time that has already been
    /// committed to office-history.json for a user.
    ///
    /// This does NOT include a currently active session.
    /// </summary>
    public static TimeSpan GetTotalOfficeTime(
        int userId)
    {
        lock (_lock)
        {
            return _history
                .Where(x =>
                    x.UserId == userId)
                .Aggregate(
                    TimeSpan.Zero,
                    (total, record) =>
                        total + record.TimeInOffice);
        }
    }


    // ==================================================
    // Get Completed Office Time For Date
    // ==================================================

    /// <summary>
    /// Gets the amount of office time already committed
    /// to history for a specific user and date.
    ///
    /// This does NOT include a currently active session.
    /// </summary>
    public static TimeSpan GetOfficeTimeForDate(
        int userId,
        DateOnly date)
    {
        lock (_lock)
        {
            var record =
                FindRecord(
                    userId,
                    date);


            return record?.TimeInOffice
                ?? TimeSpan.Zero;
        }
    }


    // ==================================================
    // Add Office Time
    // ==================================================

    /// <summary>
    /// Adds completed office time to a user's history.
    ///
    /// IMPORTANT:
    /// This method should only receive time that has
    /// actually been completed and is no longer part of
    /// the user's active session.
    /// </summary>
    public static void AddOfficeTime(
        int userId,
        DateOnly date,
        TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }


        lock (_lock)
        {
            var existing =
                FindRecord(
                    userId,
                    date);


            if (existing is null)
            {
                existing =
                    CreateRecord(
                        userId,
                        date,
                        duration);

                _history.Add(
                    existing);
            }
            else
            {
                existing.TimeInOffice +=
                    duration;
            }


            Save();
        }
    }


    // ==================================================
    // Add Out-of-Office Time
    // ==================================================

    /// <summary>
    /// Adds out-of-office time to a user's history for a
    /// specific date.
    ///
    /// GoneForTheDay is intentionally ignored.
    /// </summary>
    public static void AddOutOfOfficeTime(
        int userId,
        DateOnly date,
        Status reason,
        TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }


        if (reason == Status.GoneForTheDay)
        {
            return;
        }


        lock (_lock)
        {
            var existing =
                FindRecord(
                    userId,
                    date);


            if (existing is null)
            {
                existing =
                    CreateRecord(
                        userId,
                        date);

                _history.Add(
                    existing);
            }


            existing.TimeOutOfOffice ??=
                new Dictionary<Status, TimeSpan>();


            if (existing.TimeOutOfOffice.TryGetValue(
                    reason,
                    out TimeSpan currentDuration))
            {
                existing.TimeOutOfOffice[reason] =
                    currentDuration +
                    duration;
            }
            else
            {
                existing.TimeOutOfOffice[reason] =
                    duration;
            }


            Save();
        }
    }


    // ==================================================
    // Get Out-of-Office Time
    // ==================================================

    /// <summary>
    /// Gets all recorded out-of-office time for a user
    /// on a specific date.
    /// </summary>
    public static Dictionary<Status, TimeSpan>
        GetOutOfOfficeTime(
            int userId,
            DateOnly date)
    {
        lock (_lock)
        {
            var record =
                FindRecord(
                    userId,
                    date);


            if (record is null ||
                record.TimeOutOfOffice is null)
            {
                return [];
            }


            return new Dictionary<Status, TimeSpan>(
                record.TimeOutOfOffice);
        }
    }


    // ==================================================
    // Get Total Out-of-Office Time
    // ==================================================

    /// <summary>
    /// Gets the total amount of recorded out-of-office
    /// time for a user on a specific date.
    /// </summary>
    public static TimeSpan GetTotalOutOfOfficeTime(
        int userId,
        DateOnly date)
    {
        lock (_lock)
        {
            var record =
                FindRecord(
                    userId,
                    date);


            if (record is null ||
                record.TimeOutOfOffice is null)
            {
                return TimeSpan.Zero;
            }


            return record.TimeOutOfOffice
                .Where(x =>
                    x.Key != Status.GoneForTheDay)
                .Aggregate(
                    TimeSpan.Zero,
                    (total, entry) =>
                        total + entry.Value);
        }
    }


    // ==================================================
    // Create Daily Record
    // ==================================================

    /// <summary>
    /// Creates a new daily history record if one does
    /// not already exist.
    /// </summary>
    public static void CreateDailyRecord(
        int userId,
        DateOnly date,
        DateTime? startTime = null)
    {
        lock (_lock)
        {
            if (FindRecord(
                    userId,
                    date) is not null)
            {
                return;
            }


            _history.Add(
                CreateRecord(
                    userId,
                    date,
                    TimeSpan.Zero,
                    startTime));


            Save();
        }
    }


    // ==================================================
    // Get History For Date
    // ==================================================

    /// <summary>
    /// Gets a copy of an office history record for a
    /// specific user and date.
    /// </summary>
    public static OfficeHistory? GetUserHistoryForDate(
        int userId,
        DateOnly date)
    {
        lock (_lock)
        {
            var record =
                FindRecord(
                    userId,
                    date);


            return record is null
                ? null
                : Clone(record);
        }
    }


    // ==================================================
    // Set Start Time
    // ==================================================

    /// <summary>
    /// Sets the first punch-in time for a user on a day.
    ///
    /// Once a start time exists, it is never overwritten.
    /// </summary>
    public static void SetStartTime(
        int userId,
        DateOnly date,
        DateTime startTime)
    {
        lock (_lock)
        {
            var existing =
                FindRecord(
                    userId,
                    date);


            if (existing is null)
            {
                _history.Add(
                    CreateRecord(
                        userId,
                        date,
                        TimeSpan.Zero,
                        startTime));


                Save();


                return;
            }


            if (existing.StartTime is null)
            {
                existing.StartTime =
                    startTime;


                Save();
            }
        }
    }


    // ==================================================
    // Load
    // ==================================================

    private static void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine(
                    "Office history file not found. " +
                    "Creating a new history store.");


                _history = [];


                Save();


                return;
            }


            try
            {
                string json =
                    File.ReadAllText(
                        _filePath);


                if (string.IsNullOrWhiteSpace(json))
                {
                    _history = [];


                    return;
                }


                _history =
                    JsonSerializer.Deserialize<
                        List<OfficeHistory>>(
                        json,
                        _jsonOptions)
                    ?? [];


                foreach (var record in _history)
                {
                    record.TimeOutOfOffice ??=
                        new Dictionary<Status, TimeSpan>();
                }


                Console.WriteLine(
                    $"Loaded {_history.Count} " +
                    $"office history records.");
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"Failed to deserialize " +
                    $"office history: " +
                    $"{ex.Message}");


                _history = [];
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Failed to load office history: " +
                    $"{ex.Message}");


                _history = [];
            }
        }
    }


    // ==================================================
    // Save
    // ==================================================

    private static void Save()
    {
        try
        {
            string json =
                JsonSerializer.Serialize(
                    _history,
                    _jsonOptions);


            File.WriteAllText(
                _filePath,
                json);


            Console.WriteLine(
                $"Saved {_history.Count} " +
                $"office history records.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to save office history: " +
                $"{ex.Message}");


            throw;
        }
    }


    // ==================================================
    // Clone
    // ==================================================

    private static OfficeHistory Clone(
        OfficeHistory record)
    {
        return new OfficeHistory
        {
            UserId =
                record.UserId,

            Date =
                record.Date,

            TimeInOffice =
                record.TimeInOffice,

            StartTime =
                record.StartTime,

            TimeOutOfOffice =
                record.TimeOutOfOffice?
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value)
                ?? []
        };
    }


    // ==================================================
    // Find Record
    // ==================================================

    private static OfficeHistory? FindRecord(
        int userId,
        DateOnly date)
    {
        return _history.FirstOrDefault(
            x =>
                x.UserId == userId &&
                x.Date == date);
    }


    // ==================================================
    // Create Record
    // ==================================================

    private static OfficeHistory CreateRecord(
        int userId,
        DateOnly date,
        TimeSpan timeInOffice = default,
        DateTime? startTime = null)
    {
        return new OfficeHistory
        {
            UserId =
                userId,

            Date =
                date,

            TimeInOffice =
                timeInOffice,

            StartTime =
                startTime,

            TimeOutOfOffice =
                []
        };
    }
}