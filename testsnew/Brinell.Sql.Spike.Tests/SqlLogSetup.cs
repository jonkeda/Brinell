using System.Runtime.CompilerServices;
using Brinell.Core.Testing;
using Brinell.Mocking.Data;

namespace Brinell.Sql.Spike.Tests;

/// <summary>The SQL log names the test that is running, from Brinell's <see cref="TestScope"/>.</summary>
internal static class SqlLogSetup
{
    [ModuleInitializer]
    internal static void NameTheTest() => SqlLogScope.Current = () => TestScope.Current.Test;
}
