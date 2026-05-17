using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Switchly.Sdk.Internal;

// RulesetFetcher'ın simetriği: o GET'le ruleset çekiyor, bu POST'la event batch'i gönderiyor.
// Hata durumunda exception fırlatır; BackgroundFlusher fail-soft handle eder.
internal sealed class ExposureSender(HttpClient http, IOptions<SwitchlyOptions> options)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SwitchlyOptions _opts = options.Value;

    public async Task SendAsync(IReadOnlyList<ExposureEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0) return;

        var url = $"{_opts.BaseUrl.TrimEnd('/')}/api/track/exposures";

        var payload = new TrackExposuresRequest(
            PublicKey: _opts.PublicKey,
            ProjectKey: _opts.ProjectKey,
            EnvironmentKey: _opts.EnvironmentKey,
            Events: events.Select(e => new TrackExposuresEventItem(
                UserKey: e.UserKey,
                FlagKey: e.FlagKey,
                VariantKey: e.VariantKey,
                IsOn: e.IsOn,
                OccurredAt: e.OccurredAt
            )).ToList()
        );

        using var response = await http.PostAsJsonAsync(url, payload, JsonOpts, ct);
        response.EnsureSuccessStatusCode();

        // Backend Response<T> wrapper'ı: 200 OK ile dönen success=false durumu da olabilir.
        var envelope = await response.Content
            .ReadFromJsonAsync<ApiResponse<TrackExposuresResponseData>>(JsonOpts, ct);

        if (envelope is null || !envelope.Success)
            throw new SwitchlyException(envelope?.Message ?? "Switchly track API returned an error.");
    }

    private sealed record TrackExposuresRequest(
        string PublicKey,
        string ProjectKey,
        string EnvironmentKey,
        List<TrackExposuresEventItem> Events
    );

    private sealed record TrackExposuresEventItem(
        string UserKey,
        string FlagKey,
        string? VariantKey,
        bool IsOn,
        DateTimeOffset OccurredAt
    );

    private sealed record TrackExposuresResponseData(int Accepted, int Skipped);

    private sealed record ApiResponse<T>(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("data")] T? Data
    );
}
