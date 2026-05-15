using Microsoft.AspNetCore.SignalR;

namespace aspire1.Web.Hubs;

/// <summary>
/// Minimal SignalR hub for weather emoji reactions.
/// Real-time updates to Blazor Server circuits are delivered via <see cref="Services.IReactionNotifier"/>.
/// This hub exists so that <see cref="IHubContext{ReactionHub}"/> can be injected and for future
/// JavaScript client extensibility.
/// Maps to /hubs/reactions.
/// </summary>
public sealed class ReactionHub : Hub
{
}
