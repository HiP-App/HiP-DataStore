using MyTested.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Controllers;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;
using Xunit;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests.ControllerTests
{
    public class TagsControllerTest
    {
        private TagIndex _tagIndex => MvcTestContext.Services.GetService<InMemoryCache>().Index<TagIndex>();
        private TagArgs TagArgs { get; set; }

        public TagsControllerTest()
        {
            TagArgs = new TagArgs
            {
                Description = "Hello",
                Image = null,
                Status = ContentStatus.Draft
            };
        }

        /// <summary>
        /// Returns ok if all tag Ids are retrieved.
        /// </summary>
        [Fact]
        public void GetIdsTest()
        {
            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.GetIds(ContentStatus.Draft))
                .ShouldReturn()
                .Ok();
        }

        /// <summary>
        /// Returns ok if all tags are retrieved.
        /// </summary>
        [Fact]
        public void GetAllTest()
        {
            var tagQueryArgs = new TagQueryArgs();

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.GetAll(tagQueryArgs))
                .ShouldReturn()
                .Ok();
        }

        /// <summary>
        /// Returns ok if tags are retrieved by Id.
        /// </summary>
        [Fact]
        public void GetByIdTest()
        {
            TagArgs.Title = "Germany";

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.Post(TagArgs)) //Creating a tag.
                .ShouldReturn()
                .Created();

            var tagId = _tagIndex.GetIdByTagTitle(TagArgs.Title).GetValueOrDefault(-1);

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.GetById(tagId,DateTime.Today))
                .ShouldReturn()
                .Ok();
        }

        /// <summary>
        /// Returns 404 if tags are not found.
        /// </summary>
        [Fact]
        public void GetByIdTest404()
        {
            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.GetById(10, DateTime.Now))
                .ShouldReturn()
                .NotFound();
        }

        /// <summary>
        /// Returns ok if tags are created.
        /// </summary>
        [Fact]
        public void PostAsyncTest()
        {
            TagArgs.Title = "Uni Paderborn";

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.Post(TagArgs)) //Creating a tag.
                .ShouldReturn()
                .Created();
            //This test will fail the next time when executed 
            //because it tries to create a tag with same title.
            //Hence deleting the tag with this title by getting it's Id
            //so that the next time the test will not fail.

            var tagId = _tagIndex.GetIdByTagTitle(TagArgs.Title).GetValueOrDefault(-1);

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.DeleteById(tagId))
                .ShouldReturn()
                .NoContent();
        }

        /// <summary>
        /// Returns 400 if model is invalid.
        /// </summary>
        [Fact]
        public void PostAsyncTest400()
        {
            var tagArgs = new TagArgs();

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.Post(tagArgs))
                .ShouldReturn()
                .BadRequest();
        }

        /// <summary>
        /// Returns 409 if tag already exist.
        /// </summary>
        [Fact]
        public void PostAsyncTest409()
        {
            TagArgs.Title = "Paderborn";

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.Post(TagArgs)) //Creating a tag.
                .ShouldReturn()
                .Created();

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.Post(TagArgs))
                .ShouldReturn()
                .StatusCode(409);
        }

        /// <summary>
        /// Returns 204 when tried to update tag without content.
        /// </summary>
        //[Fact]
        public void UpdateByIdTest204()
        {
            TagArgs.Title = "Updated";

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.UpdateById(1, TagArgs))
                .ShouldReturn()
                .NoContent();
        }

        /// <summary>
        /// Returns 404 if tag not found.
        /// </summary>
        [Fact]
        public void UpdateByIdTest404()
        {
            TagArgs.Title = "Hi";

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.UpdateById(0, TagArgs))
                .ShouldReturn()
                .NotFound();
        }

        /// <summary>
        /// Returns 400 for bad request.
        /// </summary>
        [Fact]
        public void UpdateByIdTest400()
        {
            var tagArgs = new TagArgs();

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.UpdateById(1, tagArgs))
                .ShouldReturn()
                .BadRequest();
        }

        /// <summary>
        /// Returns 404 when tag does not exist.
        /// </summary>
        [Fact]
        public void DeleteByIdTest404()
        {
            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.DeleteById(11))
                .ShouldReturn()
                .NotFound();
        }
    }
}
