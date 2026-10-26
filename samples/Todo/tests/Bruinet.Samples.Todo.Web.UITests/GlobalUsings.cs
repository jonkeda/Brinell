global using Xunit;
global using Bruinet.Core.Locators;
global using Bruinet.Html.Interfaces;
global using Bruinet.Html.Pages;
global using Bruinet.Blazor.Pages;
global using Bruinet.Samples.Todo.Contracts;
global using Bruinet.Samples.Todo.TestSupport;

// Every test named in the timings, so a run is its touch map (bruinet tests touch-map).
[assembly: Bruinet.Uat.TestScope]
