using System.Reflection;
using Brinell.Core.Interfaces;
using Brinell.Core.Recording;

namespace Brinell.Uat;

/// <summary>What a UAT fixture's app shows, for a recording.</summary>
public static class UatRecording
{
    /// <summary>
    /// The fixture's target, when it is <see cref="IRecordable"/>; else that of its first public
    /// test context property that is (the fixture's <c>Context</c>); null when none says.
    /// </summary>
    /// <param name="fixture">The UAT fixture.</param>
    public static RecordingTarget? TargetOf(object fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        if (fixture is IRecordable recordable)
        {
            return recordable.RecordingTarget;
        }

        return fixture.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0 && typeof(ITestContext).IsAssignableFrom(property.PropertyType))
            .Select(property => property.GetValue(fixture))
            .OfType<IRecordable>()
            .Select(context => context.RecordingTarget)
            .FirstOrDefault(target => target is not null);
    }
}
