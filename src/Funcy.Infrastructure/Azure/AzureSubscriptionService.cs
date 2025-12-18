using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Funcy.Core.Model;
using Funcy.Infrastructure.Azure.Models;
using Funcy.Infrastructure.Shell;

namespace Funcy.Infrastructure.Azure;

public class AzureSubscriptionService : IAzureSubscriptionService
{
    public async Task<string> GetCurrentSubscriptionId()
    {
        return await ShellCommandRunner.RunAsync("az", "account show --query id -o tsv");
    }
    
    public async Task<string> GetCurrentSubscriptionName()
    {
        return await ShellCommandRunner.RunAsync("az", "account show --query name -o tsv");
    }
    
    public async Task<List<FunctionAppGraphRow>> GetAllFunctionApps()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var subscriptionId = await GetCurrentSubscriptionId();
        var query =
            $"Resources | where subscriptionId == '{subscriptionId}' " +
            "| where type =~ 'microsoft.web/sites' " +
            "| where kind has 'functionapp' " +
            "| project id, name, resourceGroup, system=tostring(tags['System']), state=tostring(properties.state)";
        const int pageSize = 1000;

        var results = new List<FunctionAppGraphRow>();
        string? skipToken = null;

        while (true)
        {
            var graphQuery = BuildGraphArgs(query, pageSize, skipToken);
            var functionJson = await ShellCommandRunner.RunAsync("az", graphQuery);
            var response = JsonSerializer.Deserialize<GraphQueryResponse>(functionJson, options);
            if (response is null)
            {
                throw new InvalidOperationException("Failed to deserialize response");
            }
            
            results.AddRange(response.Data);
            skipToken = response.SkipToken;
            
            if (string.IsNullOrWhiteSpace(skipToken))
            {
                break;
            }
        }

        return results;
    }
    
    private static string BuildGraphArgs(string query, int first, string? skipToken)
    {
        var sb = new StringBuilder(256);
        sb.Append("graph query");
        sb.Append(" --first ");
        sb.Append(first);
        sb.Append(" -q ");
        sb.Append(Quote(query));

        if (!string.IsNullOrWhiteSpace(skipToken))
        {
            sb.Append(" --skip-token ");
            sb.Append(Quote(skipToken));
        }
        sb.Append(" -o json");
        return sb.ToString();
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}

public interface IAzureSubscriptionService
{
    Task<string> GetCurrentSubscriptionId();
    Task<string> GetCurrentSubscriptionName();
    Task<List<FunctionAppGraphRow>> GetAllFunctionApps();
}