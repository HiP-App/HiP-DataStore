# HiP DataStore

## Development Environment Setup
At the moment, EventStore and MongoDB endpoints are hard-coded to localhost. Thus, for testing purposes, install & run EventStore and MongoDB on your local machine:
* [Download EventStore](https://geteventstore.com/downloads/)
    * Run with `EventStore.ClusterNode.exe --db ./db --log ./logs`
* [Download MongoDB](https://www.mongodb.com/download-center?jmp=docs)
    * Default database path: `C:\data\db` (if installed on drive C:)
    * Run with `mongod.exe`
    * See documentation: [Install on Windows](https://docs.mongodb.com/manual/tutorial/install-mongodb-on-windows/)
    * See documentation: [The Mongo Shell](https://docs.mongodb.com/manual/mongo/)
