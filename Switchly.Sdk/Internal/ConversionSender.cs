using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Switchly.Sdk.Internal;

// ExposureSender'ın kardeşi: aynı pattern, farklı endpoint + farklı payload şekli.
internal sealed class ConversionSender(HttpClient http, IOptions<SwitchlyOptions> options)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SwitchlyOptions _opts = options.Value;

    public async Task SendAsync(IReadOnlyList<ConversionEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0) return;

        var url = $"{_opts.BaseUrl.TrimEnd('/')}/api/track/conversions";

        var payload = new TrackConversionsRequest(
            PublicKey: _opts.PublicKey,
            ProjectKey: _opts.ProjectKey,
            EnvironmentKey: _opts.EnvironmentKey,
            Events: events.Select(e => new TrackConversionsEventItem(
                UserKey: e.UserKey,
                EventName: e.EventName,
                Value: e.Value,
                PropertiesJson: e.PropertiesJson,
                OccurredAt: e.OccurredAt
            )).ToList()
        );

        using var response = await http.PostAsJsonAsync(url, payload, JsonOpts, ct);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content
            .ReadFromJsonAsync<ApiResponse<TrackConversionsResponseData>>(JsonOpts, ct);

        if (envelope is null || !envelope.Success)
            throw new SwitchlyException(envelope?.Message ?? "Switchly conversion track API returned an error.");
    }

    private sealed record TrackConversionsRequest(
        string PublicKey,
        string ProjectKey,
        string EnvironmentKey,
        List<TrackConversionsEventItem> Events
    );

    private sealed record TrackConversionsEventItem(
        string UserKey,
        string EventName,
        decimal? Value,
        string? PropertiesJson,
        DateTimeOffset OccurredAt
    );

    private sealed record TrackConversionsResponseData(int Accepted, int Skipped);

    private sealed record ApiResponse<T>(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("data")] T? Data
    );
}
