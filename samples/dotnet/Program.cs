using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

static string RequiredEnvironmentVariable(string name)
{
    string? value = Environment.GetEnvironmentVariable(name);
    return !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new InvalidOperationException($"Set the {name} environment variable.");
}

string endpoint = RequiredEnvironmentVariable("COSMOS_ENDPOINT");
string databaseId = RequiredEnvironmentVariable("COSMOS_DATABASE");
string containerId = RequiredEnvironmentVariable("COSMOS_CONTAINER");

string? clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
ManagedIdentityId identity = string.IsNullOrWhiteSpace(clientId)
    ? ManagedIdentityId.SystemAssigned
    : ManagedIdentityId.FromUserAssignedClientId(clientId.Trim());
ManagedIdentityCredential credential = new(identity);
using CosmosClient client = new(endpoint, credential);
Container container = client.GetContainer(databaseId, containerId);
SampleItem item = new()
{
    Id = $"priority-sample-{Guid.NewGuid():N}",
    Pk = "priority-sample",
    Name = "Background catalog update"
};
PartitionKey partitionKey = new(item.Pk);

ItemResponse<SampleItem> created = await container.CreateItemAsync(
    item,
    partitionKey,
    new ItemRequestOptions { PriorityLevel = PriorityLevel.Low });
Console.WriteLine($"Created {created.Resource.Id} with Low priority ({created.RequestCharge} RU).");

ItemResponse<SampleItem> read = await container.ReadItemAsync<SampleItem>(
    item.Id,
    partitionKey,
    new ItemRequestOptions { PriorityLevel = PriorityLevel.High });
Console.WriteLine($"Read {read.Resource.Id} with High priority ({read.RequestCharge} RU).");

public sealed class SampleItem
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("pk")]
    public string Pk { get; set; } = "";

    [JsonProperty("name")]
    public string Name { get; set; } = "";
}
