using System.Runtime.CompilerServices;
using Bruinell.Core.Testing;
using Bruinell.Mocking.Data;

namespace Bruinell.Samples.Todo.Web.UITests;

/// <summary>The SQL log names the test that is running, from Bruinell's <see cref="TestScope"/>.</summary>
internal static class SqlLogSetup
{
    [ModuleInitializer]
    internal static void NameTheTest() => SqlLogScope.Current = () => TestScope.Current.Test;
}
