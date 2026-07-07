using System.Collections.Concurrent;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.ServiceBus;
using Funcy.Core.Interfaces;
using Funcy.Core.KeyVault;
using Funcy.Core.Model;
using Microsoft.Extensions.Logging;

namespace Funcy.Infrastructure.Azure;

public class ServiceBusInsightService(
    ILogger<ServiceBusInsightService> logger,
    IAzureResourceService resourceService,
    IKeyVaultSecretResolver keyVaultSecretResolver,
    ArmClient client) : IServiceBusInsightService
{
    // Cached Service Bus namespaces per subscription (the probe candidate set). A Task is cached,
    // not the value, so concurrent apps in the same subscription share a single graph query.
    private readonly ConcurrentDictionary<string, Task<IReadOnlyList<(string Id, string Name)>>> _namespacesBySubscription = new();

    // Active + dead-letter message counts for one Service Bus entity.
    private readonly record struct CountDetails(long Active, long DeadLetter);

    public async Task<IReadOnlyList<ServiceBusCountResult>> GetCountsAsync(
        string functionAppArmId,
        IReadOnlyList<FunctionDetails> serviceBusFunctions,
        CancellationToken cancellationToken)
    {
        var results = new List<ServiceBusCountResult>(serviceBusFunctions.Count);
        if (serviceBusFunctions.Count == 0)
        {
            return results;
        }

        // Only pay for the app settings fetch when something still needs resolving: a namespace not
        // yet cached (may need the connection string / Key Vault) or a %SettingName% binding name.
        var needSettings = serviceBusFunctions.Any(f =>
            string.IsNullOrEmpty(f.ServiceBusNamespaceId)
            || HasPlaceholder(f.QueueName)
            || HasPlaceholder(f.TopicName)
            || HasPlaceholder(f.SubscriptionName));

        ServiceBusConnectionResolver? resolver = null;
        if (needSettings)
        {
            resolver = await TryBuildResolverAsync(functionAppArmId, cancellationToken);
        }

        foreach (var function in serviceBusFunctions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await GetCountsForFunctionAsync(functionAppArmId, function, resolver, cancellationToken));
        }

        return results;
    }

    private async Task<ServiceBusCountResult> GetCountsForFunctionAsync(
        string functionAppArmId,
        FunctionDetails function,
        ServiceBusConnectionResolver? resolver,
        CancellationToken cancellationToken)
    {
        // Resolve %SettingName% binding names first so they feed both the ARM lookup and the
        // display, regardless of whether the count fetch below succeeds.
        var (queueName, topicName, subscriptionName) = ResolveBindingNames(resolver, function);
        LogUnresolved(function.QueueName, queueName);
        LogUnresolved(function.TopicName, topicName);
        LogUnresolved(function.SubscriptionName, subscriptionName);

        var hasTarget = !string.IsNullOrEmpty(queueName)
                        || (!string.IsNullOrEmpty(topicName) && !string.IsNullOrEmpty(subscriptionName));
        if (!hasTarget)
        {
            return Failed(function, queueName, topicName, subscriptionName);
        }

        try
        {
            // Namespace already resolved once and cached (in SQLite): query it directly and never
            // re-resolve — reconfiguring an app to a different namespace is handled by clearing the
            // cache, not by re-probing on every refresh.
            if (!string.IsNullOrEmpty(function.ServiceBusNamespaceId))
            {
                var counts = await TryGetCountDetailsAsync(
                    function.ServiceBusNamespaceId, queueName, topicName, subscriptionName, cancellationToken);
                return ToResult(function, counts, function.ServiceBusNamespaceId, queueName, topicName, subscriptionName);
            }

            var (namespaceId, resolvedCounts) = await ResolveNamespaceAndFetchAsync(
                functionAppArmId, function, resolver, queueName, topicName, subscriptionName, cancellationToken);
            if (namespaceId is null)
            {
                logger.LogWarning("Could not resolve Service Bus namespace for function {Function}", function.Key);
                return Failed(function, queueName, topicName, subscriptionName);
            }

            return ToResult(function, resolvedCounts, namespaceId, queueName, topicName, subscriptionName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to fetch Service Bus counts for function {Function}", function.Key);
            return Failed(function, queueName, topicName, subscriptionName);
        }
    }

    private static ServiceBusCountResult ToResult(FunctionDetails function, CountDetails? counts, string namespaceId,
        string? queueName, string? topicName, string? subscriptionName)
        => counts is null
            ? new ServiceBusCountResult(function.Key, null, null, false, queueName, topicName, subscriptionName, namespaceId)
            : new ServiceBusCountResult(function.Key, counts.Value.Active, counts.Value.DeadLetter, true,
                queueName, topicName, subscriptionName, namespaceId);

    // Finds the namespace holding the target entity by probing the subscription's namespaces — the
    // count GET we make anyway is the probe, so the namespace where the entity exists is the answer.
    // Only when the probe is ambiguous (>1) or empty (0, e.g. a cross-subscription namespace) do we
    // fall back to the connection string's namespace host, reading a Key Vault secret only if the
    // connection setting is a Key Vault reference. Returns the resolved id and the counts it yielded.
    private async Task<(string? NamespaceId, CountDetails? Counts)> ResolveNamespaceAndFetchAsync(
        string functionAppArmId, FunctionDetails function, ServiceBusConnectionResolver? resolver,
        string? queueName, string? topicName, string? subscriptionName, CancellationToken cancellationToken)
    {
        var subscriptionId = new ResourceIdentifier(functionAppArmId).SubscriptionId!;
        var candidates = await GetCandidateNamespacesAsync(subscriptionId);

        var matches = new List<(string Id, CountDetails Counts)>();
        foreach (var ns in candidates)
        {
            var counts = await TryGetCountDetailsAsync(ns.Id, queueName, topicName, subscriptionName, cancellationToken);
            if (counts is not null)
            {
                matches.Add((ns.Id, counts.Value));
            }
        }

        if (matches.Count == 1)
        {
            return (matches[0].Id, matches[0].Counts);
        }

        // 0 or >1 matches: use the connection string's authoritative namespace name (Key Vault read
        // only when the connection is a reference) to disambiguate or reach a cross-subscription one.
        var namespaceName = await ResolveNamespaceNameAsync(function.ConnectionSetting, resolver, cancellationToken);
        if (!string.IsNullOrEmpty(namespaceName))
        {
            var named = candidates.FirstOrDefault(c => string.Equals(c.Name, namespaceName, StringComparison.OrdinalIgnoreCase));
            var namespaceId = named.Id ?? await resourceService.GetServiceBusNamespaceIdAsync(namespaceName);
            if (!string.IsNullOrEmpty(namespaceId))
            {
                var counts = await TryGetCountDetailsAsync(namespaceId, queueName, topicName, subscriptionName, cancellationToken);
                return (namespaceId, counts);
            }
        }

        // Ambiguous with no authoritative name: fall back to the first probe hit if there was one.
        return matches.Count > 0 ? (matches[0].Id, matches[0].Counts) : (null, null);
    }

    // The subscription's namespaces, cached so concurrent apps share one graph query. A faulted
    // lookup is evicted so a transient graph failure does not poison the cache for the session.
    private async Task<IReadOnlyList<(string Id, string Name)>> GetCandidateNamespacesAsync(string subscriptionId)
    {
        var task = _namespacesBySubscription.GetOrAdd(subscriptionId, resourceService.GetServiceBusNamespacesAsync);
        try
        {
            return await task;
        }
        catch
        {
            _namespacesBySubscription.TryRemove(new KeyValuePair<string, Task<IReadOnlyList<(string Id, string Name)>>>(subscriptionId, task));
            throw;
        }
    }

    // Reads the active/dead-letter counts for the target entity in a namespace. Returns null when
    // the entity does not exist there (404), so a probe can move on to the next candidate.
    private async Task<CountDetails?> TryGetCountDetailsAsync(string namespaceId, string? queueName,
        string? topicName, string? subscriptionName, CancellationToken cancellationToken)
    {
        var nsId = new ResourceIdentifier(namespaceId);
        try
        {
            if (!string.IsNullOrEmpty(topicName) && !string.IsNullOrEmpty(subscriptionName))
            {
                var id = ServiceBusSubscriptionResource.CreateResourceIdentifier(
                    nsId.SubscriptionId, nsId.ResourceGroupName, nsId.Name, topicName, subscriptionName);
                var data = (await client.GetServiceBusSubscriptionResource(id).GetAsync(cancellationToken)).Value.Data;
                return new CountDetails(data.CountDetails?.ActiveMessageCount ?? 0, data.CountDetails?.DeadLetterMessageCount ?? 0);
            }

            if (!string.IsNullOrEmpty(queueName))
            {
                var id = ServiceBusQueueResource.CreateResourceIdentifier(
                    nsId.SubscriptionId, nsId.ResourceGroupName, nsId.Name, queueName);
                var data = (await client.GetServiceBusQueueResource(id).GetAsync(cancellationToken)).Value.Data;
                return new CountDetails(data.CountDetails?.ActiveMessageCount ?? 0, data.CountDetails?.DeadLetterMessageCount ?? 0);
            }

            return null;
        }
        catch (global::Azure.RequestFailedException e) when (e.Status == 404)
        {
            // Entity not in this namespace — expected while probing candidates.
            return null;
        }
    }

    // Resolves the Service Bus namespace name from the connection setting: a fully qualified
    // namespace host or an inline connection string is parsed directly; a @Microsoft.KeyVault(...)
    // reference is first resolved to its secret (the actual connection string) via the shared
    // resolver, reusing the same Key Vault path the app-settings view uses.
    private async Task<string?> ResolveNamespaceNameAsync(
        string? connectionSetting,
        ServiceBusConnectionResolver? resolver,
        CancellationToken cancellationToken)
    {
        var connectionValue = resolver?.ResolveConnectionValue(connectionSetting);
        if (KeyVaultReferenceParser.TryParse(connectionValue, out var reference))
        {
            try
            {
                connectionValue = await keyVaultSecretResolver.ResolveAsync(reference!, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogWarning(e,
                    "Failed to resolve Key Vault reference for Service Bus connection setting {Setting}",
                    connectionSetting);
                return null;
            }
        }

        return ServiceBusConnectionResolver.ExtractNamespace(connectionValue);
    }

    private async Task<ServiceBusConnectionResolver?> TryBuildResolverAsync(
        string functionAppArmId,
        CancellationToken cancellationToken)
    {
        try
        {
            var webSite = client.GetWebSiteResource(ResourceIdentifier.Parse(functionAppArmId));
            var settings = (await webSite.GetApplicationSettingsAsync(cancellationToken)).Value.Properties;
            return new ServiceBusConnectionResolver(
                new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to fetch application settings for {FunctionApp}", functionAppArmId);
            return null;
        }
    }

    // Resolves a function's %SettingName% binding names against the app settings the resolver wraps.
    // Names with no matching setting (or when no resolver is available) are returned raw.
    public static (string? QueueName, string? TopicName, string? SubscriptionName) ResolveBindingNames(
        ServiceBusConnectionResolver? resolver, FunctionDetails function)
        => (Resolve(resolver, function.QueueName),
            Resolve(resolver, function.TopicName),
            Resolve(resolver, function.SubscriptionName));

    private static string? Resolve(ServiceBusConnectionResolver? resolver, string? value)
        => resolver is null ? value : resolver.ResolveValue(value);

    private void LogUnresolved(string? raw, string? resolved)
    {
        if (IsPlaceholder(raw) && HasPlaceholder(resolved))
        {
            logger.LogDebug(
                "Service Bus binding setting {Setting} not found in application settings; leaving name unresolved",
                raw![1..^1]);
        }
    }

    private static ServiceBusCountResult Failed(
        FunctionDetails function, string? queueName, string? topicName, string? subscriptionName)
        => new(function.Key, null, null, false, queueName, topicName, subscriptionName);

    private static bool HasPlaceholder(string? value) => value?.Contains('%') == true;

    private static bool IsPlaceholder(string? value)
        => value is { Length: >= 2 } && value[0] == '%' && value[^1] == '%';
}
