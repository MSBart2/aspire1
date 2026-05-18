namespace aspire1.Web.Services;

/// <summary>
/// Broadcasts reaction count updates to all subscribed Blazor Server circuits.
/// Each circuit subscribes on component mount and unsubscribes on dispose.
/// </summary>
public interface IReactionNotifier
{
    void Subscribe(Func<DateOnly, IReadOnlyDictionary<string, int>, Task> handler);
    void Unsubscribe(Func<DateOnly, IReadOnlyDictionary<string, int>, Task> handler);
    Task NotifyAsync(DateOnly date, IReadOnlyDictionary<string, int> counts);
}

/// <summary>
/// Thread-safe singleton implementation using an explicit subscriber list with Task.WhenAll fan-out.
/// Uses a Lock (System.Threading.Lock, .NET 9+) and snapshot-before-notify to avoid holding
/// the lock across async boundaries. Safe for concurrent subscribe/unsubscribe across circuits.
/// </summary>
public sealed class ReactionNotifier : IReactionNotifier
{
    private readonly List<Func<DateOnly, IReadOnlyDictionary<string, int>, Task>> _subscribers = [];
    private readonly Lock _lock = new();

    public void Subscribe(Func<DateOnly, IReadOnlyDictionary<string, int>, Task> handler)
    {
        lock (_lock) _subscribers.Add(handler);
    }

    public void Unsubscribe(Func<DateOnly, IReadOnlyDictionary<string, int>, Task> handler)
    {
        lock (_lock) _subscribers.Remove(handler);
    }

    public async Task NotifyAsync(DateOnly date, IReadOnlyDictionary<string, int> counts)
    {
        List<Func<DateOnly, IReadOnlyDictionary<string, int>, Task>> snapshot;
        lock (_lock) snapshot = [.._subscribers];
        await Task.WhenAll(snapshot.Select(h => h(date, counts)));
    }
}
