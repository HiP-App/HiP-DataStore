using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    public class MediaTest
    {
        public const string StudentUser = "SomeStudent";
        public const string AdminUser = "TheAdmin";
        public const string SampleImagePath = "Assets/SampleImage.png";

        [Fact]
        public async Task Test()
        {
            var server = new TestServer(new WebHostBuilder().UseStartup<TestStartup>());

            var client = new MediaClient("")
            {
                CreateHttpClient = server.CreateClient,
                Authorization = StudentUser + "-Student"
            };

            // Create a medium (as student user)
            var id = await client.PostAsync(new MediaArgs
            {
                Title = "Sample media",
                Type = MediaType.Image
            });

            var oldTimestamp = (await client.GetByIdAsync(id)).Timestamp;

            // Upload an image (as admin user)
            client.Authorization = AdminUser + "-Administrator";
            var originalFileSize = new FileInfo(SampleImagePath).Length;
            using (var stream = File.OpenRead(SampleImagePath))
                await client.PutFileByIdAsync(id, new FileParameter(stream, Path.GetFileName(SampleImagePath)));

            // Verify that timestamp changed, but not the user ID
            var medium = await client.GetByIdAsync(id);
            Assert.Equal(StudentUser, medium.UserId);
            Assert.True(medium.Timestamp > oldTimestamp);

            // Verify that we can download the file and it's the same we uploaded
            var file = await client.GetFileByIdAsync(id);
            using (var memStream = new MemoryStream())
            {
                file.Stream.CopyTo(memStream);
                Assert.Equal(originalFileSize, memStream.Length);
            }
        }
    }
}
