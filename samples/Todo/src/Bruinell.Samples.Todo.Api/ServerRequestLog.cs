using System.Collections.Concurrent;

namespace Bruinell.Samples.Todo.Api;

/// <summary>One accepted API request: what it was and which client sent it.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The request path.</param>
/// <param name="ClientId">The <c>X-Todo-Client</c> header, or <c>null</c> when the client sent none.</param>
public sealed record ServerRequest(string Method, string Path, string? ClientId);

/// <summary>
/// The requests the API accepted, in arrival order, kept in memory while it runs.
/// </summary>
/// <remarks>
/// So a test that runs several clients against one API can check which client sent what, the way
/// WireMock's request log does for the hermetic tiers.
/// </remarks>
public sealed class ServerRequestLog
{
    private readonly ConcurrentQueue<ServerRequest> _requests = new();

    /// <summary>Every accepted request so far.</summary>
    public IReadOnlyList<ServerRequest> All => [.. _requests];

    /// <summary>Records one request.</summary>
    public void Add(ServerRequest request) => _requests.Enqueue(request);
}
