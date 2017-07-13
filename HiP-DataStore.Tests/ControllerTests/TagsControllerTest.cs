using System;
using MyTested.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Controllers;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    public class TagsControllerTest
    {
        private readonly TagIndex _tagIndex;

        public TagsControllerTest(IEnumerable<IDomainIndex> indices)
        {
            _tagIndex = indices.OfType<TagIndex>().First();
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
            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.GetById(0, new DateTime(2017, 07, 02)))
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
            var tagArgs = new TagArgs
            {
                Title = "Uni Paderborn",
                Description = "Paderborner Dom",
                Image = null,
                Status = ContentStatus.Draft
            };

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.PostAsync(tagArgs)) //Creating a tag.
                .ShouldReturn()
                .Ok();
            //This test will fail the next time when executed 
            //because it tries to create a tag with same title.
            //Hence deleting the tag with this title by getting it's Id
            //so that the next time the test will not fail.

            var IdByTagTitle = (int) _tagIndex.GetIdByTagTitle(tagArgs.Title);

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.DeleteById(IdByTagTitle))
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
                .Calling(c => c.PostAsync(tagArgs))
                .ShouldReturn()
                .BadRequest();
        }

        /// <summary>
        /// Returns 409 if tag already exist.
        /// </summary>
        [Fact]
        public void PostAsyncTest409()
        {
            var tagArgs = new TagArgs
            {
                Title = "Dom",
                Description = "Paderborner Dom",
                Image = null,
                Status = ContentStatus.Draft
            };

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.PostAsync(tagArgs))
                .ShouldReturn()
                .StatusCode(409);
        }

        /// <summary>
        /// Returns 204 when tried to update tag without content.
        /// </summary>
        //[Fact]
        public void UpdateByIdTest204()
        {
            var tagArgs = new TagArgs
            {
                Title = "Update",
                Description = "Updated"
            };

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.UpdateById(1,tagArgs))
                .ShouldReturn()
                .NoContent();
        }
        
        /// <summary>
        /// Returns 404 if tag not found.
        /// </summary>
        [Fact]
        public void UpdateByIdTest404()
        {
            var tagArgs = new TagArgs
            {
                Title = "Update",
                Description = "Updated"
            };

            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.UpdateById(1, tagArgs))
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
        /// Returns 204 when tried to delete tag without content.
        /// </summary>
        [Fact]
        public void DeleteByIdTest204()
        {
            MyMvc
                .Controller<TagsController>()
                .Calling(c => c.DeleteById(12))
                .ShouldReturn()
                .NoContent();
            //Commenting this test as it will fail the next time when executed 
            //because it tries to delete the same tag.
            //To check if this works, execute in your local.
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
