global using Xunit;
global using Bruinet.Core.Locators;
global using Bruinet.Html.Interfaces;
global using Bruinet.Blazor.Pages;
global using Bruinet.Blazor.Controls;
global using Bruinet.Blazor.Context;
global using Bruinet.Html.Pages;

// Every test in its TestScope, with a test span in the timings: the SQL report joins the log to the tests by it.
[assembly: Bruinet.Uat.TestScope]
