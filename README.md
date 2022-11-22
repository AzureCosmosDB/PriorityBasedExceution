# PriorityBasedThrottling

Priority based throttling is a capability which allows users to specify priority to the request sent to Cosmos DB. Based on the priority specified by the user, if there are more requests than the configured RU/S in a second, then Cosmos DB will throttle low priority requests to allow high priority requests to execute. 

This capability allows a user to perform more important tasks while delaying lesser important tasks when there are higher number of requests than what a container with configured RU/s can handle at a given time. The lesser important tasks will be continuously retried by any client using SDK based on the retry time and will be executed once the requirement of important (high priority) tasks are satisfied. 

This feature is not guaranteed to always throttle low priority requests in favour of high priority requests and there are no SLA’s associated with the feature. This is a best effort scenario and there is a time associated with the start of throttling of low priority requests which is dependent on the rate at which high priority requests are sent to Cosmos DB. 

FAQ’s 

How many priority levels can be specified in the request? 
Currently, there are only 2 priority, high and low. 

What is the default priority of a request? 
By default, all requests are of high priority. 

Does enabling priority based throttling means reserving a fraction of RU/s for high priority requests? 
No, there is no reservation of RU/s. The user can use all their provisioned throughput irrespective of the priority of requests they send.  

Is there any cost associated with this feature? 
No, there is no cost associated with this feature, its free of charge. 
