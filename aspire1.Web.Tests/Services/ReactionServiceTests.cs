using aspire1.Web.Services;

namespace aspire1.Web.Tests.Services;

public class ReactionServiceTests
{
    private static ReactionService CreateService(IReactionNotifier? notifier = null) =>
        new(notifier ?? Substitute.For<IReactionNotifier>());

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    // ── GetReactionCountsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetReactionCountsAsync_WhenNoReactions_ReturnsAllZeros()
    {
        var svc = CreateService();

        var counts = await svc.GetReactionCountsAsync(Today);

        counts.Should().HaveCount(ReactionService.SupportedEmojis.Length);
        counts.Values.Should().AllSatisfy(v => v.Should().Be(0));
    }

    [Fact]
    public async Task GetReactionCountsAsync_ReturnsCountForAllSupportedEmojis()
    {
        var svc = CreateService();

        var counts = await svc.GetReactionCountsAsync(Today);

        foreach (var emoji in ReactionService.SupportedEmojis)
            counts.Should().ContainKey(emoji);
    }

    // ── AddReactionAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddReactionAsync_ValidEmoji_ReturnsTrue()
    {
        var svc = CreateService();

        var result = await svc.AddReactionAsync(Today, "☀️");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddReactionAsync_UnsupportedEmoji_ReturnsFalse()
    {
        var svc = CreateService();

        var result = await svc.AddReactionAsync(Today, "🦄");

        result.Should().BeFalse("unsupported emojis must be rejected");
    }

    [Fact]
    public async Task AddReactionAsync_SameEmojiTwice_SecondCallReturnsFalse()
    {
        var svc = CreateService();

        await svc.AddReactionAsync(Today, "👍");
        var second = await svc.AddReactionAsync(Today, "👍");

        second.Should().BeFalse("circuit-scoped rate limiting must block duplicate votes");
    }

    [Fact]
    public async Task AddReactionAsync_DifferentEmojis_BothSucceed()
    {
        var svc = CreateService();

        var first = await svc.AddReactionAsync(Today, "☀️");
        var second = await svc.AddReactionAsync(Today, "🔥");

        first.Should().BeTrue();
        second.Should().BeTrue("different emojis are independent votes");
    }

    [Fact]
    public async Task AddReactionAsync_SameEmojiDifferentDates_BothSucceed()
    {
        var svc = CreateService();
        var tomorrow = Today.AddDays(1);

        var today = await svc.AddReactionAsync(Today, "❄️");
        var tomorrowResult = await svc.AddReactionAsync(tomorrow, "❄️");

        today.Should().BeTrue();
        tomorrowResult.Should().BeTrue("same emoji on different dates is allowed");
    }

    [Fact]
    public async Task AddReactionAsync_NotifiesSubscribers()
    {
        var notifier = Substitute.For<IReactionNotifier>();
        var svc = CreateService(notifier);

        await svc.AddReactionAsync(Today, "🤔");

        await notifier.Received(1).NotifyAsync(Today, Arg.Any<IReadOnlyDictionary<string, int>>());
    }

    [Fact]
    public async Task AddReactionAsync_DuplicateVote_DoesNotNotifySubscribers()
    {
        var notifier = Substitute.For<IReactionNotifier>();
        var svc = CreateService(notifier);

        await svc.AddReactionAsync(Today, "👍");
        await svc.AddReactionAsync(Today, "👍");

        await notifier.Received(1).NotifyAsync(Arg.Any<DateOnly>(), Arg.Any<IReadOnlyDictionary<string, int>>());
    }

    [Fact]
    public async Task AddReactionAsync_UpdatesCountInMemoryFallback()
    {
        var svc = CreateService();

        await svc.AddReactionAsync(Today, "🔥");
        var counts = await svc.GetReactionCountsAsync(Today);

        counts["🔥"].Should().Be(1, "count must increment after one vote");
    }

    // ── HasVoted ──────────────────────────────────────────────────────────────

    [Fact]
    public void HasVoted_BeforeVoting_ReturnsFalse()
    {
        var svc = CreateService();

        svc.HasVoted(Today, "☀️").Should().BeFalse();
    }

    [Fact]
    public async Task HasVoted_AfterVoting_ReturnsTrue()
    {
        var svc = CreateService();

        await svc.AddReactionAsync(Today, "☀️");

        svc.HasVoted(Today, "☀️").Should().BeTrue();
    }

    [Fact]
    public async Task HasVoted_AfterVotingOneEmoji_OtherEmojiReturnsFalse()
    {
        var svc = CreateService();

        await svc.AddReactionAsync(Today, "☀️");

        svc.HasVoted(Today, "👍").Should().BeFalse("only voted emoji should be marked");
    }

    // ── SupportedEmojis ───────────────────────────────────────────────────────

    [Fact]
    public void SupportedEmojis_ContainsAllFiveExpectedEmojis()
    {
        ReactionService.SupportedEmojis.Should().Contain(["☀️", "👍", "🤔", "❄️", "🔥"]);
    }
}
