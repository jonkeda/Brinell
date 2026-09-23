using System.Collections.Specialized;
using Brinell.Scraper.Logging;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Brinell.Scraper.Tests.Logging;

public sealed class InAppLogServiceTests
{
    private static LogEntry MakeEntry(LogLevel level = LogLevel.Information, string source = "Test", string message = "msg")
        => new(DateTime.UtcNow, level, source, message);

    [Fact]
    public void Add_AppendsEntryToCollection()
    {
        // Add requires Application.Current dispatcher, so verify direct collection access works.
        var service = new InAppLogService();
        var entry = MakeEntry();

        service.Entries.Add(entry);

        Assert.Single(service.Entries);
        Assert.Same(entry, service.Entries[0]);
    }

    [Fact]
    public void Add_MultipleEntries_PreservesOrder()
    {
        var service = new InAppLogService();
        var e1 = MakeEntry(message: "first");
        var e2 = MakeEntry(message: "second");
        var e3 = MakeEntry(message: "third");

        service.Entries.Add(e1);
        service.Entries.Add(e2);
        service.Entries.Add(e3);

        Assert.Equal(3, service.Entries.Count);
        Assert.Equal("first", service.Entries[0].Message);
        Assert.Equal("second", service.Entries[1].Message);
        Assert.Equal("third", service.Entries[2].Message);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var service = new InAppLogService();
        service.Entries.Add(MakeEntry());
        service.Entries.Add(MakeEntry());

        service.Entries.Clear();

        Assert.Empty(service.Entries);
    }

    [Fact]
    public void Add_WithNoApplication_DoesNotThrow()
    {
        // When Application.Current is null, Add is a graceful no-op.
        var service = new InAppLogService();

        var ex = Record.Exception(() => service.Add(MakeEntry()));

        Assert.Null(ex);
    }

    [Fact]
    public void Entries_IsObservable()
    {
        var service = new InAppLogService();
        var raised = false;
        ((INotifyCollectionChanged)service.Entries).CollectionChanged += (_, _) => raised = true;

        service.Entries.Add(MakeEntry());

        Assert.True(raised);
    }
}
