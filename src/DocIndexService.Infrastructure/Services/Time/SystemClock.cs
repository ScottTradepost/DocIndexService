using DocIndexService.Core.Interfaces;

namespace DocIndexService.Infrastructure.Services.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
