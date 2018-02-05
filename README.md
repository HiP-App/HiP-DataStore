# HiP-DataStore

Microservice that manages the content for the [mobile app](https://github.com/HiP-App/HiP-Forms).

## Development Environment Setup
For testing purposes, install & run Event Store and MongoDB on your local machine. Steps on Windows:

* [Download Event Store](https://eventstore.org/downloads/)
    * Run with `EventStore.ClusterNode.exe --db ./db --log ./logs`
    * For further information, see documentation: [Connecting to a Server](https://eventstore.org/docs/dotnet-api/4.0.0/connecting-to-a-server/), especially section "URIs"
* [Download MongoDB](https://www.mongodb.com/download-center?jmp=docs)
    * Run with `mongod.exe`
    * Default database path: `C:\data\db` (if installed on drive C:)
    * For further information, see documentation: [Install on Windows](https://docs.mongodb.com/manual/tutorial/install-mongodb-on-windows/) and [The Mongo Shell](https://docs.mongodb.com/manual/mongo/)
* Launch the app
  * via Visual Studio: Open the solution (*.sln) and run the app (F5)
  * via Terminal: Execute `dotnet run` from the project folder containing `HiP-DataStore.csproj`

The app is preconfigured to run on dev machines without any manual configuration (using the event store and Mongo database on `localhost`). See [appsettings.json](https://github.com/HiP-App/HiP-DataStore/blob/develop/HiP-DataStore/appsettings.json) for a list of configuration fields and their default values.

