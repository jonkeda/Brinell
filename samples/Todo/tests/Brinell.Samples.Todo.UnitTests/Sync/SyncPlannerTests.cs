namespace Brinell.Samples.Todo.UnitTests.Sync;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
public sealed class SyncPlannerTests
{
    [Fact]
    [Trait("Journey", "TOD.07.1")]
    public void PlanPush_SendsNewAndChangedTodos_NotSyncedOnes()
    {
        var created = Build.Todo("Created", sync: SyncState.LocalOnly);
        var changed = Build.Todo("Changed", sync: SyncState.Pending);
        var synced = Build.Todo("Synced");

        var plan = SyncPlanner.PlanPush([created, changed, synced]);

        Assert.Equal([created, changed], plan.Puts);
        Assert.Empty(plan.Deletes);
        Assert.Empty(plan.Purges);
    }

    [Fact]
    [Trait("Journey", "TOD.07.5")]
    public void PlanPush_DeletesOnTheServer_WhatTheServerHad()
    {
        var tombstone = Build.Todo(sync: SyncState.Pending, deleted: true);

        var plan = SyncPlanner.PlanPush([tombstone]);

        Assert.Equal([tombstone.Id], plan.Deletes);
        Assert.Empty(plan.Puts);
    }

    [Fact]
    [Trait("Journey", "TOD.06.5")]
    public void PlanPush_PurgesWithoutACall_WhatTheServerNeverHad()
    {
        var neverSent = Build.Todo(sync: SyncState.LocalOnly, deleted: true);

        var plan = SyncPlanner.PlanPush([neverSent]);

        Assert.Equal([neverSent.Id], plan.Purges);
        Assert.Empty(plan.Deletes);
        Assert.Empty(plan.Puts);
    }

    [Fact]
    [Trait("Journey", "TOD.07.3")]
    public void PlanPull_StoresTodosOnlyTheServerHas_AsSynced()
    {
        var remote = Build.Todo("From another device");

        var plan = SyncPlanner.PlanPull([], [remote]);

        Assert.Equal(SyncState.Synced, Assert.Single(plan.Store).SyncState);
        Assert.Empty(plan.Remove);
    }

    [Fact]
    [Trait("Journey", "TOD.07.7")]
    public void PlanPull_WhenNothingDiffers_ChangesNothing()
    {
        var same = Build.Todo();

        var plan = SyncPlanner.PlanPull([same], [same]);

        Assert.Empty(plan.Store);
        Assert.Empty(plan.Remove);
    }

    [Fact]
    [Trait("Journey", "TOD.07.3")]
    public void PlanPull_TakesTheServersVersionOfASyncedTodo()
    {
        var id = Guid.NewGuid();
        var mine = Build.Todo("Old title", id: id);
        var theirs = Build.Todo("New title", id: id, updated: Build.Now);

        var plan = SyncPlanner.PlanPull([mine], [theirs]);

        Assert.Equal("New title", Assert.Single(plan.Store).Title);
    }

    // The conflict table for TOD.07.6: a local change the server has not seen yet, against a
    // server version changed elsewhere. The newer UpdatedAt wins; a tie keeps the local change,
    // which the server will then accept on the next push.
    [Theory]
    [Trait("Journey", "TOD.07.6")]
    [InlineData(-60, false)]
    [InlineData(0, false)]
    [InlineData(60, true)]
    public void PlanPull_Conflict_TheNewerUpdatedAtWins(int serverMinutesLater, bool serverWins)
    {
        var id = Guid.NewGuid();
        var mine = Build.Todo("Mine", sync: SyncState.Pending, updated: Build.Now, id: id);
        var theirs = Build.Todo("Theirs", updated: Build.Now.AddMinutes(serverMinutesLater), id: id);

        var plan = SyncPlanner.PlanPull([mine], [theirs]);

        Assert.Equal(serverWins, plan.Store.Count == 1);
        Assert.Empty(plan.Remove);
    }

    [Fact]
    [Trait("Journey", "TOD.07.5")]
    public void PlanPull_RemovesSyncedTodosTheServerNoLongerHas()
    {
        var deletedElsewhere = Build.Todo();

        var plan = SyncPlanner.PlanPull([deletedElsewhere], []);

        Assert.Equal([deletedElsewhere.Id], plan.Remove);
    }

    [Fact]
    [Trait("Journey", "TOD.07.1")]
    public void PlanPull_KeepsLocalChangesTheServerHasNotSeen()
    {
        var created = Build.Todo(sync: SyncState.LocalOnly);
        var changed = Build.Todo(sync: SyncState.Pending);

        var plan = SyncPlanner.PlanPull([created, changed], []);

        Assert.Empty(plan.Remove);
        Assert.Empty(plan.Store);
    }

    [Fact]
    [Trait("Journey", "TOD.06.4")]
    public void PlanPull_LeavesATombstoneAlone_EvenIfTheServerStillHasTheTodo()
    {
        var id = Guid.NewGuid();
        var tombstone = Build.Todo(sync: SyncState.Pending, deleted: true, id: id);
        var stillThere = Build.Todo(id: id, updated: Build.Now.AddDays(1));

        var plan = SyncPlanner.PlanPull([tombstone], [stillThere]);

        Assert.Empty(plan.Store);
        Assert.Empty(plan.Remove);
    }
}
