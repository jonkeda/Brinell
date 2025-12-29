using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Brinell.Mocking;

/// <summary>
/// Fluent builder for API stubs.
/// </summary>
public class ApiStubBuilder
{
    private string _path = "/";
    private string _method = "GET";
    private readonly Dictionary<string, string> _queryParams = new();
    private readonly Dictionary<string, string> _requestHeaders = new();
    private string? _requestBody;
    
    private int _statusCode = 200;
    private string? _responseBody;
    private string _contentType = "application/json";
    private readonly Dictionary<string, string> _responseHeaders = new();
    private int _delayMs;

    #region Request Configuration

    /// <summary>
    /// Set the path to match.
    /// </summary>
    public ApiStubBuilder WithPath(string path)
    {
        _path = path;
        return this;
    }

    /// <summary>
    /// Set the HTTP method to match.
    /// </summary>
    public ApiStubBuilder WithMethod(string method)
    {
        _method = method.ToUpperInvariant();
        return this;
    }

    /// <summary>
    /// Add a query parameter to match.
    /// </summary>
    public ApiStubBuilder WithQueryParam(string name, string value)
    {
        _queryParams[name] = value;
        return this;
    }

    /// <summary>
    /// Add a request header to match.
    /// </summary>
    public ApiStubBuilder WithHeader(string name, string value)
    {
        _requestHeaders[name] = value;
        return this;
    }

    /// <summary>
    /// Set the request body to match (exact).
    /// </summary>
    public ApiStubBuilder WithBody(string body)
    {
        _requestBody = body;
        return this;
    }

    /// <summary>
    /// Set the request body to match (JSON serialized).
    /// </summary>
    public ApiStubBuilder WithJsonBody(object body)
    {
        _requestBody = JsonSerializer.Serialize(body);
        return this;
    }

    #endregion

    #region Response Configuration

    /// <summary>
    /// Set the response status code.
    /// </summary>
    public ApiStubBuilder ReturnsStatus(int statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    /// <summary>
    /// Set the response body as JSON.
    /// </summary>
    public ApiStubBuilder ReturnsJson(object body, int statusCode = 200)
    {
        _statusCode = statusCode;
        _responseBody = JsonSerializer.Serialize(body);
        _contentType = "application/json";
        return this;
    }

    /// <summary>
    /// Set the response body as text.
    /// </summary>
    public ApiStubBuilder ReturnsText(string body, int statusCode = 200)
    {
        _statusCode = statusCode;
        _responseBody = body;
        _contentType = "text/plain";
        return this;
    }

    /// <summary>
    /// Set the response body directly.
    /// </summary>
    public ApiStubBuilder ReturnsBody(string body, string contentType = "application/json", int statusCode = 200)
    {
        _statusCode = statusCode;
        _responseBody = body;
        _contentType = contentType;
        return this;
    }

    /// <summary>
    /// Add a response header.
    /// </summary>
    public ApiStubBuilder WithResponseHeader(string name, string value)
    {
        _responseHeaders[name] = value;
        return this;
    }

    /// <summary>
    /// Add a delay before responding.
    /// </summary>
    public ApiStubBuilder WithDelay(int milliseconds)
    {
        _delayMs = milliseconds;
        return this;
    }

    /// <summary>
    /// Return an error response.
    /// </summary>
    public ApiStubBuilder ReturnsError(int statusCode = 500, string? message = null)
    {
        _statusCode = statusCode;
        _responseBody = message != null 
            ? JsonSerializer.Serialize(new { error = message }) 
            : null;
        _contentType = "application/json";
        return this;
    }

    #endregion

    /// <summary>
    /// Build the WireMock request and response.
    /// </summary>
    internal (IRequestBuilder Request, IResponseBuilder Response) Build()
    {
        var request = Request.Create()
            .WithPath(_path)
            .UsingMethod(_method);

        foreach (var param in _queryParams)
        {
            request.WithParam(param.Key, param.Value);
        }

        foreach (var header in _requestHeaders)
        {
            request.WithHeader(header.Key, header.Value);
        }

        if (_requestBody != null)
        {
            request.WithBody(_requestBody);
        }

        var response = Response.Create()
            .WithStatusCode(_statusCode);

        if (_responseBody != null)
        {
            response.WithBody(_responseBody);
        }

        response.WithHeader("Content-Type", _contentType);

        foreach (var header in _responseHeaders)
        {
            response.WithHeader(header.Key, header.Value);
        }

        if (_delayMs > 0)
        {
            response.WithDelay(_delayMs);
        }

        return (request, response);
    }
}
