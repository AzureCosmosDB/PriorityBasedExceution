import com.azure.cosmos.CosmosAsyncClient;
import com.azure.cosmos.CosmosAsyncContainer;
import com.azure.cosmos.CosmosClientBuilder;
import com.azure.cosmos.ThroughputControlGroupConfig;
import com.azure.cosmos.ThroughputControlGroupConfigBuilder;
import com.azure.cosmos.models.CosmosItemRequestOptions;
import com.azure.cosmos.models.CosmosItemResponse;
import com.azure.cosmos.models.PartitionKey;
import com.azure.cosmos.models.PriorityLevel;
import com.azure.identity.ManagedIdentityCredential;
import com.azure.identity.ManagedIdentityCredentialBuilder;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import java.util.UUID;

public class PriorityBasedExecution {
    public static void main(String[] args) {
        String endpoint = requiredEnvironmentVariable("COSMOS_ENDPOINT");
        String databaseId = requiredEnvironmentVariable("COSMOS_DATABASE");
        String containerId = requiredEnvironmentVariable("COSMOS_CONTAINER");

        ManagedIdentityCredentialBuilder credentialBuilder = new ManagedIdentityCredentialBuilder();
        String clientId = System.getenv("AZURE_CLIENT_ID");
        if (clientId != null && !clientId.trim().isEmpty()) {
            credentialBuilder.clientId(clientId.trim());
        }
        ManagedIdentityCredential credential = credentialBuilder.build();
        CosmosAsyncClient client = new CosmosClientBuilder()
            .endpoint(endpoint)
            .credential(credential)
            .buildAsyncClient();
        try {
            CosmosAsyncContainer container = client.getDatabase(databaseId).getContainer(containerId);
            ThroughputControlGroupConfig lowPriority = new ThroughputControlGroupConfigBuilder()
                .groupName("low-priority")
                .priorityLevel(PriorityLevel.LOW)
                .build();
            ThroughputControlGroupConfig highPriority = new ThroughputControlGroupConfigBuilder()
                .groupName("high-priority")
                .priorityLevel(PriorityLevel.HIGH)
                .build();
            container.enableLocalThroughputControlGroup(lowPriority);
            container.enableLocalThroughputControlGroup(highPriority);

            SampleItem item = new SampleItem();
            item.id = "priority-sample-" + UUID.randomUUID();
            item.pk = "priority-sample";
            item.name = "Background catalog update";
            PartitionKey partitionKey = new PartitionKey(item.pk);

            CosmosItemRequestOptions createOptions = new CosmosItemRequestOptions();
            createOptions.setThroughputControlGroupName(lowPriority.getGroupName());
            CosmosItemResponse<SampleItem> created = container
                .createItem(item, partitionKey, createOptions)
                .block();
            System.out.printf("Created %s with Low priority (%s RU).%n",
                created.getItem().id, created.getRequestCharge());

            CosmosItemRequestOptions readOptions = new CosmosItemRequestOptions();
            readOptions.setThroughputControlGroupName(highPriority.getGroupName());
            CosmosItemResponse<SampleItem> read = container
                .readItem(item.id, partitionKey, readOptions, SampleItem.class)
                .block();
            System.out.printf("Read %s with High priority (%s RU).%n",
                read.getItem().id, read.getRequestCharge());
        } finally {
            client.close();
        }
    }

    private static String requiredEnvironmentVariable(String name) {
        String value = System.getenv(name);
        if (value == null || value.trim().isEmpty()) {
            throw new IllegalStateException("Set the " + name + " environment variable.");
        }
        return value;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class SampleItem {
        public String id;
        public String pk;
        public String name;
    }
}
