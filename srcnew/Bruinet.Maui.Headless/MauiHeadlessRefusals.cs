using Bruinet.Core.Exceptions;
using Bruinet.Core.Locators;
using MauiHeadless.Interaction;
using MauiElement = Microsoft.Maui.Controls.Element;

namespace Bruinet.Maui.Headless;

/// <summary>
/// MauiHeadless's refusals as Bruinet's: a hidden or disabled element is not ready, as on a device; any
/// other refusal is the app saying no, a <see cref="BruinetException"/> with MauiHeadless's reason.
/// </summary>
internal static class MauiHeadlessRefusals
{
    /// <summary>Runs <paramref name="action"/> on <paramref name="element"/>, translating a refusal.</summary>
    public static void Run(MauiElement element, Action action)
    {
        try
        {
            action();
        }
        catch (ElementNotInteractableException refusal)
        {
            throw Translate(element, refusal);
        }
    }

    /// <summary>The Bruinet exception a device driver throws for the same refusal.</summary>
    public static BruinetException Translate(MauiElement element, ElementNotInteractableException refusal)
        => refusal.Reason switch
        {
            InteractionRefusal.Hidden => new ElementNotReadyException(LocatorOf(element), NotReadyReason.NotVisible, refusal.Message),
            InteractionRefusal.Disabled => new ElementNotReadyException(LocatorOf(element), NotReadyReason.Disabled, refusal.Message),
            _ => new BruinetException(refusal.Message),
        };

    private static Locator LocatorOf(MauiElement element)
        => string.IsNullOrEmpty(element.AutomationId)
            ? Locator.ByClassName(element.GetType().Name)
            : Locator.ByAutomationId(element.AutomationId);
}
