using System.Text.Json.Serialization;

namespace ToggleAvailability.Server.Models;

public class OfficeHistoryOutOfOffice
{
    public int UserId { get; set; }

    public DateOnly Date { get; set; }

    public Status Status { get; set; }

    public TimeSpan Duration { get; set; }

    [JsonIgnore]
    public OfficeHistory? OfficeHistory { get; set; }
}