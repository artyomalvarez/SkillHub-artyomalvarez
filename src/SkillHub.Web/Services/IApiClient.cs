namespace SkillHub.Web.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string uri);
    Task<TResponse?> PostAsync<TResponse>(string uri, object? data = null);
    Task<bool> PostAsync(string uri, object? data = null);
    Task<bool> PatchAsync(string uri, object? data = null);
    Task<TResponse?> PatchAsync<TResponse>(string uri, object? data = null);
    Task<byte[]> GetByteArrayAsync(string uri);
    Task<byte[]> GetBytesAsync(string uri);
}
