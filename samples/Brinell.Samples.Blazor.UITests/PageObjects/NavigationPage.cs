using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the Navigation page.
/// </summary>
public class NavigationPage : PageBase
{
    public override string AutomationId => "[data-automation-id='NavigationTitle']";

    // ═══════════════════════════════════════════════════════════════
    // HEADER
    // ═══════════════════════════════════════════════════════════════

    public LabelControl NavigationTitle { get; }
    public LabelControl ActionResult { get; }

    // ═══════════════════════════════════════════════════════════════
    // BREADCRUMB SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl BreadcrumbTitle { get; }
    public LinkControl BreadcrumbHome { get; }
    public LinkControl BreadcrumbProducts { get; }
    public LabelControl BreadcrumbCurrent { get; }

    // ═══════════════════════════════════════════════════════════════
    // ACCORDION SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl AccordionTitle { get; }
    public ButtonControl ExpandAllButton { get; }
    public ButtonControl CollapseAllButton { get; }

    // ═══════════════════════════════════════════════════════════════
    // TAB SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl TabSectionTitle { get; }

    // ═══════════════════════════════════════════════════════════════
    // TOOLBAR SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl ToolbarTitle { get; }
    public ButtonControl ToolbarNew { get; }
    public ButtonControl ToolbarOpen { get; }
    public ButtonControl ToolbarSave { get; }
    public ButtonControl ToolbarCut { get; }
    public ButtonControl ToolbarCopy { get; }
    public ButtonControl ToolbarPaste { get; }
    public ButtonControl ToolbarDelete { get; }

    // ═══════════════════════════════════════════════════════════════
    // DROPDOWN SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl DropdownTitle { get; }
    public ButtonControl DropdownButton { get; }

    // ═══════════════════════════════════════════════════════════════
    // LINKS SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl LinksTitle { get; }
    public LinkControl Link1 { get; }
    public LinkControl Link2 { get; }
    public LinkControl Link3 { get; }

    // ═══════════════════════════════════════════════════════════════
    // PAGINATION SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl PaginationTitle { get; }
    public ButtonControl PrevPage { get; }
    public ButtonControl NextPage { get; }
    public LabelControl PageInfo { get; }

    // ═══════════════════════════════════════════════════════════════
    // MODAL SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl ModalSectionTitle { get; }
    public ButtonControl ShowInfoModal { get; }
    public ButtonControl ShowConfirmModal { get; }
    public ButtonControl ShowAlertModal { get; }

    // Modal dialog elements
    public LabelControl ModalTitleText { get; }
    public LabelControl ModalBody { get; }
    public ButtonControl ModalClose { get; }
    public ButtonControl ModalCancelButton { get; }
    public ButtonControl ModalConfirmButton { get; }

    public NavigationPage(SeleniumTestContext context) : base(context)
    {
        NavigationTitle = new LabelControl(context, this, "[data-automation-id='NavigationTitle']");
        ActionResult = new LabelControl(context, this, "[data-automation-id='ActionResult']");

        // Breadcrumb
        BreadcrumbTitle = new LabelControl(context, this, "[data-automation-id='BreadcrumbTitle']");
        BreadcrumbHome = new LinkControl(context, this, "[data-automation-id='BreadcrumbHome']");
        BreadcrumbProducts = new LinkControl(context, this, "[data-automation-id='BreadcrumbProducts']");
        BreadcrumbCurrent = new LabelControl(context, this, "[data-automation-id='BreadcrumbCurrent']");

        // Accordion
        AccordionTitle = new LabelControl(context, this, "[data-automation-id='AccordionTitle']");
        ExpandAllButton = new ButtonControl(context, this, "[data-automation-id='ExpandAllButton']");
        CollapseAllButton = new ButtonControl(context, this, "[data-automation-id='CollapseAllButton']");

        // Tabs
        TabSectionTitle = new LabelControl(context, this, "[data-automation-id='TabSectionTitle']");

        // Toolbar
        ToolbarTitle = new LabelControl(context, this, "[data-automation-id='ToolbarTitle']");
        ToolbarNew = new ButtonControl(context, this, "[data-automation-id='ToolbarNew']");
        ToolbarOpen = new ButtonControl(context, this, "[data-automation-id='ToolbarOpen']");
        ToolbarSave = new ButtonControl(context, this, "[data-automation-id='ToolbarSave']");
        ToolbarCut = new ButtonControl(context, this, "[data-automation-id='ToolbarCut']");
        ToolbarCopy = new ButtonControl(context, this, "[data-automation-id='ToolbarCopy']");
        ToolbarPaste = new ButtonControl(context, this, "[data-automation-id='ToolbarPaste']");
        ToolbarDelete = new ButtonControl(context, this, "[data-automation-id='ToolbarDelete']");

        // Dropdown
        DropdownTitle = new LabelControl(context, this, "[data-automation-id='DropdownTitle']");
        DropdownButton = new ButtonControl(context, this, "[data-automation-id='DropdownButton1']");

        // Links
        LinksTitle = new LabelControl(context, this, "[data-automation-id='LinksTitle']");
        Link1 = new LinkControl(context, this, "[data-automation-id='Link1']");
        Link2 = new LinkControl(context, this, "[data-automation-id='Link2']");
        Link3 = new LinkControl(context, this, "[data-automation-id='Link3']");

        // Pagination
        PaginationTitle = new LabelControl(context, this, "[data-automation-id='PaginationTitle']");
        PrevPage = new ButtonControl(context, this, "[data-automation-id='PrevPage']");
        NextPage = new ButtonControl(context, this, "[data-automation-id='NextPage']");
        PageInfo = new LabelControl(context, this, "[data-automation-id='PageInfo']");

        // Modal
        ModalSectionTitle = new LabelControl(context, this, "[data-automation-id='ModalTitle']");
        ShowInfoModal = new ButtonControl(context, this, "[data-automation-id='ShowInfoModal']");
        ShowConfirmModal = new ButtonControl(context, this, "[data-automation-id='ShowConfirmModal']");
        ShowAlertModal = new ButtonControl(context, this, "[data-automation-id='ShowAlertModal']");

        // Modal dialog
        ModalTitleText = new LabelControl(context, this, "[data-automation-id='ModalTitleText']");
        ModalBody = new LabelControl(context, this, "[data-automation-id='ModalBody']");
        ModalClose = new ButtonControl(context, this, "[data-automation-id='ModalClose']");
        ModalCancelButton = new ButtonControl(context, this, "[data-automation-id='ModalCancelButton']");
        ModalConfirmButton = new ButtonControl(context, this, "[data-automation-id='ModalConfirmButton']");
    }

    public override bool IsDisplayed()
    {
        return NavigationTitle.IsVisible();
    }

    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════

    public NavigationPage ExpandAll()
    {
        Log("ExpandAll()");
        ExpandAllButton.Click();
        return this;
    }

    public NavigationPage CollapseAll()
    {
        Log("CollapseAll()");
        CollapseAllButton.Click();
        return this;
    }

    public NavigationPage ClickToolbarNew()
    {
        Log("ClickToolbarNew()");
        ToolbarNew.Click();
        return this;
    }

    public NavigationPage ClickToolbarSave()
    {
        Log("ClickToolbarSave()");
        ToolbarSave.Click();
        return this;
    }

    public NavigationPage GoToNextPage()
    {
        Log("GoToNextPage()");
        NextPage.Click();
        return this;
    }

    public NavigationPage GoToPrevPage()
    {
        Log("GoToPrevPage()");
        PrevPage.Click();
        return this;
    }

    public NavigationPage OpenInfoModal()
    {
        Log("OpenInfoModal()");
        ShowInfoModal.Click();
        return this;
    }

    public NavigationPage OpenConfirmModal()
    {
        Log("OpenConfirmModal()");
        ShowConfirmModal.Click();
        return this;
    }

    public NavigationPage CloseModal()
    {
        Log("CloseModal()");
        ModalClose.Click();
        return this;
    }

    public NavigationPage ConfirmModal()
    {
        Log("ConfirmModal()");
        ModalConfirmButton.Click();
        return this;
    }
}
