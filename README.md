# Priority Based Execution

Priority based execution is a capability which allows users to specify priority to the request sent to Cosmos DB. Based on the priority specified by the user, if there are more requests than the configured RU/S in a second, then Cosmos DB will throttle low priority requests to allow high priority requests to execute. 

This capability allows a user to perform more important tasks while delaying lesser important tasks when there are higher number of requests than what a container with configured RU/s can handle at a given time. The lesser important tasks will be continuously retried by any client using SDK based on the retry time and will be executed once the requirement of important (high priority) tasks are satisfied. 

This feature is not guaranteed to always throttle low priority requests in favour of high priority requests and there are no SLA’s associated with the feature. This is a best effort scenario and there is a time associated with the start of throttling of low priority requests which is dependent on the rate at which high priority requests are sent to Cosmos DB. 

## Supported API's and SDK's:
### API
- NoSQL
### SDK
- .NET
- Java

## How to get started: 
Step 1: Whitelist your account by completing the nomination form : [priority based throttling - preview request](https://forms.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR_kUn4g8ufhFjXbbwUF1gXFUMUQzUzFZSVkzODRSRkxXM0RKVDNUSDBGNi4u)

Step2: Download the SDK latest version
- .NET: [Azure Cosmos DB dotnetv3 SDK- 3.33.0-preview](https://www.nuget.org/packages/Microsoft.Azure.Cosmos/3.33.0-preview)
- Java: [Azure Cosmos DB Java SDK - 4.45.0](https://mvnrepository.com/artifact/com.azure/azure-cosmos/4.45.0)

Step3: Send us your feedback, comments, questions using the Issues tab on this repo. 

## Sample Code

### C#
```C#
Document doc = new Document() 
{ 
    Id = "id", 
    Address = "address1", 
    Pkey = "pkey1" 
}; 

// this code will insert an item in Cosmos DB container with low priority

using Microsoft.Azure.Cosmos.PartitionKey;
using Microsoft.Azure.Cosmos.PriorityLevel;
 
RequestOptions requestOptions = new ItemRequestOptions{PriorityLevel = PriorityLevel.Low};
ItemResponse<Document> response = await container.CreateItemAsync<Document(doc, new PartitionKey("pkey1"), requestOptions); 
```
### Java

```java

import com.azure.cosmos.ThroughputControlGroupConfig;
import com.azure.cosmos.ThroughputControlGroupConfigBuilder;
import com.azure.cosmos.models.CosmosItemRequestOptions;
import com.azure.cosmos.models.PriorityLevel;

class Family{
   String id;
   String lastName;
}

//define throughput control group with low priority
ThroughputControlGroupConfig groupConfig = new ThroughputControlGroupConfigBuilder()
                .groupName("low-priority-group")
                .priorityLevel(PriorityLevel.LOW)
                .build();
container.enableLocalThroughputControlGroup(groupConfig);

CosmosItemRequestOptions requestOptions = new CosmosItemRequestOptions();
        requestOptions.setThroughputControlGroupName(groupConfig.getGroupName());

Family family = new Family();
family.setLastName("Anderson");


// Insert this item with low priority in the container using request options.
container.createItem(family, new PartitionKey(family.getLastName()), requestOptions)
    .doOnSuccess((response) -> {
        logger.info("inserted doc with id: {}", response.getItem().getId());
    }).doOnError((exception) -> {
        logger.error("Exception. e: {}", exception.getLocalizedMessage(), exception);
    }).subscribe();

```

### Python

```python
import azure.cosmos.cosmos_client as cosmos_client
from azure.cosmos.partition_key import PartitionKey

# Define your Cosmos DB account and database details
endpoint = "<cosmos-db-endpoint>"
key = "<cosmos-db-key>"
database_id = "<database-name>"
container_id = "<container-name>"

# Create a Cosmos DB client
client = cosmos_client.CosmosClient(endpoint, key)

# Create or get a database
database = client.create_database_if_not_exists(id=database_id)

# Create or get a container
container = database.create_container_if_not_exists(
    id=container_id,
    partition_key=PartitionKey(path="/pkey"),
)

# Insert a document into the container with custom headers
document = {
    "id": "3",
    "name": "John Doe",
    "city": "New York",
}

# Define custom headers directly in the options
headers = {
    "x-ms-cosmos-priority-level": "Low"
}

container.create_item(body=document, headers = headers)

# Query documents in the container
query = "SELECT * FROM c"
items = list(container.query_items(query=query, enable_cross_partition_query=True, headers = headers))

for item in items:
    print(item)
```

## FAQ’s 

1. #### How many priority levels can be specified in the request?<br/>
    Currently, there are only 2 priority, high and low. 

2. #### What is the default priority of a request?<br/>
   By default, all requests are of high priority. 

3. #### Does enabling priority based throttling means reserving a fraction of RU/s for high priority requests?<br/>
   No, there is no reservation of RU/s. The user can use all their provisioned throughput irrespective of the priority of requests they send.  

4. #### What are the pricing changes associated with this feature?<br/>
   There is no cost associated with this feature, its free of charge. 
   
   
