using System.Text.Json;
using System.Text.Json.Serialization;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Data;

public static class OfficeHistoryStore
{
    private static readonly string _filePath =
        Path.Combine(
            AppContext.BaseDirectory,
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
    // Get history for user
    // ==================================================

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
    // Add office time
    // ==================================================

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
                _history.Add(
                    new OfficeHistory
                    {
                        UserId =
                            userId,

                        Date =
                            date,

                        TimeInOffice =
                            duration
                    });
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
                record.TimeInOffice
        };
    }
}