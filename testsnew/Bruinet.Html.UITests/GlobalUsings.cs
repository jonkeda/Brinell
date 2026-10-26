global using Xunit;
global using Bruinet.Core.Interfaces;
global using Bruinet.Core.Locators;
global using Bruinet.Html.Context;
global using Bruinet.Html.Controls;
global using Bruinet.Html.Interfaces;
global using Bruinet.Html.Pages;
global using Bruinet.Html.Playwright;

// The UAT suite (Uat/, Features/).
global using Bruinet.Core.Composition;
global using Bruinet.Uat;
global using Microsoft.Extensions.DependencyInjection;

// Every test named in the timings, so a run is its touch map (bruinet touch-map).
[assembly: Bruinet.Uat.TestScope]
