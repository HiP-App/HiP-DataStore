using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.EventSourcing.FakeStore;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo.Test;
using System.Threading.Tasks;
using Xunit;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    public class ExhibitsTest
    {
        [Fact]
        public async Task Test1()
        {
            var server = new TestServer(new WebHostBuilder().UseStartup<TestStartup>());

            var client = new ExhibitsClient("")
            {
                CreateHttpClient = server.CreateClient,
                Authorization = "Admin-Administrator"
            };

            // Create an exhibit
            var exhibitArgs = new ExhibitArgs
            {
                Name = "Sample Exhibit",
                Status = ContentStatus.Draft,
                AccessRadius = .001
            };
            var id = await client.PostAsync(exhibitArgs);

            // Verify that the correct number of events was generated and the cache DB was updated
            var eventStream = FakeEventStore.Current.Streams["test"];
            Assert.Equal(3, eventStream.Events.Count);

            var mongoDb = FakeMongoDbContext.Current;
            var cachedExhibit = mongoDb.Get<Exhibit>((ResourceTypes.Exhibit, id));
            Assert.Equal(exhibitArgs.Name, cachedExhibit.Name);
            Assert.Equal(exhibitArgs.Status.ToString(), cachedExhibit.Status.ToString());
            Assert.Equal(exhibitArgs.AccessRadius, cachedExhibit.AccessRadius, precision: 3);

            // Same user should be able to get the content she created
            await client.GetByIdAsync(id);

            // Anonymous access should be forbidden
            client.Authorization = "";
            var exception = await Assert.ThrowsAsync<SwaggerException>(async () => await client.GetByIdAsync(id));
            Assert.True(exception.StatusCode == "401");

            // Access by another (non-admin) user should be forbidden
            client.Authorization = "SomeUser-Student";
            exception = await Assert.ThrowsAsync<SwaggerException>(async () => await client.GetByIdAsync(id));
            Assert.True(exception.StatusCode == "403");
        }
    }
}
