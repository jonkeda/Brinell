global using Xunit;
global using Brinell.Core.Locators;
global using Brinell.Html.Interfaces;
global using Brinell.Html.Pages;
global using Brinell.Blazor.Pages;
global using Brinell.Samples.Todo.Contracts;
global using Brinell.Samples.Todo.TestSupport;

// Every test named in the timings, so a run is its touch map (brinell tests touch-map).
[assembly: Brinell.Uat.TestScope]
