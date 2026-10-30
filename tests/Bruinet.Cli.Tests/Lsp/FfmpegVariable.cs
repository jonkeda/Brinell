using Xunit;

namespace Bruinet.Bdd.LanguageServer.Tests;

/// <summary>The test classes that set <c>BRUINET_FFMPEG</c>: one at a time, since the process shares it.</summary>
[CollectionDefinition(Name)]
public sealed class FfmpegVariable
{
    /// <summary>The collection's name.</summary>
    public const string Name = "BRUINET_FFMPEG";
}
