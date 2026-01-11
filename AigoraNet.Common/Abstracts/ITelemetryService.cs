namespace AigoraNet.Common.Abstracts;

public interface ITelemetryService
{
    void TrackEvent(string name, IDictionary<string, string>? properties = null);
    void TrackException(Exception ex, IDictionary<string, string>? properties = null);
    void TrackMetric(string name, double value, IDictionary<string, string>? properties = null);
}
