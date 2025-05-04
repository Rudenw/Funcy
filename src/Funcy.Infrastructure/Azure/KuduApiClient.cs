using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Funcy.Infrastructure.Azure;

public class KuduApiClient(TokenCredential credential, HttpClient httpClient, IMemoryCache cache)
{
    public async Task<List<KuduFunctionInfo>> GetFunctionsAsync(string functionAppName, CancellationToken ct = default)
    {

        var token = await cache.GetOrCreateAsync("Token", async entry =>
        {
            var newToken = await credential.GetTokenAsync(
                new TokenRequestContext(["https://management.azure.com/.default"]), ct);

            entry.AbsoluteExpiration = newToken.ExpiresOn - TimeSpan.FromMinutes(1);

            return newToken;
        });

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);

        var scmHost = $"{functionAppName}.scm.azurewebsites.net";
        var url = $"https://{scmHost}/api/functions";

        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var results = new List<KuduFunctionInfo>();

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var name = element.GetProperty("name").GetString() ?? "unknown";

            string triggerType = "unknown";
            if (element.TryGetProperty("config", out var config) &&
                config.TryGetProperty("bindings", out var bindings) &&
                bindings.ValueKind == JsonValueKind.Array &&
                bindings.GetArrayLength() > 0)
            {
                triggerType = bindings[0].GetProperty("type").GetString() ?? "unknown";
            }

            results.Add(new KuduFunctionInfo
            {
                Name = name,
                TriggerType = triggerType
            });
        }

        return results;
    }
    
    public class KuduFunctionInfo
    {
        public string Name { get; set; } = string.Empty;
        public string TriggerType { get; set; } = "unknown";
    }
}