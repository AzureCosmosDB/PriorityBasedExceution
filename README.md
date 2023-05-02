# Priority Based Execution

Priority based execution is a capability which allows users to specify priority to the request sent to Cosmos DB. Based on the priority specified by the user, if there are more requests than the configured RU/S in a second, then Cosmos DB will throttle low priority requests to allow high priority requests to execute. 

This capability allows a user to perform more important tasks while delaying lesser important tasks when there are higher number of requests than what a container with configured RU/s can handle at a given time. The lesser important tasks will be continuously retried by any client using SDK based on the retry time and will be executed once the requirement of important (high priority) tasks are satisfied. 

This feature is not guaranteed to always throttle low priority requests in favour of high priority requests and there are no SLA’s associated with the feature. This is a best effort scenario and there is a time associated with the start of throttling of low priority requests which is dependent on the rate at which high priority requests are sent to Cosmos DB. 

## Supported API's and SDK's:
### API
- NoSQL
### SDK
- .NET

## How to get started: 
Step 1: Whitelist your account by completing the nomination form : [priority based throttling - preview request](https://forms.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR_kUn4g8ufhFjXbbwUF1gXFUMUQzUzFZSVkzODRSRkxXM0RKVDNUSDBGNi4u)

Step2: Download the Nuget Package: [Azure Cosmos DB dotnetv3 SDK- 3.30.0-preview](https://www.nuget.org/packages/Microsoft.Azure.Cosmos/3.33.0-preview)

Step3: Send us your feedback, comments, questions using the Issues tab on this repo. 

## Sample Code

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

## FAQ’s 

1. #### How many priority levels can be specified in the request?<br/>
    Currently, there are only 2 priority, high and low. 

2. #### What is the default priority of a request?<br/>
   By default, all requests are of high priority. 

3. #### Does enabling priority based throttling means reserving a fraction of RU/s for high priority requests?<br/>
   No, there is no reservation of RU/s. The user can use all their provisioned throughput irrespective of the priority of requests they send.  

4. #### What are the pricing changes associated with this feature?<br/>
   There is no cost associated with this feature, its free of charge. 
   
   
