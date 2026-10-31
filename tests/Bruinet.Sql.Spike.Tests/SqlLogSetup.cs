using System.Runtime.CompilerServices;
using Bruinet.Core.Testing;
using Bruinet.Mocking.Data;

namespace Bruinet.Sql.Spike.Tests;

/// <summary>The SQL log names the test that is running, from Bruinet's <see cref="TestScope"/>.</summary>
internal static class SqlLogSetup
{
    [ModuleInitializer]
    internal static void NameTheTest() => SqlLogScope.Current = () => TestScope.Current.Test;
}
