using Brinell.Core.Configuration;
using Brinell.Core.Exceptions;
using Brinell.Core.Logging;

namespace Brinell.Blazor.Tests.Mocks;

public static class MockHtmlFactory
{
    public static Mock<IHtmlTestContext> CreateMockContext()
    {
        var mock = new Mock<IHtmlTestContext>();
        mock.Setup(c => c.Timeouts).Returns(new TimeoutSettings());
        mock.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.Css);
        mock.Setup(c => c.Context).Returns(() => mock.Object);
        mock.Setup(c => c.Logger).Returns(Mock.Of<ITestLogger>());
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
        mock.Setup(e => e.InputValue).Returns(text ?? "");
        return mock;
    }

    public static Mock<IHtmlElement> CreateMockToggleElement(
        bool isChecked = false,
        string? text = null,
        bool visible = true,
        bool enabled = true)
    {
        var mock = CreateMockElement(text, visible, enabled);
        mock.Setup(e => e.IsChecked).Returns(isChecked);
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
