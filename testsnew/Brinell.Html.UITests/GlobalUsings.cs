global using Xunit;
global using Brinell.Core.Interfaces;
global using Brinell.Core.Locators;
global using Brinell.Html.Context;
global using Brinell.Html.Controls;
global using Brinell.Html.Interfaces;
global using Brinell.Html.Pages;
global using Brinell.Html.Playwright;

// The UAT suite (Uat/, Features/).
global using Brinell.Core.Composition;
global using Brinell.Uat;
global using Microsoft.Extensions.DependencyInjection;

// Every test named in the timings, so a run is its touch map (brinell touch-map).
[assembly: Brinell.Uat.TestScope]
