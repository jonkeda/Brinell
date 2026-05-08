using Brinell.Scraper.Exceptions;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brinell.Scraper.Tests.Services;

public sealed class LlmRetryHelperTests
{
    private static LlmRetryHelper NewHelper() =>
        new(NullLogger<LlmRetryHelper>.Instance);

    [Fact]
    public async Task WithRetry_FirstAttemptOk_NoRetry()
    {
        var helper = NewHelper();
        var calls = 0;

        var result = await helper.WithRetryAsync<string>(
            call: (p, ct) =>
            {
                calls++;
                return Task.FromResult("payload");
            },
            initialPrompt: "go",
            validate: r => (true, r, null),
            maxRetries: 1);

        Assert.Equal("payload", result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task WithRetry_FailThenSuccess_RetriesOnceAndAppendsError()
    {
        var helper = NewHelper();
        var calls = 0;
        string? lastPrompt = null;

        var result = await helper.WithRetryAsync<string>(
            call: (p, ct) =>
            {
                calls++;
                lastPrompt = p;
                return Task.FromResult(calls == 1 ? "bad" : "good");
            },
            initialPrompt: "INITIAL",
            validate: r => r == "good"
                ? (true, r, null)
                : (false, r, "syntax error on line 1"),
            maxRetries: 1);

        Assert.Equal("good", result);
        Assert.Equal(2, calls);
        Assert.NotNull(lastPrompt);
        Assert.Contains("INITIAL", lastPrompt);
        Assert.Contains("Previous attempt had errors", lastPrompt);
        Assert.Contains("syntax error on line 1", lastPrompt);
    }

    [Fact]
    public async Task WithRetry_MaxRetriesExceeded_ThrowsLlmValidationException()
    {
        var helper = NewHelper();
        var calls = 0;

        var ex = await Assert.ThrowsAsync<LlmValidationException>(() =>
            helper.WithRetryAsync<string>(
                call: (p, ct) =>
                {
                    calls++;
                    return Task.FromResult("bad");
                },
                initialPrompt: "x",
                validate: r => (false, r, "always invalid"),
                maxRetries: 1));

        Assert.Equal(2, calls); // 1 initial + 1 retry
        Assert.Contains("Max retries exceeded", ex.Message);
        Assert.Contains("always invalid", ex.Message);
    }

    [Fact]
    public async Task WithRetry_HonorsCancellation()
    {
        var helper = NewHelper();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            helper.WithRetryAsync<string>(
                call: (p, ct) => Task.FromResult("x"),
                initialPrompt: "x",
                validate: r => (true, r, null),
                maxRetries: 1,
                ct: cts.Token));
    }

    [Fact]
    public async Task WithRetry_ZeroRetries_FailsImmediately()
    {
        var helper = NewHelper();
        var calls = 0;

        await Assert.ThrowsAsync<LlmValidationException>(() =>
            helper.WithRetryAsync<string>(
                call: (p, ct) => { calls++; return Task.FromResult("bad"); },
                initialPrompt: "x",
                validate: r => (false, r, "nope"),
                maxRetries: 0));

        Assert.Equal(1, calls);
    }
}
