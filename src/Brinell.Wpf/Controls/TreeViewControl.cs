using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF TreeView control wrapper.
/// Uses WPF-specific ItemsControlBase for FlaUI integration.
/// </summary>
public class TreeViewControl : ItemsControlBase, IItemsControl
{
    public TreeViewControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TreeViewControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get item elements from the TreeView (root level only).
    /// </summary>
    protected override AutomationElement[] GetItemElements()
    {
        var element = FindElement();
        if (element != null)
        {
            var tree = element.AsTree();
            return tree?.Items.Cast<AutomationElement>().ToArray() ?? Array.Empty<AutomationElement>();
        }
        return Array.Empty<AutomationElement>();
    }

    /// <summary>
    /// Get selected node text (immediate, no wait).
    /// </summary>
    public string? GetSelectedText()
    {
        var element = FindElement();
        if (element != null)
        {
            var tree = element.AsTree();
            return tree?.SelectedTreeItem?.Text;
        }
        return null;
    }

    /// <summary>
    /// Get selected text as display text.
    /// </summary>
    public override string GetText()
    {
        return GetSelectedText() ?? string.Empty;
    }

    /// <summary>
    /// Select a node by path (e.g., "Root/Child/GrandChild").
    /// </summary>
    public void SelectNode(string path)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var tree = element.AsTree();
            if (tree == null) return;

            var parts = path.Split('/');
            TreeItem? current = null;

            foreach (var part in parts)
            {
                var items = current == null ? tree.Items : current.Items;
                current = items.FirstOrDefault(i => i.Text == part);
                
                if (current == null)
                {
                    LogDebug($"Node not found: {part} in path {path}");
                    return;
                }
                
                current.Expand();
            }

            current?.Select();
        }
        LogAction("SelectNode", path);
    }

    /// <summary>
    /// Expand a node by path.
    /// </summary>
    public void ExpandNode(string path)
    {
        CheckVisible();
        var node = FindNode(path);
        node?.Expand();
        LogAction("ExpandNode", path);
    }

    /// <summary>
    /// Collapse a node by path.
    /// </summary>
    public void CollapseNode(string path)
    {
        CheckVisible();
        var node = FindNode(path);
        node?.Collapse();
        LogAction("CollapseNode", path);
    }

    /// <summary>
    /// Get selected node text.
    /// </summary>
    public string? GetSelectedNodeText() => GetSelectedText();

    /// <summary>
    /// Get count of root-level nodes (alias for GetItemCount).
    /// </summary>
    public int GetNodeCount() => GetItemCount();

    /// <summary>
    /// Get all root-level node texts.
    /// </summary>
    public string[] GetRootNodeTexts()
    {
        var element = FindElement();
        if (element != null)
        {
            var tree = element.AsTree();
            return tree?.Items.Select(i => i.Text).ToArray() ?? Array.Empty<string>();
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Check if a node exists by path.
    /// </summary>
    public bool NodeExists(string path)
    {
        return FindNode(path) != null;
    }

    /// <summary>
    /// Check if a node is expanded.
    /// </summary>
    public bool IsNodeExpanded(string path)
    {
        var node = FindNode(path);
        if (node == null) return false;
        
        try
        {
            return node.ExpandCollapseState == ExpandCollapseState.Expanded;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Wait for a specific node to appear.
    /// </summary>
    public bool WaitForNode(string path, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(
            () => NodeExists(path),
            timeoutMs,
            $"node '{path}' exists");
        LogWait($"Node={path}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Find a tree node by path.
    /// </summary>
    private TreeItem? FindNode(string path)
    {
        var element = FindElement();
        if (element == null) return null;

        var tree = element.AsTree();
        if (tree == null) return null;

        var parts = path.Split('/');
        TreeItem? current = null;

        foreach (var part in parts)
        {
            var items = current == null ? tree.Items : current.Items;
            current = items.FirstOrDefault(i => i.Text == part);
            
            if (current == null) return null;
            
            if (parts.Last() != part)
            {
                current.Expand();
            }
        }

        return current;
    }
}
