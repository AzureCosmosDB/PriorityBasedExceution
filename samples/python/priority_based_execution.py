import os
import uuid

from azure.cosmos import CosmosClient
from azure.identity import ManagedIdentityCredential


def required_environment_variable(name):
    value = os.environ.get(name)
    if not value or not value.strip():
        raise ValueError(f"Set the {name} environment variable.")
    return value


def main():
    endpoint = required_environment_variable("COSMOS_ENDPOINT")
    database_id = required_environment_variable("COSMOS_DATABASE")
    container_id = required_environment_variable("COSMOS_CONTAINER")
    client_id = os.environ.get("AZURE_CLIENT_ID", "").strip() or None

    with ManagedIdentityCredential(client_id=client_id) as credential, CosmosClient(
        endpoint, credential=credential
    ) as client:
        container = client.get_database_client(database_id).get_container_client(container_id)
        item = {
            "id": f"priority-sample-{uuid.uuid4().hex}",
            "pk": "priority-sample",
            "name": "Background catalog update",
        }

        created = container.create_item(body=item, priority="Low")
        print(f"Created {created['id']} with Low priority.")

        read = container.read_item(
            item=item["id"], partition_key=item["pk"], priority="High"
        )
        print(f"Read {read['id']} with High priority.")


if __name__ == "__main__":
    main()
