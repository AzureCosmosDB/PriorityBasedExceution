const { randomUUID } = require("node:crypto");
const { CosmosClient, PriorityLevel } = require("@azure/cosmos");
const { ManagedIdentityCredential } = require("@azure/identity");

function requiredEnvironmentVariable(name) {
  const value = process.env[name];
  if (!value || !value.trim()) {
    throw new Error(`Set the ${name} environment variable.`);
  }
  return value;
}

async function main() {
  const endpoint = requiredEnvironmentVariable("COSMOS_ENDPOINT");
  const databaseId = requiredEnvironmentVariable("COSMOS_DATABASE");
  const containerId = requiredEnvironmentVariable("COSMOS_CONTAINER");
  const clientId = process.env.AZURE_CLIENT_ID?.trim();
  const credential = clientId
    ? new ManagedIdentityCredential({ clientId })
    : new ManagedIdentityCredential();
  const client = new CosmosClient({ endpoint, aadCredentials: credential });

  try {
    const container = client.database(databaseId).container(containerId);
    const item = {
      id: `priority-sample-${randomUUID()}`,
      pk: "priority-sample",
      name: "Background catalog update",
    };

    const created = await container.items.create(item, {
      priorityLevel: PriorityLevel.Low,
    });
    console.log(`Created ${created.resource.id} with Low priority (${created.requestCharge} RU).`);

    const read = await container.item(item.id, item.pk).read({
      priorityLevel: PriorityLevel.High,
    });
    console.log(`Read ${read.resource.id} with High priority (${read.requestCharge} RU).`);
  } finally {
    client.dispose();
  }
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
