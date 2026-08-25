namespace ToggleAvailabilityBlazor.Models
{
    public class OfficeHistory
    {
        public int UserId { get; set; }

        public DateOnly Date { get; set; }

        public TimeSpan TimeInOffice { get; set; }

        public DateTime? StartTime { get; set; }
    }
}
