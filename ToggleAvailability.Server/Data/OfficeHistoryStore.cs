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


    /// <summary>
    /// Gets the total office history of a specific user
    /// </summary>
    /// <param name="userId">The id of the user that the records are being gathered for</param>
    /// <returns>A list of office records for the given user</returns>
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


    /// <summary>
    /// Sets the TimeInOffice of a history record
    /// </summary>
    /// <param name="userId">The userid of the user the record belongs to</param>
    /// <param name="date">The date of the record</param>
    /// <param name="duration">The length of time the user was in the office</param>

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
                _history.FirstOrDefault(
                    x =>
                        x.UserId == userId &&
                        x.Date == date);

            if (existing is null)
            {
                _history.Add(CreateRecord(userId,  date, duration));
            }
            else
            {
                existing.TimeInOffice +=
                    duration;
            }

            Save();
        }
    }

    /// <summary>
    /// Creates a new daily record for a user
    /// </summary>
    /// <param name="userId">The user the record is being created for</param>
    /// <param name="date">The date of the record being created</param>

    public static void CreateDailyRecord(
        int userId,
        DateOnly date)
    {
        lock (_lock)
        {
            if (FindRecord(userId, date) is not null)
            {
                return;
            }

            _history.Add(CreateRecord(userId, date));

            Save();
        }
    }



    /// <summary>
    /// Loads the office history objects from the json into a list
    /// </summary>
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


    /// <summary>
    /// Saves the office history objects to the json
    /// </summary>
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


    /// <summary>
    /// Clones office history record so the original record is protected
    /// </summary>
    /// <param name="record">The record to clone</param>
    /// <returns>The cloned record</returns>
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
                record.StartTime
        };
    }

    /// <summary>
    /// Gets a copy of an office history record for a user on a specific date
    /// </summary>
    /// <param name="userId">The userid of the record being searched for</param>
    /// <param name="date">The date of the record being searched for</param>
    /// <returns>A copy of the office history record, if found</returns>
    public static OfficeHistory? GetUserHistoryForDate(
        int userId,
        DateOnly date)
    {
        lock (_lock)
        {
            var record = FindRecord(userId, date);

            return record is null
                ? null
                : Clone(record);
        }
    }

    /// <summary>
    /// Sets the time of the first punch-in for a user for a day
    /// </summary>
    /// <param name="userId">The id of the user getting their start time set</param>
    /// <param name="date">The date that the start time is to be set on</param>
    /// <param name="startTime">The time to be set as the start time for the day</param>
    public static void SetStartTime(
        int userId,
        DateOnly date,
        DateTime startTime)
    {
        lock (_lock)
        {
            var existing = FindRecord(userId, date);

            if (existing is null)
            {
                _history.Add(CreateRecord(userId, date, TimeSpan.Zero, startTime));

                Save();

                return;
            }

            // Only record the first clock-in
            // of the day.
            if (existing.StartTime is null)
            {
                existing.StartTime = startTime;

                Save();
            }
        }
    }

    /// <summary>
    /// Searches the office history file for a specific user on a specific date
    /// </summary>
    /// <param name="userId">The userId of the user being searched for</param>
    /// <param name="date">The date of the record being searched for</param>
    /// <returns>The OfficeHistory object of the record, if found</returns>
    private static OfficeHistory? FindRecord(
    int userId,
    DateOnly date)
    {
        return _history.FirstOrDefault(
            x =>
                x.UserId == userId &&
                x.Date == date);
    }


    /// <summary>
    /// Creates a new history record
    /// </summary>
    /// <param name="userId">The userid of the user that the record is being created for</param>
    /// <param name="date">The date of the record</param>
    /// <param name="timeInOffice">The total time the user was in the office for the day</param>
    /// <param name="startTime">The time of the user's first punch-in</param>
    /// <returns>The OfficeHistory object for this record</returns>
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
                startTime
        };
    }

}