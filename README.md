# IoT_OpcAgent

IoT Agent written in .NET 6
## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Configuration](#configuration)
- [How it works](#how-it-works)
## Overview
IoT Agent integrates on-premise devices with Azure-based IoT services to create a _blazingly_ _fast_ and scalable system for factory operations.

## Installation

### Download the project
Download `.zip` file or run commads from your favourite CLI
```bash
git clone https://github.com/ZDSDD/IoT_OpcAgent.git
cd IoT_OpcAgent
```
### How to run IoT Agent
To run the application, you can [build it yourself](https://stackoverflow.com/questions/44074121/build-net-core-console-application-to-output-an-exe), or run the OpcAgent.exe
### How to run Azure Function
As the above, or [deploy](#deploy-to-azure) it to Azure so it can run constantly and listen for the requests.

## Configuration
This section will show which local variables need to be set up, both on Azure App and on the OPC Agent.

### Local Configuration for Function Apps
This sample local.settings.json file should be located in the root folder of the FunctionAppsDemo solution.

```json
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "AzureWebJobsStorageConnectionStringValue",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "ServiceBusConnectionString": "<my_service_bus_connection_string>",
    "QueueNameProduction": "<name_of_my_created_queue1>",
    "QueueNameErrorEvent": "<name_of_my_created_queue2>",
    "QueueNameThreeErrors": "<name_of_my_created_queue3>",
    "ThreeErrorsBlobContainerName": "<blob_container_name_in_azure>",
    "productionBlobContainerName": "<blob_container_name_in_azure>",
    "IoTHubConnectionString": "<my_IoTHuB_connection_string>",
    "Storage": "<my_storage_connection_string>",
    "CommunicationServiceConnectionString": "<my_communication_service_connection_string>",
    "senderAddress": "<email_that_sends_emails>",
    "emailTo": "<some@mail.com>"
  }
}
```
### Deploy to Azure 
If deployed on Azure, navigate to Settings -> Environment variables -> **Add application setting**.
>Official Microsoft documentation app-service-settings [link](https://learn.microsoft.com/en-us/azure/app-service/reference-app-settings)

### Local Configuration for IoT Agent
To configure the IoT Agent locally, you must provide the `secrets.json` file for the solution. [How to init secrets](https://learn.microsoft.com/pl-pl/aspnet/core/security/app-secrets)
```
{
  "ConnectionStrings": {
    "serverAddress": "<server_adress>", //i.e. "opc.tcp://localhost:4840/", 
    "IoTHub": "<IoTHub_connection_string>"
  },
  "Devices": [
    {
      "DeviceNodeId": "<device_node_id>", // i.e. "DeviceNodeId": "ns=2;s=Device 1"
      "DeviceConnectionString": "<iot_hub_device_connection_string1>"
    },
    {
      "DeviceNodeId": "<device_node_id>",
      "DeviceConnectionString": "<iot_hub_device_connection_string2>"
    },
    {
      "DeviceNodeId": "<device_node_id>",
      "DeviceConnectionString": "<iot_hub_device_connection_string4>"
    },
    {
      "DeviceNodeId": "<device_node_id>",
      "DeviceConnectionString": "<iot_hub_device_connection_string5>"
    }
  //... and more pre-defined devices as you need.
  ]
}
```

### Azure Stream Analytics Job query
In order for our buissness logic to work, we need to utilize ASA on Azure. 
> Azure Stream Analytics is a fully managed stream processing engine that is designed to analyze and process large volumes of streaming data with sub-millisecond latencies. - *Microsoft*

That's how the query should look like.

```sql
/*
Here are links to help you get started with Stream Analytics Query Language:
Common query patterns - https://go.microsoft.com/fwLink/?LinkID=619153
Query language - https://docs.microsoft.com/stream-analytics-query/query-language-elements-azure-stream-analytics
*/
SELECT
    System.Timestamp() AS WindowStartTime,
    IoTHub.ConnectionDeviceId,
    AVG(Temperature) AS AverageTemperature,
    MIN(Temperature) AS MinTemperature,
    MAX(Temperature) AS MaxTemperature
INTO
    [OUTPUT] -- Blob storage
FROM
    [INPUT] -- IoT Hub name i.e. [iot-seba]
WHERE
    System.Timestamp() >= DATEADD(minute, -5, System.Timestamp())  -- Data from the last 5 minutes

GROUP BY
    IoTHub.ConnectionDeviceId,
    TumblingWindow(minute, 1)

-- query 2

SELECT
    System.Timestamp() AS EventTime,
    IoTHub.ConnectionDeviceId
INTO
    [OUTPUT] -- Service Bus queue name
FROM
    [INPUT] -- IoT Hub name i.e. [iot-seba]
WHERE
    Event = 'error' AND ErrorsIncreased = 1
GROUP BY
    IoTHub.ConnectionDeviceId,
    TumblingWindow(second, 60)
HAVING
    COUNT(*) >= 3

-- Query 3: Production KPIs
SELECT
    System.Timestamp() AS WindowStartTime,
    IoTHub.ConnectionDeviceId as DeviceId,
    SUM(CASE WHEN GoodCount IS NULL THEN 0 ELSE GoodCount END) as TotalGoodCount,
    SUM(CASE WHEN BadCount IS NULL THEN 0 ELSE BadCount END) as TotalBadCount
INTO
    [OUTPUT] -- Service Bus queue name
FROM
    [INPUT] -- IoT Hub name i.e. [iot-seba]
GROUP BY
    IoTHub.ConnectionDeviceId,
    TumblingWindow(minute, 5)

-- query 4
SELECT
    *
INTO
    [OUTPUT]
FROM 
    [INPUT]
WHERE
    Event = 'error'
```

### Containers
To store data calculations, there is need to create containers on storage acount. Tha'st where data calculations will be stored.

### Temperatures data calculations
> Every 1 minute give me the average, minimum and maximum temperature over the last 5 minutes (grouped by device).\
> See query 1
#### Sample temperatures data stored in the container as blob.
```
{"WindowStartTime":"2024-05-17T19:10:00.0000000Z","ConnectionDeviceId":"device2","AverageTemperature":61.00682340912079,"MinTemperature":60.42769792801131,"MaxTemperature":61.86731380333519}
```
### Errors calculation
> Situations whenever a device experiences more than 3 errors in under 1 minute.
> See ASA query 2
#### Sample errors data
```
{"EventTime":"2024-05-17T19:35:00.0000000Z","ConnectionDeviceId":"device3"}
```
### Production KPIs
> % of good production in total volume, grouped by device in 5-minute windows.
> See ASA query 3

#### Sample Production KPIs data
```
{"WindowStartTime":"2024-05-17T19:35:00.0000000Z","DeviceId":"device4","TotalGoodCount":0.0,"TotalBadCount":0.0}
```

### Device Twin
Sample device twin on Azure IoT Hub 
```
{
//
    "properties": {
        "desired": {
            "$metadata": {
//
            },
            "$version": 33,
            "ProductionRate": 50,
            "telemetryConfig": {
                "sendFrequency": "10s"
            }
        },
        "reported": {
            "$metadata": {
//
            },
//
            "DeviceErrors": 1,
            "ProductionRate": 50
        }
    },
///
    },
///
}
```

## How it works
### Connection to the device (OPC UA server)
The agent retrieves device connection data from `secrets.json` and attempts to establish a connection. It will reject a device if there is no connection available.

### Telemetry
Agent sends telemetry data to the IoT Hub at fixed time intervals configured in the device twin on Azure. You can configure it under "properties" -> "desired" -> "telemetryConfig" ->__"sendFrequency"__.\
Example values: `10s`, `5m`, `2h`, `1420s`, `96m`.\
ContentEncoding: `UTF8`

#### Sample device twin config
```
{
    ...
    "properties": {
        "desired": {
            "$metadata": {
            ...
            },
            "$version": 32,
            "ProductionRate": 0,
            "telemetryConfig": {
                "sendFrequency": "10s"
            }
        },
        "reported": {
        ...
        }
}
```
### Sample telemetry message
```
{
  "body": {
    "ProductionStatus": 1,
    "WorkorderId": "c18358be-7f98-426b-862b-7d29c0c386fe",
    "GoodCount": 10,
    "BadCount": 1,
    "TotalGoodCount": 16,
    "TotalBadCount": 1,
    "Temperature": 77.23789701115987
  },
  "enqueuedTime": "Fri May 17 2024 17:29:38 GMT+0200 (czas środkowoeuropejski letni)"
}
```

### Handling errors
When a device encounters an error, it will send a single message to the IoTHub. This message will be processed by Azure Analytics Stream.

### Sample error event message 
```
{
  "body": {
    "Errors": 14,
    "DeviceNode": "Device 4",
    "Event": "error",
    "ErrorsIncreased": 1 // 0 if errors decreased.
  },
  "enqueuedTime": "Fri May 17 2024 17:31:28 GMT+0200 (czas środkowoeuropejski letni)"
}
```
### Emergency Stop (Direct Method)
- Method name: "EmergencyStop"
- required parameters: none
- what it does: it stops production on the machine.
#### Sample responses
```
{
    "status": 500,
    "payload": {
        "status": 500,
        "payload": {
            "message": "ns=2;s=Device 1: exception occured during emergency stop.",
            "exception_message": "Sample exception message"
        }
    }
}
```
```
{
    "status": 200,
    "payload": {
        "status": 200,
        "payload": {
            "message": "Emergency stop handled successfully."
        }
    }
}
```

### Reset Error Status (Direct Method)
- Method name: "ResetErrorStatus"
- required parameters: none
- what id does: it resets every error there is on the device
#### Sample responses
```
{
    "status": 500,
    "payload": {
        "status": 500,
        "payload": {
            "message": "ns=2;s=Device 1:  exception occured during Reset Error Status..",
            "exception_message": "Sample exception message"
        }
    }
}
```
```
{
    "status": 200,
    "payload": {
        "status": 200,
        "payload": {
            "message": "Reset Error Status handled successfully"
        }
    }
}
```

### Sends email
If a Device Error occurs (of any type), send an email to predefined address.\
This is done thanks to _Communication_ _Service_.

#### Sample email body 
 > There was an error with Device 1 Device error code: 5

## Azure services used in the project:
 + Function App
 + Storage account
 + Service Bus Queue
 + Service Bus Namespace
 + IoT Hub
 + Stream Analytics job
 + Communication Service

## Diagram

![iot drawio](https://github.com/ZDSDD/IoT_OpcAgent/assets/106777224/c5e3d65b-6e44-41b5-900c-28f437f0fc15)

