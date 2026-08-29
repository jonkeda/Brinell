global using System;
global using System.Collections.ObjectModel;
global using System.Threading.Tasks;
global using System.Windows.Input;
global using Brinell.Samples.Shared.Commands;
global using Brinell.Samples.Shared.ViewModels;

global using Microsoft.Maui;
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Hosting;

// Microsoft.Maui.Platform is deliberately not imported globally: on iOS it also defines
// ContentView, which collides with Microsoft.Maui.Controls.ContentView across every view in
// the app. Platform types belong to the Platforms folders, which import them locally.

// WinUI only: used by the automation peers under Platforms/Windows, which are the
// Windows-specific workaround for MAUI layouts having no AutomationPeer there.
#if WINDOWS
global using Microsoft.UI.Xaml.Automation.Peers;
#endif
