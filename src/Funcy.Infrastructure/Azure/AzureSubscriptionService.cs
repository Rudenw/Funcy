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
}

public interface IAzureSubscriptionService
{
    Task<string> GetCurrentSubscriptionId();
    Task<string> GetCurrentSubscriptionName();
}