global using Xunit;
global using Brinell.Core.Locators;
global using Brinell.Html.Interfaces;
global using Brinell.Blazor.Pages;
global using Brinell.Blazor.Controls;
global using Brinell.Blazor.Context;
global using Brinell.Html.Pages;

// Every test in its TestScope, with a test span in the timings: the SQL report joins the log to the tests by it.
[assembly: Brinell.Uat.TestScope]
