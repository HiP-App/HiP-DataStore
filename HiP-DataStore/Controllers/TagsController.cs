﻿using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tag = PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Tag;
using Microsoft.AspNetCore.Authorization;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TagsController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly EntityIndex _entityIndex;
        private readonly MediaIndex _mediaIndex;
        private readonly TagIndex _tagIndex;
        private readonly ReferencesIndex _referencesIndex;

        public TagsController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _eventStore = eventStore;
            _db = db;
            _entityIndex = indices.OfType<EntityIndex>().First();
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _tagIndex = indices.OfType<TagIndex>().First();
            _referencesIndex = indices.OfType<ReferencesIndex>().First();
        }

        [HttpGet("ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetIds(ContentStatus? status)
        {
            return Ok(_entityIndex.AllIds(ResourceType.Tag, status ?? ContentStatus.Published));
        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<TagResult>), 200)]
        [ProducesResponseType(304)]
        public IActionResult GetAll(TagQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var query = _db.Database.GetCollection<Tag>(ResourceType.Tag.Name).AsQueryable();

            try
            {
                var tags = query
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByStatus(args.Status)
                    .FilterByTimestamp(args.Timestamp)
                    .FilterByUsage(args.Used)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .PaginateAndSelect(args.Page, args.PageSize, x => new TagResult(x)
                    {
                        Timestamp = _referencesIndex.LastModificationCascading(ResourceType.Tag, x.Id)
                    });


                return Ok(tags);
            }
            catch (InvalidSortKeyException e)
            {
                ModelState.AddModelError(nameof(args.OrderBy), e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TagResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tag = _db.Database.GetCollection<Tag>(ResourceType.Tag.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == id);

            if (tag == null)
                return NotFound();

            if (timestamp != null && tag.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var tagResult = new TagResult(tag)
            {
                Timestamp = _referencesIndex.LastModificationCascading(ResourceType.Tag, id)
            };

            return Ok(tagResult);

        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> PostAsync([FromBody]TagArgs args)
        {
            ValidateTagArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (_tagIndex.IsTitleExist(args.Title))
                return StatusCode(409);

            int id = _entityIndex.NextId(ResourceType.Tag);

            var ev = new TagCreated
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Tags/{id}", id);

        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> UpdateById(int id, [FromBody]TagArgs args)
        {
            ValidateTagArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Tag, id))
                return NotFound();

            var tagIdWithSameTitle = _tagIndex.GetIdByTagTitle(args.Title);

            if (tagIdWithSameTitle != null && tagIdWithSameTitle != id)
                return StatusCode(409, ErrorMessages.TagNameAlreadyUsed);

            var ev = new TagUpdated
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now,
            };

            await _eventStore.AppendEventAsync(ev);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteById(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Tag, id))
                return NotFound();

            if (_referencesIndex.IsUsed(ResourceType.Tag, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            var ev = new TagDeleted { Id = id };
            await _eventStore.AppendEventAsync(ev);
            return NoContent();
        }

        [HttpGet("{id}/Refs")]
        [ProducesResponseType(typeof(ReferenceInfoResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetReferenceInfo(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return ReferenceInfoHelper.GetReferenceInfo(ResourceType.Tag, id, _entityIndex, _referencesIndex);
        }


        private void ValidateTagArgs(TagArgs args)
        {
            if (args == null)
                return;

            // ensure referenced image exists
            if (args.Image != null && !_mediaIndex.IsImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFound(args.Image.Value));
        }
    }

}

