using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Services;
using Brinell.Samples.Todo.Core.Sync;

namespace Brinell.Samples.Todo.Infrastructure.Sync;

/// <summary>
/// Carries out a sync: push what changed locally, then pull the server's list.
/// </summary>
/// <remarks>
/// <para>
/// The decisions are <see cref="SyncPlanner"/>'s; this class does the calls and the writes in
/// order, and stops at the first failure with everything not yet confirmed still pending, so
/// the next sync tries again (TOD.08.1, TOD.08.4).
/// </para>
/// <para>
/// One sync at a time: a second call waits for the first.
/// </para>
/// </remarks>
public sealed class SyncService(
    ITodoRepository repository,
    ITodoApi api,
    IConnectivityState connectivity,
    TimeProvider time) : ISyncService
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <inheritdoc />
    public SyncResult? LastResult { get; private set; }

    /// <inheritdoc />
    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            LastResult = await RunAsync(cancellationToken).ConfigureAwait(false);
            return LastResult;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SyncResult> RunAsync(CancellationToken cancellationToken)
    {
        if (!connectivity.IsOnline)
        {
            return new SyncResult(SyncError.Offline, time.GetUtcNow());
        }

        int pushed = 0, deleted = 0, pulled = 0, removed = 0;

        try
        {
            var push = SyncPlanner.PlanPush(await repository.GetAllAsync(cancellationToken).ConfigureAwait(false));

            foreach (var id in push.Purges)
            {
                await repository.PurgeAsync(id, cancellationToken).ConfigureAwait(false);
            }

            foreach (var item in push.Puts)
            {
                var stored = await api.PutAsync(item, cancellationToken).ConfigureAwait(false);
                await ConfirmAsync(item, stored with { SyncState = SyncState.Synced }, cancellationToken).ConfigureAwait(false);
                pushed++;
            }

            foreach (var id in push.Deletes)
            {
                await api.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
                await repository.PurgeAsync(id, cancellationToken).ConfigureAwait(false);
                deleted++;
            }

            var server = await api.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var pull = SyncPlanner.PlanPull(await repository.GetAllAsync(cancellationToken).ConfigureAwait(false), server);

            foreach (var item in pull.Store)
            {
                await repository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
                pulled++;
            }

            foreach (var id in pull.Remove)
            {
                await repository.PurgeAsync(id, cancellationToken).ConfigureAwait(false);
                removed++;
            }

            return new SyncResult(SyncError.None, time.GetUtcNow(), pushed, deleted, pulled, removed);
        }
        catch (TodoApiException failure)
        {
            return new SyncResult(failure.Error, time.GetUtcNow(), pushed, deleted, pulled, removed);
        }
    }

    /// <summary>
    /// Stores the server's answer to a push - unless the user changed the todo again while the
    /// request was out, in which case the newer local change stays pending.
    /// </summary>
    private async Task ConfirmAsync(TodoItem sent, TodoItem stored, CancellationToken cancellationToken)
    {
        var current = await repository.GetAsync(sent.Id, cancellationToken).ConfigureAwait(false);
        if (current is null || current.UpdatedAt != sent.UpdatedAt || current.IsDeleted)
        {
            return;
        }

        await repository.SaveAsync(stored, cancellationToken).ConfigureAwait(false);
    }
}
