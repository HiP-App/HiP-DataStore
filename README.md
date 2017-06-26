# HiP DataStore

## Development Environment Setup
For testing purposes, install & run Event Store and MongoDB on your local machine. Steps on Windows:
* [Download Event Store](https://geteventstore.com/downloads/)
    * Run with `EventStore.ClusterNode.exe --db ./db --log ./logs`
    * For further information, see documentation: [Connecting to a Server](http://docs.geteventstore.com/dotnet-api/4.0.0/connecting-to-a-server/), especially section "URIs"
* [Download MongoDB](https://www.mongodb.com/download-center?jmp=docs)
    * Default database path: `C:\data\db` (if installed on drive C:)
    * Run with `mongod.exe`
    * For further information, see documentation: [Install on Windows](https://docs.mongodb.com/manual/tutorial/install-mongodb-on-windows/) and [The Mongo Shell](https://docs.mongodb.com/manual/mongo/)

Additionally you need to configure HiP-DataStore to use your local Event Store and MongoDB by **creating a file `appsettings.Development.json`**. As a template, you can use [`appsettings.Development.json.example`](HiP-DataStore/appsettings.Development.json.example).