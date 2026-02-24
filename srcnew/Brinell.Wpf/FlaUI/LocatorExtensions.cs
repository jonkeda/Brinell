using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;

namespace Brinell.Wpf.FlaUI;

/// <summary>
/// Extension methods for converting Brinell Locator to FlaUI PropertyConditions.
/// </summary>
public static class LocatorExtensions
{
    /// <summary>
    /// Converts a Brinell Locator to a FlaUI PropertyCondition.
    /// </summary>
    public static ConditionBase ToCondition(this Locator locator, ConditionFactory conditionFactory)
    {
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(conditionFactory);

        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => conditionFactory.ByAutomationId(locator.Value).Or(conditionFactory.ByName(locator.Value)),
            LocatorStrategy.AccessibilityId => conditionFactory.ByAutomationId(locator.Value).Or(conditionFactory.ByName(locator.Value)),
            LocatorStrategy.Name => conditionFactory.ByName(locator.Value),
            LocatorStrategy.ClassName => conditionFactory.ByClassName(locator.Value),
            LocatorStrategy.ControlType => conditionFactory.ByControlType(ParseControlType(locator.Value)),
            LocatorStrategy.Text => conditionFactory.ByName(locator.Value),
            LocatorStrategy.Id => conditionFactory.ByAutomationId(locator.Value).Or(conditionFactory.ByName(locator.Value)),

            LocatorStrategy.XPath or
            LocatorStrategy.Css or
            LocatorStrategy.LinkText or
            LocatorStrategy.PartialLinkText or
            LocatorStrategy.TagName or
            LocatorStrategy.DataTestId or
            LocatorStrategy.DataAutomationId =>
                throw new LocatorNotSupportedException(locator.Strategy, "FlaUI"),

            _ => throw new LocatorNotSupportedException(locator.Strategy, "FlaUI")
        };
    }

    private static ControlType ParseControlType(string value)
    {
        if (Enum.TryParse<ControlType>(value, ignoreCase: true, out var controlType))
        {
            return controlType;
        }

        return value.ToLowerInvariant() switch
        {
            "button" => ControlType.Button,
            "text" or "textbox" or "entry" => ControlType.Edit,
            "label" or "textblock" => ControlType.Text,
            "checkbox" or "check" => ControlType.CheckBox,
            "radio" or "radiobutton" => ControlType.RadioButton,
            "combobox" or "combo" or "picker" => ControlType.ComboBox,
            "list" or "listview" or "collectionview" => ControlType.List,
            "listitem" => ControlType.ListItem,
            "tree" or "treeview" => ControlType.Tree,
            "treeitem" => ControlType.TreeItem,
            "menu" => ControlType.Menu,
            "menuitem" => ControlType.MenuItem,
            "tab" or "tabcontrol" => ControlType.Tab,
            "tabitem" => ControlType.TabItem,
            "slider" => ControlType.Slider,
            "progressbar" or "progress" => ControlType.ProgressBar,
            "scrollbar" => ControlType.ScrollBar,
            "image" => ControlType.Image,
            "window" => ControlType.Window,
            "pane" or "frame" or "border" => ControlType.Pane,
            "group" => ControlType.Group,
            "document" => ControlType.Document,
            "hyperlink" or "link" => ControlType.Hyperlink,
            "tooltip" => ControlType.ToolTip,
            "toolbar" => ControlType.ToolBar,
            "statusbar" => ControlType.StatusBar,
            "table" or "grid" or "datagrid" => ControlType.Table,
            "header" => ControlType.Header,
            "headeritem" => ControlType.HeaderItem,
            "spinner" => ControlType.Spinner,
            "calendar" => ControlType.Calendar,
            "datepicker" => ControlType.Custom,
            "timepicker" => ControlType.Custom,
            _ => ControlType.Custom
        };
    }
}
