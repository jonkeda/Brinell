using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Sync;

namespace Brinell.Samples.Todo.Infrastructure.Api;

/// <summary>
/// <see cref="ITodoApi"/> over HTTP.
/// </summary>
/// <remarks>
/// Turns every failure into a <see cref="TodoApiException"/> naming what went wrong, so the sync
/// and the list page can tell "the server is down" from "the network is" from "the key is wrong".
/// </remarks>
public sealed class TodoApiClient(HttpClient http) : ITodoApi
{
    /// <summary>
    /// Creates the <see cref="HttpClient"/> the client expects: base address, key header, timeout.
    /// </summary>
    public static HttpClient CreateHttpClient(string baseUrl, string apiKey, TimeSpan timeout, HttpMessageHandler? handler = null)
    {
        var client = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.Timeout = timeout;
        client.DefaultRequestHeaders.Add(TodoApi.ApiKeyHeader, apiKey);
        return client;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
        => SendAsync(async () =>
        {
            using var response = await http.GetAsync(Relative(TodoApi.TodosPath), cancellationToken).ConfigureAwait(false);
            await ThrowIfFailedAsync(response).ConfigureAwait(false);

            var dtos = await response.Content
                .ReadFromJsonAsync<List<TodoDto>>(TodoApi.JsonOptions, cancellationToken).ConfigureAwait(false);

            return (IReadOnlyList<TodoItem>)(dtos ?? []).Select(TodoMapping.ToItem).ToList();
        }, cancellationToken);

    /// <inheritdoc />
    public Task<TodoItem> PutAsync(TodoItem item, CancellationToken cancellationToken = default)
        => SendAsync(async () =>
        {
            using var response = await http
                .PutAsJsonAsync(Relative(TodoApi.TodoPath(item.Id)), TodoMapping.ToDto(item), TodoApi.JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            await ThrowIfFailedAsync(response).ConfigureAwait(false);

            var stored = await response.Content
                .ReadFromJsonAsync<TodoDto>(TodoApi.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new TodoApiException(SyncError.ServerError, $"PUT {TodoApi.TodoPath(item.Id)} answered without a body.");

            return TodoMapping.ToItem(stored);
        }, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => SendAsync(async () =>
        {
            using var response = await http.DeleteAsync(Relative(TodoApi.TodoPath(id)), cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                await ThrowIfFailedAsync(response).ConfigureAwait(false);
            }

            return true;
        }, cancellationToken);

    private static string Relative(string path) => path.TrimStart('/');

    private static async Task<T> SendAsync<T>(Func<Task<T>> call, CancellationToken cancellationToken)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (TodoApiException)
        {
            throw;
        }
        catch (TaskCanceledException timeout) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TodoApiException(SyncError.Timeout, "The server did not answer in time.", timeout);
        }
        catch (HttpRequestException unreachable)
        {
            throw new TodoApiException(SyncError.Unreachable, $"The server could not be reached: {unreachable.Message}", unreachable);
        }
        catch (JsonException malformed)
        {
            throw new TodoApiException(SyncError.ServerError, $"The server's answer could not be read: {malformed.Message}", malformed);
        }
    }

    private static async Task ThrowIfFailedAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var request = $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.AbsolutePath}";
        var error = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            ? SyncError.Unauthorized
            : SyncError.ServerError;

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new TodoApiException(error, $"{request} answered {(int)response.StatusCode} {response.StatusCode}. {body}".Trim());
    }
}

/// <summary>Maps between the wire format and the app's model.</summary>
public static class TodoMapping
{
    /// <summary>A server todo as the app stores it: synced, not deleted.</summary>
    public static TodoItem ToItem(TodoDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Notes = dto.Notes,
        DueDate = dto.DueDate,
        Status = dto.Status switch
        {
            TodoStatusDto.Open => TodoStatus.Open,
            TodoStatusDto.InProgress => TodoStatus.InProgress,
            TodoStatusDto.Done => TodoStatus.Done,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto.Status, null),
        },
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        SyncState = SyncState.Synced,
    };

    /// <summary>An app todo on the wire. Sync state and tombstones stay local.</summary>
    public static TodoDto ToDto(TodoItem item) => new(
        item.Id,
        item.Title,
        item.Notes,
        item.DueDate,
        item.Status switch
        {
            TodoStatus.Open => TodoStatusDto.Open,
            TodoStatus.InProgress => TodoStatusDto.InProgress,
            TodoStatus.Done => TodoStatusDto.Done,
            _ => throw new ArgumentOutOfRangeException(nameof(item), item.Status, null),
        },
        item.CreatedAt,
        item.UpdatedAt);
}
