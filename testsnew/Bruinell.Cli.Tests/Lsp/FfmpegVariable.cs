using Xunit;

namespace Bruinell.Bdd.LanguageServer.Tests;

/// <summary>The test classes that set <c>BRUINELL_FFMPEG</c>: one at a time, since the process shares it.</summary>
[CollectionDefinition(Name)]
public sealed class FfmpegVariable
{
    /// <summary>The collection's name.</summary>
    public const string Name = "BRUINELL_FFMPEG";
}
