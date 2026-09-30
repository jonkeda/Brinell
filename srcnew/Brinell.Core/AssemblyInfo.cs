using System.Runtime.CompilerServices;

// The calls layer (Brinell.Core.Calls) is internal: a mechanism for the stacks' control bases,
// not an API for tests.
[assembly: InternalsVisibleTo("Brinell.Maui")]
[assembly: InternalsVisibleTo("Brinell.Maui.Tests")]
[assembly: InternalsVisibleTo("Brinell.Html")]
[assembly: InternalsVisibleTo("Brinell.Html.Tests")]
