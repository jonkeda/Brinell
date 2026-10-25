using System.Runtime.CompilerServices;

// The calls layer (Bruinell.Core.Calls) is internal: a mechanism for the stacks' control bases,
// not an API for tests.
[assembly: InternalsVisibleTo("Bruinell.Maui")]
[assembly: InternalsVisibleTo("Bruinell.Maui.Tests")]
[assembly: InternalsVisibleTo("Bruinell.Html")]
[assembly: InternalsVisibleTo("Bruinell.Html.Tests")]
[assembly: InternalsVisibleTo("Bruinell.NativeAndroid")]
[assembly: InternalsVisibleTo("Bruinell.NativeAndroid.Tests")]
