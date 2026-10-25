global using Xunit;
global using Bruinell.Core.Locators;
global using Bruinell.Html.Interfaces;
global using Bruinell.Blazor.Pages;
global using Bruinell.Blazor.Controls;
global using Bruinell.Blazor.Context;
global using Bruinell.Html.Pages;

// Every test in its TestScope, with a test span in the timings: the SQL report joins the log to the tests by it.
[assembly: Bruinell.Uat.TestScope]
