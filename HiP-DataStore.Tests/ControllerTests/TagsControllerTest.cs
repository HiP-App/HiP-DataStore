using MyTested.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Controllers;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using Xunit;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    public class TagsControllerTest
    {
        /// <summary>
        /// Tests response from Get action of UsersController.
        /// </summary>
        [Fact]
        public void GetUserListTest()
        {
            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.GetIds(ContentStatus.Draft))
                .ShouldReturn()
                .Ok();
        }
    }
}
