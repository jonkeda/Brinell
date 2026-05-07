using Xunit;

namespace Brinell.Scraper.Tests.Services;

using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

public sealed class RetryServiceTests
{
    private const string ValidCode = """
        namespace Test;

        public sealed class LoginPage
        {
            public string UserName { get; }
        }
        """;

    private const string InvalidCode = """
        namespace Test;

        public sealed class LoginPage
        {
            public string UserName { get; }
        """; // Missing closing brace

    private const string CorrectedCodeFenced = $"""
        ```csharp
        {ValidCode}
        ```
        """;

    [Fact]
    public async Task ValidateWithRetry_ValidCode_NoRetry()
    {
        var copilotService = Substitute.For<ICopilotService>();
        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns([]);

        var service = new RetryService(copilotService, NullLogger<RetryService>.Instance);

        var (code, validation) = await service.ValidateWithRetryAsync(ValidCode, registry);

        Assert.True(validation.IsValid);
        Assert.Equal(ValidCode, code);
        await copilotService.DidNotReceive()
            .GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateWithRetry_FirstAttemptFails_SecondSucceeds()
    {
        var copilotService = Substitute.For<ICopilotService>();
        copilotService.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns($"```csharp\n{ValidCode}\n```");

        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns([]);

        var service = new RetryService(copilotService, NullLogger<RetryService>.Instance);

        var (code, validation) = await service.ValidateWithRetryAsync(InvalidCode, registry);

        Assert.True(validation.IsValid);
        await copilotService.Received(1)
            .GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateWithRetry_AllAttemptsFail_ReturnsErrors()
    {
        var copilotService = Substitute.For<ICopilotService>();
        copilotService.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns($"```csharp\n{InvalidCode}\n```");

        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns([]);

        var service = new RetryService(copilotService, NullLogger<RetryService>.Instance);

        var (code, validation) = await service.ValidateWithRetryAsync(InvalidCode, registry);

        Assert.False(validation.IsValid);
        await copilotService.Received(2)
            .GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
