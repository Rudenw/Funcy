using System.Text.Json.Serialization;

namespace Funcy.Infrastructure.Azure.Models;

public record GraphQueryResponse(
    int Count,
    List<FunctionAppGraphRow> Data,
    [property: JsonPropertyName("skip_token")]
    string SkipToken,
    [property: JsonPropertyName("total_records")]
    int TotalCount);

public record FunctionAppGraphRow(string Id, string Name, string State, string System, string ResourceGroup, string SubscriptionId);