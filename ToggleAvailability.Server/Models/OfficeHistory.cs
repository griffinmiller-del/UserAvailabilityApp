namespace ToggleAvailability.Server.Models;

public class OfficeHistory
{
    public int UserId { get; set; }


    public DateOnly Date { get; set; }


    public TimeSpan TimeInOffice { get; set; }


    public DateTime? StartTime { get; set; }


    // ==================================================
    // EF Core relationship
    // ==================================================

    public List<OfficeHistoryOutOfOffice> OutOfOfficeEntries
    {
        get;
        set;
    } = [];
}