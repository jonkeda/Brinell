using Bruinell.Core.Configuration;
using Bruinell.Core.Exceptions;
using Bruinell.Core.Logging;

namespace Bruinell.Blazor.Tests.Mocks;

public static class MockHtmlFactory
{
    public static Mock<IHtmlTestContext> CreateMockContext()
    {
        var mock = new Mock<IHtmlTestContext>();
        mock.Setup(c => c.Timeouts).Returns(new TimeoutSettings());
        mock.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.Css);
        mock.Setup(c => c.Context).Returns(() => mock.Object);
        mock.Setup(c => c.Logger).Returns(Mock.Of<ITestLogger>());
        mock.Setup(c => c.IsIdle()).Returns(true);
        return mock;
    }

    public static Mock<IHtmlElement> CreateMockElement(
        string? text = "Test Text",
        bool visible = true,
        bool enabled = true)
    {
        var mock = new Mock<IHtmlElement>();
        mock.Setup(e => e.Text).Returns(text);
        mock.Setup(e => e.Visible).Returns(visible);
        mock.Setup(e => e.Enabled).Returns(enabled);
        // What a live Blazor app shows once it is interactive; Blazor pages wait for it (html plan, step 5).
        mock.Setup(e => e.GetAttribute("data-busy")).Returns("false");

        // Actions have their effect, as on a real element: controls confirm them (html plan, step 6),
        // and a mock that ignored them would be an app bug the call must report.
        var value = text ?? "";
        var focused = false;
        mock.Setup(e => e.InputValue).Returns(() => value);
        mock.Setup(e => e.Fill(It.IsAny<string>())).Callback<string>(filled => value = filled);
        mock.Setup(e => e.SendKeys(It.IsAny<string>(), It.IsAny<Bruinell.Core.TextInputMethod>()))
            .Callback<string, Bruinell.Core.TextInputMethod>((typed, _) => value += typed);
        mock.Setup(e => e.SelectOption(It.IsAny<string>())).Callback<string>(selected => value = selected);
        mock.Setup(e => e.Clear()).Callback(() => value = "");
        mock.Setup(e => e.Focus()).Callback(() => focused = true);
        mock.Setup(e => e.Blur()).Callback(() => focused = false);
        mock.Setup(e => e.Evaluate<bool>("el => document.activeElement === el")).Returns(() => focused);
        return mock;
    }

    /// <summary>
    /// An &lt;audio&gt; or &lt;video&gt; whose DOM properties follow the scripts run on it: play,
    /// pause, and assignments such as <c>el =&gt; el.volume = 0.5</c>.
    /// </summary>
    public static Mock<IHtmlElement> CreateMockMediaElement()
    {
        var mock = CreateMockElement();
        var properties = new Dictionary<string, string?>
        {
            ["paused"] = "true",
            ["ended"] = "false",
            ["muted"] = "false",
            ["volume"] = "1",
            ["currentTime"] = "0",
            ["duration"] = "0",
        };
        mock.Setup(e => e.GetDomProperty(It.IsAny<string>())).Returns<string>(name => properties.GetValueOrDefault(name));
        mock.Setup(e => e.Evaluate(It.IsAny<string>())).Callback<string>(script =>
        {
            if (script == "el => el.play()")
            {
                properties["paused"] = "false";
            }
            else if (script == "el => el.pause()")
            {
                properties["paused"] = "true";
            }
            else if (System.Text.RegularExpressions.Regex.Match(script, @"^el => el\.(\w+) = (.+)$") is { Success: true } assignment)
            {
                properties[assignment.Groups[1].Value] = assignment.Groups[2].Value;
            }
        });
        return mock;
    }

    public static Mock<IHtmlElement> CreateMockToggleElement(
        bool isChecked = false,
        string? text = null,
        bool visible = true,
        bool enabled = true)
    {
        var mock = CreateMockElement(text, visible, enabled);
        var state = isChecked;
        mock.Setup(e => e.IsChecked).Returns(() => state);
        mock.Setup(e => e.Check()).Callback(() => state = true);
        mock.Setup(e => e.Uncheck()).Callback(() => state = false);
        return mock;
    }

    public static void SetupFindElement(Mock<IHtmlTestContext> context, Mock<IHtmlElement> element)
    {
        context.Setup(c => c.FindElement(It.IsAny<Locator>())).Returns(element.Object);
        context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns(element.Object);
    }

    public static void SetupFindElements(Mock<IHtmlTestContext> context, params Mock<IHtmlElement>[] elements)
    {
        var list = elements.Select(e => e.Object).ToList().AsReadOnly();
        context.Setup(c => c.FindElements(It.IsAny<Locator>())).Returns(list);
    }

    public static void SetupElementNotFound(Mock<IHtmlTestContext> context)
    {
        context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns((IHtmlElement?)null);
        context.Setup(c => c.FindElement(It.IsAny<Locator>()))
            .Throws(new ElementNotFoundException("Element not found"));
    }

    public static void SetupDomProperty(Mock<IHtmlElement> element, string property, string? value)
    {
        element.Setup(e => e.GetDomProperty(property)).Returns(value);
    }

    public static void SetupDomAttribute(Mock<IHtmlElement> element, string attribute, string? value)
    {
        element.Setup(e => e.GetDomAttribute(attribute)).Returns(value);
    }

    public static void SetupEvaluate<T>(Mock<IHtmlElement> element, string expression, T returnValue)
    {
        element.Setup(e => e.Evaluate<T>(It.Is<string>(s => s == expression))).Returns(returnValue);
    }
}
