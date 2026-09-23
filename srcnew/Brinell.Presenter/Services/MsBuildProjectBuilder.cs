using System.Diagnostics;

namespace Brinell.Presenter.Services;

/// <summary>
/// Builds a project by running <c>dotnet build</c>, streaming its output as it arrives.
/// </summary>
/// <remarks>
/// A no-op incremental build of a small project takes several seconds, so this streams rather
/// than waits: a progress log with cancellation, not a spinner. Evaluation says where the output
/// goes; only a build says whether it can be produced.
/// </remarks>
public sealed class MsBuildProjectBuilder : IProjectBuilder
{
    /// <inheritdoc />
    public async Task<ProjectBuildResult> BuildAsync(
        string projectPath,
        string? targetFramework,
        string? configuration,
        Action<string>? onOutput,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        if (!File.Exists(projectPath))
        {
            return new ProjectBuildResult(false, $"Project was not found: {projectPath}");
        }

        List<string> arguments = ["build", projectPath, "-v:minimal", "--nologo", "/nr:false"];
        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            arguments.Add("-f");
            arguments.Add(targetFramework);
        }

        if (!string.IsNullOrWhiteSpace(configuration))
        {
            arguments.Add("-c");
            arguments.Add(configuration);
        }

        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        List<string> lines = [];
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        void Capture(object _, DataReceivedEventArgs args)
        {
            if (args.Data is null)
            {
                return;
            }

            lock (lines)
            {
                lines.Add(args.Data);
            }

            onOutput?.Invoke(args.Data);
        }

        process.OutputDataReceived += Capture;
        process.ErrorDataReceived += Capture;

        try
        {
            if (!process.Start())
            {
                return new ProjectBuildResult(false, "dotnet build did not start.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new ProjectBuildResult(false, Join(lines) + Environment.NewLine + "Build cancelled.");
        }
        catch (Exception ex)
        {
            return new ProjectBuildResult(false, $"Could not run dotnet build: {ex.Message}");
        }

        return new ProjectBuildResult(process.ExitCode == 0, Join(lines));
    }

    private static string Join(List<string> lines)
    {
        lock (lines)
        {
            return string.Join(Environment.NewLine, lines);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // It exited between the check and the kill; nothing to stop.
        }
    }
}
