using Azure.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using static System.Net.HttpStatusCode;
using Microsoft.Azure.Documents.Client;

public class Program
{

    private static readonly string DatabaseName = "TestDatabase";
    private static readonly string ContainerName = "TestPBE";
    private static readonly string PartitionKey = "/id";
    private static readonly int manualThroughput = 400;

    private CosmosClient client;
    private Database database;
    private Container container;
    int totalLowThrottled = 0;
    int totalLowSuccessful = 0;

    int totalHighSuccessful = 0;
    int totalHighThrottled = 0;
    int numDocs = 2000;

    private Program()
    {
        CosmosClientBuilder builder = new CosmosClientBuilder(accountEndpoint: Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")!,
            authKeyOrResourceToken: Environment.GetEnvironmentVariable("COSMOS_KEY")!);
        builder.WithConnectionModeDirect().WithThrottlingRetryOptions(TimeSpan.FromSeconds(1), 0);
        this.client = builder.Build();
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("starting program!");
        Program program = new Program();
        await program.Setup();
        await program.Run();
    }

    private async Task Setup()
    {
        database = await client.CreateDatabaseIfNotExistsAsync(DatabaseName);
        ContainerProperties containerProperties = new ContainerProperties(ContainerName, partitionKeyPath: PartitionKey);



        var throughputProperties = ThroughputProperties.CreateManualThroughput(manualThroughput);
        container = await database.CreateContainerIfNotExistsAsync(containerProperties, throughputProperties);
        IngestData().Wait();
    }

    private async Task Run()
    {
        Console.WriteLine("Running workload with priority");
        await Task.Delay(TimeSpan.FromSeconds(10));
        await RunWorkloadScenario(300, 300, true, "with priority");
        Console.WriteLine("Waiting for 1 minute");
        Console.WriteLine("Running workload without priority");
        await RunWorkloadScenario(300, 300, false, "without priority");
    }

    private async Task IngestData()
    {
        int count = 0;
        using FeedIterator<int> feed = container.GetItemQueryIterator<int>(queryText: "SELECT VALUE count(1) from c");

        while (feed.HasMoreResults)
        {
            var response = await feed.ReadNextAsync();
            count = response.FirstOrDefault();
            Console.WriteLine("Number of documents in collection: " + count);
        }

        if (count < numDocs)
        {
            List<Product> products = Product.GenerateProducts(numDocs, 0);
            await WriteDocumentsConcurrentlyAsync(products);
        }
    }

    private async Task WriteDocumentsConcurrentlyAsync(List<Product> products)
    {
        int docs = 0;
        for (int i = 0; i < products.Count; i++)
        {
            await container.UpsertItemAsync<Product>(
                item: products[i],
                partitionKey: new PartitionKey(products[i].id)
            ).ContinueWith(itemResponse =>
            {
                if (itemResponse.IsCompletedSuccessfully)
                {
                    docs++;
                }
            });
        }
        Console.WriteLine(docs + " documents written successfully \n");
    }

    private async Task RunWorkloadScenario(int lowPriorityDocs, int highPriorityDocs, bool priorityEnabled, string priorityDescription)
    {
        ResetVariables();
        int simulationDurationSecs = 10;

        for (int i = 0; i < simulationDurationSecs; i++)
        {
            var taskList = Enumerable.Range(0, lowPriorityDocs + highPriorityDocs)
                .Select(j => ReadDocumentAsync(j, j%2, priorityEnabled))
                .ToList();

            await Task.WhenAll(taskList);
            await Task.Delay(100);
        }
        PrintResults(priorityDescription, priorityEnabled);
    }

    private void PrintResults(string priorityDescription, bool priorityEnabled)
    {
        if (priorityEnabled)
        {
            Console.WriteLine("Result with priority enabled");
        }
        else
        {
            Console.WriteLine("Results without priority enabled");
        }
        Console.WriteLine($"Total high successful requests {priorityDescription}: {totalHighSuccessful}, total high throttled {priorityDescription}: {totalHighThrottled}");
        Console.WriteLine($"Total low successful requests {priorityDescription}: {totalLowSuccessful}, total low throttled {priorityDescription}: {totalLowThrottled}");
        Console.WriteLine("\n");
    }

    private void ResetVariables()
    {
        totalHighSuccessful = 0;
        totalHighThrottled = 0;
        totalLowSuccessful = 0;
        totalLowThrottled = 0;
    }

    private Task ReadDocumentAsync(int idNum, int priority, bool priorityEnabled)
    {
        string id = "id_" + idNum;

        ItemRequestOptions requestOptions = new ItemRequestOptions();
        if (priorityEnabled)
        {
            requestOptions.PriorityLevel = (priority == 1) ? PriorityLevel.High : PriorityLevel.Low;
        }

        return container.ReadItemAsync<Product>(id: id, partitionKey: new PartitionKey(id), requestOptions)
        .ContinueWith(itemResponse =>
        {
            if (itemResponse.IsCompletedSuccessfully)
            {
                if (priority == 1)
                {
                    Interlocked.Increment(ref totalHighSuccessful);
                }
                else
                {
                    Interlocked.Increment(ref totalLowSuccessful);
                }
            }
            else
            {
                //Console.WriteLine("Request failed: " + id + " with priority " + priority);

                CosmosException cosmosException = (CosmosException)itemResponse.Exception.InnerException;
                if (cosmosException != null && cosmosException.StatusCode == TooManyRequests)
                {
                    if (priority == 1)
                    {
                        Interlocked.Increment(ref totalHighThrottled);
                    }
                    else
                    {
                        Interlocked.Increment(ref totalLowThrottled);
                    }
                }
                else
                {
                    Console.WriteLine("Read failed with exception: " + itemResponse.Exception);
                }
            }
        });

    }
}