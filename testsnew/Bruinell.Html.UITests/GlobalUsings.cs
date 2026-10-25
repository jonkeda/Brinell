global using Xunit;
global using Bruinell.Core.Interfaces;
global using Bruinell.Core.Locators;
global using Bruinell.Html.Context;
global using Bruinell.Html.Controls;
global using Bruinell.Html.Interfaces;
global using Bruinell.Html.Pages;
global using Bruinell.Html.Playwright;

// The UAT suite (Uat/, Features/).
global using Bruinell.Core.Composition;
global using Bruinell.Uat;
global using Microsoft.Extensions.DependencyInjection;

// Every test named in the timings, so a run is its touch map (bruinell touch-map).
[assembly: Bruinell.Uat.TestScope]
