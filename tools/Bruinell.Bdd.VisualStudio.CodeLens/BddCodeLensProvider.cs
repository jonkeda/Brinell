using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json.Linq;

namespace Bruinell.Bdd.VisualStudio.CodeLens;

/// <summary>
/// "used in 12 BDD steps · 5 scenarios" above a step definition, a page phrase or a page object's
/// control. Nothing shows for a member no step uses.
/// </summary>
[Export(typeof(IAsyncCodeLensDataPointProvider))]
[Name(Id)]
[ContentType("CSharp")]
[LocalizedName(typeof(Strings), "ProviderName")]
[Priority(220)]
internal sealed class BddCodeLensProvider : IAsyncCodeLensDataPointProvider
{
    public const string Id = "BruinellUatUsages";

    [Import]
    internal Lazy<ICodeLensCallbackService> Callback { get; set; } = null!;

    public Task<bool> CanCreateDataPointAsync(CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken token)
        => Task.FromResult(descriptor.Kind is CodeElementKinds.Method or CodeElementKinds.Property);

    public Task<IAsyncCodeLensDataPoint> CreateDataPointAsync(CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken token)
        => Task.FromResult<IAsyncCodeLensDataPoint>(new BddCodeLensDataPoint(this, descriptor));

    // The steps that use a member, from the extension; null when none do or none can say.
    internal async Task<JObject?> UsagesAsync(CodeLensDescriptor descriptor, CancellationToken token)
    {
        var json = await Callback.Value.InvokeAsync<string>(this, "GetUatUsagesAsync", [descriptor.FilePath, descriptor.ElementDescription], token).ConfigureAwait(false);
        return JObject.Parse(json) is { } usages && (int?)usages["steps"] is > 0 ? usages : null;
    }
}

internal sealed class BddCodeLensDataPoint : IAsyncCodeLensDataPoint
{
    private readonly BddCodeLensProvider _provider;

    public BddCodeLensDataPoint(BddCodeLensProvider provider, CodeLensDescriptor descriptor)
    {
        _provider = provider;
        Descriptor = descriptor;
    }

    // A build or an edited feature file changes the counts; the lens is asked again when the file reopens.
#pragma warning disable CS0067
    public event AsyncEventHandler? InvalidatedAsync;
#pragma warning restore CS0067

    public CodeLensDescriptor Descriptor { get; }

    public async Task<CodeLensDataPointDescriptor?> GetDataAsync(CodeLensDescriptorContext descriptorContext, CancellationToken token)
    {
        if (await _provider.UsagesAsync(Descriptor, token).ConfigureAwait(false) is not { } usages)
        {
            return null;
        }

        var steps = (int)usages["steps"]!;
        var scenarios = (int?)usages["scenarios"] ?? 0;
        return new CodeLensDataPointDescriptor
        {
            Description = $"used in {steps} BDD step{(steps == 1 ? string.Empty : "s")} · {scenarios} scenario{(scenarios == 1 ? string.Empty : "s")}",
            TooltipText = "The Bruinell BDD steps that bind to this member",
            IntValue = steps,
            ImageId = null,
        };
    }

    public async Task<CodeLensDetailsDescriptor?> GetDetailsAsync(CodeLensDescriptorContext descriptorContext, CancellationToken token)
    {
        if (await _provider.UsagesAsync(Descriptor, token).ConfigureAwait(false) is not { } usages || usages["usages"] is not JArray list)
        {
            return null;
        }

        return new CodeLensDetailsDescriptor
        {
            Headers =
            [
                new CodeLensDetailHeaderDescriptor { UniqueName = "File", DisplayName = "File", Width = 0.25 },
                new CodeLensDetailHeaderDescriptor { UniqueName = "Line", DisplayName = "Line", Width = 0.07 },
                new CodeLensDetailHeaderDescriptor { UniqueName = "Step", DisplayName = "Step", Width = 0.43 },
                new CodeLensDetailHeaderDescriptor { UniqueName = "Scenario", DisplayName = "Scenario", Width = 0.25 },
            ],
            Entries = list.OfType<JObject>().Select(usage => new CodeLensDetailEntryDescriptor
            {
                Fields =
                [
                    new CodeLensDetailEntryField { Text = System.IO.Path.GetFileName(new Uri((string)usage["uri"]!).LocalPath) },
                    new CodeLensDetailEntryField { Text = (((int?)usage["line"] ?? 0) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    new CodeLensDetailEntryField { Text = (string?)usage["text"] ?? string.Empty },
                    new CodeLensDetailEntryField { Text = (string?)usage["scenario"] ?? string.Empty },
                ],
                Tooltip = new Uri((string)usage["uri"]!).LocalPath,
            }).ToList(),
        };
    }
}
