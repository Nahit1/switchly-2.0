using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Switchly.Sdk.Internal;

internal sealed class RulesetFetcher(HttpClient http, IOptions<SwitchlyOptions> options)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SwitchlyOptions _opts = options.Value;

    public async Task<Ruleset> FetchAsync(CancellationToken ct = default)
    {
        var url = $"{_opts.BaseUrl.TrimEnd('/')}/api/flag/ruleset"
                  + $"?publicKey={Uri.EscapeDataString(_opts.PublicKey)}"
                  + $"&projectKey={Uri.EscapeDataString(_opts.ProjectKey)}"
                  + $"&environmentKey={Uri.EscapeDataString(_opts.EnvironmentKey)}";

        var envelope = await http.GetFromJsonAsync<ApiResponse<Ruleset>>(url, JsonOpts, ct)
            ?? throw new SwitchlyException("Empty response from Switchly API.");

        if (!envelope.Success || envelope.Data is null)
            throw new SwitchlyException(envelope.Message ?? "Switchly API returned an error.");

        return envelope.Data;
    }

    private sealed record ApiResponse<T>(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("data")] T? Data
    );
}
