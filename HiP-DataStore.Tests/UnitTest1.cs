using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Threading.Tasks;
using Xunit;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    public class UnitTest1
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

            var id = await client.PostAsync(new ExhibitArgs
            {
                Name = "Sample Exhibit",
                Status = ContentStatus.Draft,
                AccessRadius = .001
            });

            // Same user should be able to get the content she created
            var exhibit = await client.GetByIdAsync(id);

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
