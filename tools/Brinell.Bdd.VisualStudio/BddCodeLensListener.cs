using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json;

namespace Brinell.Bdd.VisualStudio;

/// <summary>
/// The Visual Studio side of the BDD CodeLens: the CodeLens process calls it by name for a C# member,
/// and it asks the language server which steps use it. A solution without a UAT suite never starts
/// the server for it.
/// </summary>
[Export(typeof(ICodeLensCallbackListener))]
[ContentType("CSharp")]
internal sealed class BddCodeLensListener : ICodeLensCallbackListener
{
    private readonly Dictionary<string, bool> _hasSuites = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The steps that use a C# member, as the server's <c>brinell/usages</c> JSON; <c>{}</c> when none can.</summary>
    /// <param name="filePath">The C# file.</param>
    /// <param name="symbol">The member, as CodeLens names it.</param>
    public async Task<string> GetUatUsagesAsync(string filePath, string symbol)
    {
        if (!HasSuites(filePath))
        {
            return "{}";
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (BrinellBddPackage.Instance is null &&
            await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsShell)) is IVsShell shell)
        {
            var package = new Guid(PackageIds.Package);
            shell.LoadPackage(ref package, out _);
        }

        if (BrinellBddPackage.Instance?.Controller is not { } controller)
        {
            return "{}";
        }

        try
        {
            return (await controller.UsagesAsync(filePath, symbol)).ToString(Formatting.None);
        }
        catch (Exception exception) when (exception is StreamJsonRpc.RemoteInvocationException or StreamJsonRpc.ConnectionLostException or IOException)
        {
            return "{}";
        }
    }

    // Whether the C# file's solution folder has a suite config anywhere below it; asked once per folder.
    private bool HasSuites(string filePath)
    {
        var folder = SolutionFolderOf(filePath);
        lock (_hasSuites)
        {
            if (!_hasSuites.TryGetValue(folder, out var has))
            {
                has = Directory.EnumerateFiles(folder, "uat.config.md", SearchOption.AllDirectories).Any() ||
                      Directory.EnumerateFiles(folder, "bdd.config.md", SearchOption.AllDirectories).Any();
                _hasSuites[folder] = has;
            }

            return has;
        }
    }

    private static string SolutionFolderOf(string filePath)
    {
        var start = Path.GetDirectoryName(filePath)!;
        for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
        {
            if (folder.EnumerateFiles("*.sln").Any() || folder.EnumerateFiles("*.slnx").Any())
            {
                return folder.FullName;
            }
        }

        return start;
    }
}
