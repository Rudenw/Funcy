using System.Diagnostics;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Funcy.Infrastructure.Azure;

public class AzureSubscriptionService : IAzureSubscriptionService
{
    public async Task<string> GetCurrentSubscriptionId()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "az",
                Arguments = "account show --query id -o tsv",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var subscriptionId = await process.StandardOutput.ReadToEndAsync();
        return subscriptionId.Trim();
    }
    
    public async Task<string> GetCurrentSubscriptionName()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "az",
                Arguments = "account show --query name -o tsv",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var subscriptionName = await process.StandardOutput.ReadToEndAsync();
        return subscriptionName.Trim();
    }
}

public interface IAzureSubscriptionService
{
    Task<string> GetCurrentSubscriptionId();
    Task<string> GetCurrentSubscriptionName();
}