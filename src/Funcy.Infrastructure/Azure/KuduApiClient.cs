using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Funcy.Infrastructure.Azure;

public class KuduApiClient(ILogger<KuduApiClient> logger, TokenCredential credential, IHttpClientFactory httpClientFactory, IAppCache cache)
{
    public async Task<List<KuduFunctionInfo>> GetFunctionsAsync(string functionAppName, CancellationToken ct = default)
    {

        var tokenStopwatch = new Stopwatch();
        tokenStopwatch.Start();
        var token = await cache.GetOrAddAsync("Token", async entry =>
        {
            var newToken = await credential.GetTokenAsync(
                new TokenRequestContext(["https://management.azure.com/.default"]), ct);

            entry.AbsoluteExpiration = newToken.ExpiresOn - TimeSpan.FromMinutes(1);

            return newToken;
        });
        tokenStopwatch.Stop();

        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);

        var scmHost = $"{functionAppName}.scm.azurewebsites.net";
        var url = $"https://{scmHost}/api/functions";

        var httpStopwatch = new Stopwatch();
        httpStopwatch.Start();
        var response = await httpClient.GetAsync(url, ct);
        httpStopwatch.Stop();
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var results = new List<KuduFunctionInfo>();

        var enumStopwatch = new Stopwatch();
        enumStopwatch.Start();
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
        enumStopwatch.Stop();
        
        logger.LogInformation("Getting token took {Milliseconds}ms", tokenStopwatch.ElapsedMilliseconds);
        logger.LogInformation("Getting functions took {Milliseconds}ms", httpStopwatch.ElapsedMilliseconds);
        logger.LogInformation("Enumerating functions took {Milliseconds}ms", enumStopwatch.ElapsedMilliseconds);

        return results;
    }
    
    public class KuduFunctionInfo
    {
        public string Name { get; set; } = string.Empty;
        public string TriggerType { get; set; } = "unknown";
    }
}