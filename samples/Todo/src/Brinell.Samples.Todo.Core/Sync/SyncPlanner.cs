using Brinell.Samples.Todo.Core.Models;

namespace Brinell.Samples.Todo.Core.Sync;

/// <summary>What to send to the server.</summary>
/// <param name="Puts">Todos to store on the server: new or changed locally.</param>
/// <param name="Deletes">Todos to delete on the server: deleted locally after the server had them.</param>
/// <param name="Purges">Tombstones to drop without a call: deleted before the server ever had them.</param>
public sealed record PushPlan(
    IReadOnlyList<TodoItem> Puts,
    IReadOnlyList<Guid> Deletes,
    IReadOnlyList<Guid> Purges);

/// <summary>What to change locally after reading the server's list.</summary>
/// <param name="Store">Server versions to store as synced.</param>
/// <param name="Remove">Local todos the server no longer has.</param>
public sealed record PullPlan(
    IReadOnlyList<TodoItem> Store,
    IReadOnlyList<Guid> Remove);

/// <summary>
/// Decides what a sync does, from the local and the server state alone.
/// </summary>
/// <remarks>
/// Pure, so every rule in it is a unit test: which todos are sent, which tombstones need a call,
/// and who wins when both sides changed. The sync service only carries the plan out.
/// </remarks>
public static class SyncPlanner
{
    /// <summary>Plans the push half: what the server has to be told.</summary>
    public static PushPlan PlanPush(IEnumerable<TodoItem> local)
    {
        var puts = new List<TodoItem>();
        var deletes = new List<Guid>();
        var purges = new List<Guid>();

        foreach (var item in local)
        {
            if (item.IsDeleted)
            {
                // Never sent, so the server has nothing to delete (TOD.06.5).
                if (item.SyncState == SyncState.LocalOnly)
                {
                    purges.Add(item.Id);
                }
                else
                {
                    deletes.Add(item.Id);
                }
            }
            else if (item.NeedsPush)
            {
                puts.Add(item);
            }
        }

        return new PushPlan(puts, deletes, purges);
    }

    /// <summary>
    /// Plans the pull half: how the server's list changes the local one.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>A todo only the server has is stored.</item>
    /// <item>A synced local todo takes the server's version when it differs.</item>
    /// <item>A local change not yet pushed keeps its version unless the server's is newer
    /// (last writer wins on <see cref="TodoItem.UpdatedAt"/>; TOD.07.6).</item>
    /// <item>A local tombstone is left alone: its delete goes out at the next push.</item>
    /// <item>A synced local todo the server no longer has was deleted elsewhere, and is removed.
    /// A local change the server has not seen yet is kept.</item>
    /// </list>
    /// </remarks>
    public static PullPlan PlanPull(IEnumerable<TodoItem> local, IEnumerable<TodoItem> server)
    {
        var localById = local.ToDictionary(item => item.Id);
        var serverById = server.ToDictionary(item => item.Id);

        var store = new List<TodoItem>();
        foreach (var remote in serverById.Values)
        {
            var synced = remote with { SyncState = SyncState.Synced, IsDeleted = false };

            if (!localById.TryGetValue(remote.Id, out var mine))
            {
                store.Add(synced);
            }
            else if (mine.IsDeleted)
            {
                continue;
            }
            else if (mine.SyncState == SyncState.Synced)
            {
                if (!SameContent(mine, synced))
                {
                    store.Add(synced);
                }
            }
            else if (remote.UpdatedAt > mine.UpdatedAt)
            {
                store.Add(synced);
            }
        }

        var remove = localById.Values
            .Where(mine => mine.SyncState == SyncState.Synced && !serverById.ContainsKey(mine.Id))
            .Select(mine => mine.Id)
            .ToList();

        return new PullPlan(store, remove);
    }

    private static bool SameContent(TodoItem a, TodoItem b)
        => a.Title == b.Title
            && a.Notes == b.Notes
            && a.DueDate == b.DueDate
            && a.Status == b.Status
            && a.CreatedAt == b.CreatedAt
            && a.UpdatedAt == b.UpdatedAt;
}
