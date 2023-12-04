using Microsoft.Azure.Cosmos;

using static System.Net.HttpStatusCode;

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
    private Program()
    {
        this.client = new CosmosClient(
            accountEndpoint: Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")!,
            authKeyOrResourceToken: Environment.GetEnvironmentVariable("COSMOS_KEY")!);
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
        Console.WriteLine("Running workload without priority");
        await RunWorkloadScenario(1000, 1000, false, "without priority");
        Console.WriteLine("Waiting for 1 minute");
        //await Task.Delay(TimeSpan.FromMinutes(1));
        Console.WriteLine("Running workload with priority");
        await RunWorkloadScenario(1000,1000, true, "with priority");
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

        int numDocs = 1000;

        if (count < 1000)
        {
            List<Product> products = Product.GenerateProducts(numDocs, 0);
            await WriteDocumentsConcurrentlyAsync(products, numDocs);
        }
    }

    private async Task WriteDocumentsConcurrentlyAsync(List<Product> products, int counter)
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

        // for (int i = 0; i < simulationDurationSecs; i++)
        // {
            var lowPriorityTasks = Enumerable.Range(0, lowPriorityDocs)
                .Select(j => ReadDocumentAsync(j, 2, priorityEnabled))
                .ToList();

            var highPriorityTasks = Enumerable.Range(0, highPriorityDocs)
                .Select(j => ReadDocumentAsync(j, 1, priorityEnabled))
                .ToList();

            List<Task> tasks = new List<Task>();

            for(int k=0;k<lowPriorityDocs;k++){
                tasks.Add(lowPriorityTasks[k]);
                tasks.Add(highPriorityTasks[k]);
            }
            await Task.WhenAll(tasks);
            //await Task.Delay(100);
       // }
        PrintResults(priorityDescription);
    }

    private void PrintResults(string priorityDescription)
    {
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

    private async Task ReadDocumentAsync(int idNum, int priority, bool priorityEnabled)
    {
        string id = "id_" + idNum;

        ItemRequestOptions requestOptions = new ItemRequestOptions();
        if (priorityEnabled)
        {
            requestOptions.PriorityLevel = (priority == 1) ? PriorityLevel.High : PriorityLevel.Low;
        }

        try
        {
            await container.ReadItemAsync<Product>(id: id, partitionKey: new PartitionKey(id), requestOptions)
            .ContinueWith(itemResponse => {
                if (itemResponse!= null && itemResponse.IsCompletedSuccessfully)
                {
                    Console.WriteLine("Request completed: "+ id + " with priority "+ priority);
                    // if (priority == 1)
                    // {
                    //     Interlocked.Increment(ref totalHighSuccessful);
                    // }
                    // else
                    // {
                    //     Interlocked.Increment(ref totalLowSuccessful);
                    // }
                }
                else
                {
                    //Console.WriteLine("Read failed with: " + itemResponse.Exception);
                    Console.WriteLine("Request failed: "+ id + " with priority "+ priority);
                }
            });

        }
        catch (Exception ex)
        {
            Console.WriteLine("Read failed with exception: " + ex.Message);
            // if (ex.StatusCode == TooManyRequests)
            // {
            //     if (priority == 1)
            //     {
            //         Interlocked.Increment(ref totalHighThrottled);
            //     }
            //     else
            //     {
            //         Interlocked.Increment(ref totalLowThrottled);
            //     }
            // }
            // else
            // {
            //     Console.WriteLine("Read failed with exception: " + ex.Message);
            // }
        }
    }
}