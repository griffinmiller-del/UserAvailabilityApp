
namespace ToggleAvailabilityBlazor.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }

        public Status Status { get; set; }
    }
}
