using Brinell.Uat;

namespace Brinell.Presenter.Uat.Tests.Runtime;

internal sealed class PresenterUatRuntime
{
    private readonly UatReflectionRuntime _reflectionRuntime;

    public PresenterUatRuntime(PresenterFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _reflectionRuntime = UatReflectionRuntime.FromRoot(fixture);
    }

    public UatCommandCatalog CreateCommandCatalog()
    {
        return _reflectionRuntime.CreateCommandCatalog();
    }

    public string DiscoveryReport => string.Join(Environment.NewLine, _reflectionRuntime.DescribeDiscovery());
}
