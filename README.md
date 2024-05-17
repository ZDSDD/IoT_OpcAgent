# IoT_OpcAgent

IoT Agent written in .NET It integrates on-premise devices with Azure-based IoT services to create a _blazingly_ _fast_ and scalable system for factory operations.
## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Usage](#usage)
- [Configuration](#configuration)
## Overview

Provide a brief overview of the project. Describe its purpose and main features.

## Installation

###

### Download the project
Download zip file or run commads from your favourite CLI
```bash
git clone https://github.com/ZDSDD/IoT_OpcAgent.git
cd IoT_OpcAgent
```
### Azure Stream Analytics Job query
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
```
### 

## Usage
### Direct methods
+ [EmergencyStop](#emergencystop)
+ [ResetErrorStatus](#reseterrorstatus)

#### EmergencyStop
Calls the "EmergencyStop" method on a given device. It stops production. It can fire when a device experiences 3 errors in under a minute.
#### ResetErrorStatus
Calls the "ResetErrorStatus" method on a given device. It resets every error there is.

## Configuration
This section will show which local variables need to be set up, both on Azure App and on the OPC Agent.
### Sample settings for LOCAL development
This sample local.settings.json file should be located in the root folder of the solution.

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
    "senderAddress": "<email_that_sends_emails>"
  }
}
```
### Deploy on Azure 
If deployed on Azure, navigate to Settings -> Environment variables -> **Add application setting**. For official Microsoft documentation on setting up app settings on Azure, refer to the following link: [link](https://learn.microsoft.com/en-us/azure/app-service/reference-app-settings)

### Local Configuration for IoT Agent
To configure the IoT Agent locally, you must provide the .NET `secrets.json` file. [How to init secrets](https://learn.microsoft.com/pl-pl/aspnet/core/security/app-secrets)
```
{
  "ConnectionStrings": {
    "serverAddress": "server_adress", //i.e. 
    "IoTHub": "<IoTHub_connection_string>",
    "ServiceBus": "<serviceBus connection string>"
  },
  "Devices": [
    {
      "DeviceNodeId": "ns=2;s=Device 1",
      "DeviceConnectionString": "<iot_hub_device_connection_string1>"
    },
    {
      "DeviceNodeId": "ns=2;s=Device 2",
      "DeviceConnectionString": "<iot_hub_device_connection_string2>"
    },
    {
      "DeviceNodeId": "ns=2;s=Device 3",
      "DeviceConnectionString": "<iot_hub_device_connection_string4>"
    },
    {
      "DeviceNodeId": "ns=2;s=Device 4",
      "DeviceConnectionString": "<iot_hub_device_connection_string5>"
    }
  //... and more devices as you need.
  ]
}
```
