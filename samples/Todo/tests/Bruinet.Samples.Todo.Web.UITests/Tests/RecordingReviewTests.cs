using Bruinet.Mocking.Data.Replay;
using Bruinet.Samples.Todo.TestSupport;

namespace Bruinet.Samples.Todo.Web.UITests.Tests;

/// <summary>
/// The gate on re-records (.my/sql-replay/plan.md, step 8): every recording as a person accepted it after reading its
/// summary (bruinet sql diff --recordings Recordings --accept). Not while recording, which rewrites them.
/// </summary>
public sealed class RecordingReviewTests
{
    [Fact]
    public void Recordings_AreAllReviewed()
    {
        if (TodoSqlReplay.ModeOfRun == ReplayMode.Record)
        {
            return;
        }

        Assert.Empty(RecordingReview.Unreviewed(TodoSqlReplay.RecordingsOf("Bruinet.Samples.Todo.Web.UITests")));
    }
}
