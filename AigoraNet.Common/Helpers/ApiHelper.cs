using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace AigoraNet.Common.Helpers;

public class ApiHelper
{
    private readonly HttpClient _httpClient;

    public ApiHelper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// GET 요청
    /// </summary>
    public async Task<T?> GetAsync<T>(string url, string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// POST 요청
    /// </summary>
    public async Task<T?> PostAsync<T>(string url, object data, string accessToken, CancellationToken cancellationToken = default)
    {
        var jsonData = JsonConvert.SerializeObject(data);
        using var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// PUT 요청
    /// </summary>
    public async Task<T?> PutAsync<T>(string url, object data, string accessToken, CancellationToken cancellationToken = default)
    {
        var jsonData = JsonConvert.SerializeObject(data);
        using var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// DELETE 요청
    /// </summary>
    public async Task<bool> DeleteAsync(string url, string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}
