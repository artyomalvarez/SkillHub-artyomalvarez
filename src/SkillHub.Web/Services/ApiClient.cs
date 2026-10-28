using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SkillHub.Web.Services;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class ApiClient : IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _baseUrl;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5250";
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("SkillHubApiClient");
        client.BaseAddress = new Uri(_baseUrl);
        return client;
    }

    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var token = session?.GetString("JwtToken");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static async Task HandleErrorResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync();
        var message = errorBody;

        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            if (doc.RootElement.TryGetProperty("error", out var errorProp))
            {
                message = errorProp.GetString() ?? errorBody;
            }
            else if (doc.RootElement.TryGetProperty("message", out var msgProp))
            {
                message = msgProp.GetString() ?? errorBody;
            }
            else if (doc.RootElement.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
            {
                var validationErrors = new List<string>();
                foreach (var prop in errorsProp.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var err in prop.Value.EnumerateArray())
                        {
                            validationErrors.Add(err.GetString() ?? string.Empty);
                        }
                    }
                    else
                    {
                        validationErrors.Add(prop.Value.GetString() ?? string.Empty);
                    }
                }
                if (validationErrors.Count > 0)
                {
                    message = string.Join("; ", validationErrors.Where(s => !string.IsNullOrWhiteSpace(s)));
                }
            }
            else if (doc.RootElement.TryGetProperty("title", out var titleProp))
            {
                message = titleProp.GetString() ?? errorBody;
            }
        }
        catch
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = $"Error HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            }
        }

        throw new ApiException(message, response.StatusCode);
    }

    public async Task<T?> GetAsync<T>(string uri)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        AddAuthorizationHeader(request);

        using var response = await client.SendAsync(request);
        await HandleErrorResponseAsync(response);

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    public async Task<TResponse?> PostAsync<TResponse>(string uri, object? data = null)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        
        var token = _httpContextAccessor.HttpContext?.Session?.GetString("JwtToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            AddAuthorizationHeader(request);
        }

        if (data is not null)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);
        await HandleErrorResponseAsync(response);

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
    }

    public async Task<bool> PostAsync(string uri, object? data = null)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        
        var token = _httpContextAccessor.HttpContext?.Session?.GetString("JwtToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            AddAuthorizationHeader(request);
        }

        if (data is not null)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);
        await HandleErrorResponseAsync(response);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PatchAsync(string uri, object? data = null)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, uri);
        AddAuthorizationHeader(request);

        if (data is not null)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);
        await HandleErrorResponseAsync(response);

        return response.IsSuccessStatusCode;
    }

    public async Task<TResponse?> PatchAsync<TResponse>(string uri, object? data = null)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, uri);
        AddAuthorizationHeader(request);

        if (data is not null)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);
        await HandleErrorResponseAsync(response);

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }

        return JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
    }

    public async Task<byte[]> GetByteArrayAsync(string uri)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        AddAuthorizationHeader(request);

        using var response = await client.SendAsync(request);
        await HandleErrorResponseAsync(response);

        return await response.Content.ReadAsByteArrayAsync();
    }

    public Task<byte[]> GetBytesAsync(string uri)
    {
        return GetByteArrayAsync(uri);
    }
}
