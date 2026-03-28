namespace DocIndexService.Core.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}
