
namespace ToggleAvailabilityBlazor.Models;

public class OfficeHistory
{
    public int UserId { get; set; }

    public DateOnly Date { get; set; }

    public TimeSpan TimeInOffice { get; set; }

    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Stores the total amount of time the user spent
    /// out of the office for each reason during the day.
    ///
    /// OutForTheDay is intentionally not stored.
    /// </summary>
    public Dictionary<Status, TimeSpan> TimeOutOfOffice { get; set; } = [];
}