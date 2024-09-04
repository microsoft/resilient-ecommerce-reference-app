
## General description

The API provides a an approach to the operations available on any usual e-commerce platform for ticket sales. The typical client workflow, enabled by this API's endpoints, would look something like:
1. Create a new user.
2. Retrieve a list of all upcoming concerts.
3. Add tickets to the cart.
4. Create a new order by checking out the tickets in the cart.
5. Retrieve all orders associated with the user.

Please note that orders are created based on the current state of the cart; items present in the cart at the time of order creation will be included in the order.

When a new order is created (a check-out operation is triggered), the following takes place:
1. All the tickets found in the user's cart are created in the DB -- a ticket entity for each.
2. A new order entity is created in the DB, linked to all the tickets created.
3. The payment is processed.
4. The cart is cleared.

TODO: To ensure the above operation's atomicity, a separate QCS service will be introduced (Service Bus / Functions / Logic Apps etc.).


## The API

### Carts

#### PUT /api/users/{userId}/Carts
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| userId | path | Puts a new item ({concertId}-{quantity} mapping) in the cart.  | Yes | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | OK |
| 404 | Not Found |

#### GET /api/users/{userId}/Carts
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| userId | path | The ID of the user whose cart to retrieve. | Yes | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | OK |

#### DELETE /api/users/{userId}/Carts
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| userId | path | The ID of the user whose cart to clear. | Yes | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | OK |

### Concerts

#### GET /api/Concerts
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| take | query | The endpoint returns a paged result. Specifies the size of the page (# concerts to retrieve). | No | integer |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | OK |

#### GET /api/Concerts/{concertId}
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| concertId | path | The ID of the concert to retrieve. | Yes | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | OK |
| 404 | Not Found |

### Orders

#### POST /api/users/{userId}/Orders
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| userId | path | The ID of the user initiating a new order. | Yes | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 201 | Created |
| 400 | Bad Request |
| 404 | Not Found |

#### GET /api/users/{userId}/Orders
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| userId | path | The ID of the user whose orders to retrieve. | Yes | string |
| skip | query | The endpoint returns a paged result. Specifies the number of orders, ordered by time, to skip. | No | integer |
| take | query | The endpoint returns a paged result. Specifies the size of the page (# orders to retrieve). | No | integer |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | OK |
| 404 | Not Found |

#### GET /api/users/{userId}/Orders/{orderId}
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| orderId | path | The ID of the order to retrieve. | Yes | string |
| userId | path | The ID of the user whose order to retrieve. | Yes | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | OK |
| 404 | Not Found |

### Users

#### GET /api/Users/{userId}
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| userId | path | The ID of the user to retrieve. | Yes | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | OK |
| 404 | Not Found |

#### PATCH: /api/Users/{userId}
##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| userId | path | The ID of the user to update. | Yes | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 202 | Accepted |
| 400 | Bad Request |
| 404 | Not Found |

#### POST /api/Users
##### Responses

| Code | Description |
| ---- | ----------- |
| 201 | Created |
| 400 | Bad Request |


## Tracking & Telemetry

All API requests accept query parameters for tracking. The provided values are captured and reported in the logs published to Azure Application Insights. The query parameters in question are:
| Query Parameter Key | Type | Description |
| ---- | ---- | ----------- |
| SESSION_ID | string | The session ID, used to correlate multiple subsequent requests together. |
| REQUEST_ID | string | The request ID, used to uniquely identify a client's request. |
| RETRY_COUNT | int | The number of the retry attempt, corellating to the client's retry strategy. |

All API responses also report, as part of their headers, the following:
| Header | Description |
| ---- | ----------- |
| AzRef-AppGwIp | The IP of the Application Gateway that has handled & routed the request. |
| AzRef-NodeIp | The private IP of the node that has handled the request. |
| AzRef-PodName | The name of the Kubernetes pod that has handled the request. |

## Test environment
The API requires a Redis Cache and a Database to run. To run the webapp locally, there are two options of provisioning the necessary datastore resources:

### 1. Self-host in local Docker containers

Run the `docker-compose-test.yml` file in the `test` folder found in the root of the repository. To start the webapp, along with self-hosted SQL Server and Redis Cache containers, run:
```bash
docker-compose -f docker-compose-test.yml up -d
```

The API is ready to process requests. See the SwaggerUI at `localhost:8080/api/swagger` for the available endpoints.

When you're done testing, stop the containers with:
```bash
docker-compose -f docker-compose-test.yml down
```

You can also run the health check to quickly validate the API works as expected:
```bash
bash run-healthcheck.sh
```

### 2. Link local webapp to datastore hosted in Azure

1. Create a SQL Server Database and a Redis Cache in Azure. **Note:** disable, during creation, the authentication through SAS tokens / connection strings.
2. Make sure you assign your identity (your `@microsoft` account) the necessary permissions to access the resources:
	- For the SQL Server, during creation, set your identity as the DB admin. On the first page of the SQL Server creation wizard:
		- Select the **"Use Microsoft Entra-only authentication"** option.
		- Add your identity as the admin by selecting the **"Set admin"** next to the **"Set Microsoft Entra admin"**. Set your identity as the admin.
		- Additionally, go to the **"Networking"** blade in the portal and select **"Selected networks"** for **"Public network access"**. Then **"Add your client IPv4 address"** from just below to enable network access from your local instance of the App to the DB.
	- For the Redis Cache, after creation, go to the **"Data Access Configuration"** blade in the portal and **"Add"** a new Redis user.
	    - For the **"Role"**, select **"Data Contributor"**.
		- For the **"User"**, select your identity (your `@microsoft` alias).
3. Set the following environment variables (either directly, or through `.env`/`launchSettings.json`):
  	- `"REDIS_ENDPOINT"`: "{RedisName}.redis.cache.windows.net",
	- `"SQL_ENDPOINT"`: "{SqlServerName}.database.windows.net",
	- `"SQL_APP_DATABASE_NAME"`: "{DataBaseName}",
4. Run the `Api.sln` solution to start the API on `localhost:51277`.
5. Login, when prompted through the browser, with your `@microsoft` account.

The API is ready to process requests. See the SwaggerUI at `localhost:51277/api/swagger` for the available endpoints.
