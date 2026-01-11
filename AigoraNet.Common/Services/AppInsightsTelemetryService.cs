using AigoraNet.Common.Abstracts;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;

namespace AigoraNet.Common.Services;

public class AppInsightsTelemetryService : ITelemetryService
{
    private readonly TelemetryClient _client;
    private readonly IHttpContextAccessor _http;

    public AppInsightsTelemetryService(TelemetryClient client, IHttpContextAccessor http)
    {
        _client = client;
        _http = http;
    }

    public void TrackEvent(string name, IDictionary<string, string>? properties = null)
    {
        _client.TrackEvent(name, properties);
    }

    public void TrackException(Exception ex, IDictionary<string, string>? properties = null)
    {
        _client.TrackException(ex, properties);
    }

    public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
    {
        _client.GetMetric(name).TrackValue(value);
    }
}