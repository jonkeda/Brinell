using ObjCRuntime;
using UIKit;

namespace Brinell.Samples.Maui.App;

/// <summary>
/// iOS entry point.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point of the application.
    /// </summary>
    static void Main(string[] args)
    {
        // Hands control to the Xamarin.iOS runtime, which instantiates AppDelegate.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
