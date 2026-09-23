using Brinell.Maui.Controls.Container;
using Brinell.Maui.Interfaces;
using Brinell.Presenter.ViewModels;

namespace Brinell.Presenter.Uat.Tests.PageObjects;

/// <summary>
/// The Presenter's workspace tree: nodes addressed by their kind and name, the way the app builds
/// their automation ids.
/// </summary>
/// <remarks>
/// A node's ids come from the same key, so kind and name give both the row label
/// (<c>WorkspaceNode_*</c>) and its toggle (<c>WorkspaceNodeToggle_*</c>) - see
/// <c>UatWorkspaceNodeViewModel</c>. Two nodes of one kind with the same name in different files
/// share that key; no workspace under test has such a pair.
/// </remarks>
public sealed class PresenterWorkspaceTree : TreeView<PresenterPage, PresenterWorkspaceTree>
{
    /// <summary>Creates the workspace tree within the Presenter page.</summary>
    public PresenterWorkspaceTree(IMauiScope<PresenterPage> scope)
        : base(scope, "WorkspaceTree")
    {
    }

    /// <summary>The node of the given kind and name.</summary>
    public TreeNode<PresenterWorkspaceTree> Node(UatWorkspaceNodeKind kind, string name)
    {
        var key = SanitizeAutomationId($"{kind}_{name}");
        return Node($"WorkspaceNode_{key}", $"WorkspaceNodeToggle_{key}");
    }

    private static string SanitizeAutomationId(string value)
    {
        return string.Concat(value.Select(c => char.IsLetterOrDigit(c) ? c : '_')).Trim('_');
    }
}
