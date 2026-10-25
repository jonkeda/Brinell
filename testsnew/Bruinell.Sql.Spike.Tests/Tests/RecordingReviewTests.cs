using Bruinell.Mocking.Data.Replay;
using Bruinell.Sql.Spike.Tests.TestBase;

namespace Bruinell.Sql.Spike.Tests.Tests;

/// <summary>
/// The gate on re-records (.my/sql-replay/plan.md, M6): every recording as a person accepted it after reading its summary
/// (bruinell sql diff --recordings Recordings --accept). Not while recording, which rewrites them.
/// </summary>
public sealed class RecordingReviewTests
{
    [Fact]
    public void Recordings_AreAllReviewed()
    {
        if (SpikeTestBase.Mode == ReplayMode.Record)
        {
            return;
        }

        Assert.Empty(RecordingReview.Unreviewed(Schemas.Recordings));
    }
}
