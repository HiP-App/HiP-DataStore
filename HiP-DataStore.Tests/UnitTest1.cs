using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
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
                CreateHttpClient = server.CreateClient
            };

            TestUserInjector.Name = "Administrator";
            TestUserInjector.Role = "Administrator";

            var id = await client.PostAsync(new ExhibitArgs
            {
                Name = "Sample Exhibit",
                Status = ContentStatus.Draft,
                AccessRadius = .001
            });

            TestUserInjector.Name = "Student";
            TestUserInjector.Role = "Student";

            var exhibit = await client.GetByIdAsync(id);
        }
    }
}
