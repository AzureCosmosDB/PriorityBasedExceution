# Priority-based execution

Priority-based execution lets an application assign **High** or **Low** priority to Azure Cosmos DB for NoSQL requests. When demand exceeds the configured throughput (RU/s), low-priority requests are throttled first on a best-effort basis, allowing important work to take precedence over background work.

There is no throughput reservation or additional feature charge. Prioritization is best effort, not a guarantee, and has no associated performance SLA. SDK retries follow the configured retry policy; requests can still fail when that policy is exhausted.

## Getting started

1. Use an Azure Cosmos DB for NoSQL account with provisioned throughput.
2. In the Azure portal, open the account's **Features** page and enable **Priority-based execution**. The old preview nomination form is no longer required.
3. Install a supported SDK and run one of the linked samples against a test container.

See the [priority-based execution documentation](https://learn.microsoft.com/azure/cosmos-db/priority-based-execution) for account configuration, monitoring, and default-priority settings.

## Minimum SDK versions

These are the requirements published in the [official documentation](https://learn.microsoft.com/azure/cosmos-db/priority-based-execution#sdk-requirements), not the latest available releases. Prefer the latest stable release within the supported SDK major version for production.

| SDK | Package | Minimum version | Sample file |
| --- | --- | --- | --- |
| .NET v3 | [Microsoft.Azure.Cosmos](https://www.nuget.org/packages/Microsoft.Azure.Cosmos/3.38.0) | 3.38.0 | [Program.cs](samples/dotnet/Program.cs) |
| Java v4 | [com.azure:azure-cosmos](https://central.sonatype.com/artifact/com.azure/azure-cosmos/4.45.0) | 4.45.0 | [PriorityBasedExecution.java](samples/java/src/main/java/PriorityBasedExecution.java) |
| Python v4 | [azure-cosmos](https://pypi.org/project/azure-cosmos/4.14.2/) | 4.14.2 | [priority_based_execution.py](samples/python/priority_based_execution.py) |
| JavaScript v4 | [@azure/cosmos](https://www.npmjs.com/package/@azure/cosmos/v/4.0.0) | 4.0.0 | [priority-based-execution.js](samples/javascript/priority-based-execution.js) |

The Python SDK exposed the `priority` option in earlier releases; the table follows the current published requirement rather than the first release containing that option.

## Running the samples

Each sample authenticates with **managed identity** and uses one client to create an item with **Low** priority and point-read that same item with **High** priority. Java uses local throughput-control groups to set priority, as supported by its minimum SDK version.

Before running:

- Enable priority-based execution on the account.
- Create a test database and container with partition key path **`/pk`**. The samples do not provision resources or change account settings.
- Run on an Azure host that supports managed identity, such as an Azure VM or App Service, with a system-assigned identity enabled or a user-assigned identity attached.
- Grant that identity the **Cosmos DB Built-in Data Contributor** data-plane role for the test container, or an equivalent custom role permitting metadata reads and item creation/reads. Use the container scope `/dbs/<database-name>/colls/<container-name>` where possible. An Azure management-plane role such as Contributor is not sufficient for item access. See [data-plane role assignments](https://learn.microsoft.com/azure/cosmos-db/how-to-connect-role-based-access-control#grant-data-plane-role-based-access) and [built-in data roles](https://learn.microsoft.com/azure/cosmos-db/reference-data-plane-security#built-in-roles).
- Set `COSMOS_ENDPOINT`, `COSMOS_DATABASE`, and `COSMOS_CONTAINER` in your process environment. In PowerShell, assign variables using `$env:COSMOS_DATABASE = "your-database"` and the same syntax for the other names.
- Leave `AZURE_CLIENT_ID` unset for the host's system-assigned identity, or set it to the **client ID** of an attached user-assigned managed identity. This is not the identity's principal/object ID.

All samples use `ManagedIdentityCredential` directly. They do not use account keys, client secrets, or a developer credential fallback. A local workstation without a managed-identity endpoint cannot authenticate simply by running `az login`; run the samples on the configured Azure host. Identity attachment and role assignment must be completed separately; the samples do not perform them.

Run commands from the repository root:

| SDK | Local prerequisites | Install/build | Run |
| --- | --- | --- | --- |
| .NET | .NET 10 SDK and runtime | `dotnet build samples\dotnet\PriorityBasedExecution.csproj` | `dotnet run --project samples\dotnet\PriorityBasedExecution.csproj` |
| Java | JDK 11 or later and Maven | `mvn -f samples\java\pom.xml compile` | `mvn -f samples\java\pom.xml exec:java` |
| Python | Python 3.10 or later; preferably a virtual environment | `python -m pip install -r samples\python\requirements.txt` | `python samples\python\priority_based_execution.py` |
| JavaScript | Node.js 22 or later | `npm --prefix samples\javascript install` | `npm --prefix samples\javascript start` |

The commands above use Windows path separators. On macOS or Linux, replace them with forward slashes.

Each run creates one uniquely named item and leaves it in the test container for inspection. Delete that item after testing if it is no longer needed. The output identifies the item and the requested priorities. These two sequential operations demonstrate the APIs, not throttling or a performance guarantee; observing prioritization requires competing traffic that exceeds provisioned throughput.

## Limitations

- Supported API: Azure Cosmos DB for **NoSQL**.
- Not supported for **serverless** accounts.
- Prioritization is non-deterministic for containers using shared database throughput.
- With strong or bounded staleness consistency, read-request prioritization may not work effectively.

## FAQ

### What priorities are available?

**High** and **Low**.

### What is the default priority?

The account default is **High** unless changed. When a request omits priority, the account's configured default applies. These samples explicitly set priority for both operations.

### Does priority reserve RU/s for high-priority requests?

No. Either priority can use the provisioned throughput when capacity is available.

### Does enabling this feature cost extra?

There is no additional feature charge. Normal Azure Cosmos DB usage charges still apply.

### Where can I send feedback?

Use this repository's [Issues](https://github.com/AzureCosmosDB/PriorityBasedExceution/issues) tab for feedback, questions, and sample problems. Do not include account keys or other sensitive information.
