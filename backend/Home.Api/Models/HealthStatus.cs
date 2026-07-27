namespace Home.Api.Models;

public class HealthStatus
{
    public Guid ServiceId { get; set; }
    public bool IsUp { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    public ServiceLink Service { get; set; } = null!;
}
