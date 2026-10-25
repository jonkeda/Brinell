using Bruinell.Core.Exceptions;
using Bruinell.Core.Locators;
using MUnit.Interaction;
using MauiElement = Microsoft.Maui.Controls.Element;

namespace Bruinell.Maui.Munit;

/// <summary>
/// MUnit's refusals as Bruinell's: a hidden or disabled element is not ready, as on a device; any
/// other refusal is the app saying no, a <see cref="BruinellException"/> with MUnit's reason.
/// </summary>
internal static class MunitRefusals
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

    /// <summary>The Bruinell exception a device driver throws for the same refusal.</summary>
    public static BruinellException Translate(MauiElement element, ElementNotInteractableException refusal)
        => refusal.Reason switch
        {
            InteractionRefusal.Hidden => new ElementNotReadyException(LocatorOf(element), NotReadyReason.NotVisible, refusal.Message),
            InteractionRefusal.Disabled => new ElementNotReadyException(LocatorOf(element), NotReadyReason.Disabled, refusal.Message),
            _ => new BruinellException(refusal.Message),
        };

    private static Locator LocatorOf(MauiElement element)
        => string.IsNullOrEmpty(element.AutomationId)
            ? Locator.ByClassName(element.GetType().Name)
            : Locator.ByAutomationId(element.AutomationId);
}
