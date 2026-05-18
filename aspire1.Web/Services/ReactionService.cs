using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using StackExchange.Redis;

namespace aspire1.Web.Services;

/// <summary>
/// Scoped per Blazor Server circuit. Stores and retrieves emoji reaction counts via Redis
/// (using atomic INCR via IConnectionMultiplexer) with an in-memory fallback for local development.
/// Circuit-scoped rate limiting is enforced by a HashSet — one vote per emoji per forecast per circuit.
/// </summary>
public sealed class ReactionService(IReactionNotifier notifier, IConnectionMultiplexer? redis = null)
{
    /// <summary>Ordered list of supported reaction emojis.</summary>
    public static readonly string[] SupportedEmojis = ["☀️", "👍", "🤔", "❄️", "🔥"];

    private static readonly HashSet<string> EmojiSet = [..SupportedEmojis];

    /// <summary>
    /// Circuit-scoped voted tracker. Prevents a single circuit from voting more than once
    /// per emoji per date. Key format: "{date:yyyy-MM-dd}:{emoji}".
    /// </summary>
    private readonly HashSet<string> _voted = [];

    /// <summary>In-memory fallback store for local development (no Redis). Instance-level: dev limitation, prod uses Redis.</summary>
    private readonly Dictionary<string, long> _memoryStore = [];
    private readonly object _memoryLock = new();

    private static string RedisKey(DateOnly date, string emoji) =>
        $"reactions:{date:yyyy-MM-dd}:{emoji}";

    /// <summary>
    /// Returns current reaction counts for all supported emojis on the given date.
    /// Returns zeros on cache miss or error.
    /// </summary>
    public async Task<Dictionary<string, int>> GetReactionCountsAsync(DateOnly date)
    {
        var result = SupportedEmojis.ToDictionary(e => e, _ => 0);

        if (redis is not null)
        {
            try
            {
                var db = redis.GetDatabase();
                foreach (var emoji in SupportedEmojis)
                {
                    var val = await db.StringGetAsync(RedisKey(date, emoji));
                    if (val.HasValue && long.TryParse((string?)val, out var count))
                        result[emoji] = (int)Math.Min(count, int.MaxValue);
                }
            }
            catch
            {
                // Redis unavailable — return zeros rather than crashing
            }
        }
        else
        {
            lock (_memoryLock)
            {
                foreach (var emoji in SupportedEmojis)
                {
                    var key = RedisKey(date, emoji);
                    if (_memoryStore.TryGetValue(key, out var count))
                        result[emoji] = (int)Math.Min(count, int.MaxValue);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Increments the reaction count for <paramref name="emoji"/> on <paramref name="date"/>.
    /// Returns false if the emoji is unsupported or this circuit has already voted for this combo.
    /// Uses atomic INCR (Redis) or locked dictionary increment (dev fallback).
    /// Broadcasts updated counts to all subscribed circuits via IReactionNotifier.
    /// </summary>
    public async Task<bool> AddReactionAsync(DateOnly date, string emoji)
    {
        if (!EmojiSet.Contains(emoji)) return false;

        var voteKey = $"{date:yyyy-MM-dd}:{emoji}";
        if (!_voted.Add(voteKey)) return false;

        if (redis is not null)
        {
            try
            {
                var db = redis.GetDatabase();
                var key = RedisKey(date, emoji);
                var newCount = await db.StringIncrementAsync(key);
                if (newCount == 1)
                    await db.KeyExpireAsync(key, TimeSpan.FromHours(25));
            }
            catch
            {
                // Redis write failed — remove from voted so user can retry
                _voted.Remove(voteKey);
                return false;
            }
        }
        else
        {
            lock (_memoryLock)
            {
                var key = RedisKey(date, emoji);
                _memoryStore[key] = _memoryStore.GetValueOrDefault(key) + 1;
            }
        }

        EmitTelemetry(date, emoji);

        var updatedCounts = await GetReactionCountsAsync(date);
        await notifier.NotifyAsync(date, updatedCounts);
        return true;
    }

    /// <summary>
    /// Returns true if this circuit has already voted for the given emoji/date combination.
    /// </summary>
    public bool HasVoted(DateOnly date, string emoji) =>
        _voted.Contains($"{date:yyyy-MM-dd}:{emoji}");

    private static void EmitTelemetry(DateOnly date, string emoji)
    {
        var offset = (date.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;
        ApplicationMetrics.WeatherReactions.Add(1,
            new TagList { { "emoji", emoji }, { "forecast_offset", offset.ToString() } });
    }
}
