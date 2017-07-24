using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Tag = PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Tag;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using Microsoft.AspNetCore.Authorization;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TagsController : Controller
    {
        private readonly EventStoreClient _ev;
        private readonly CacheDatabaseManager _db;
        private readonly EntityIndex _entityIndex;
        private readonly MediaIndex _mediaIndex;
        private readonly TagIndex _tagIndex;
        private readonly ReferencesIndex _referencesIndex;

        public TagsController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _ev = eventStore;
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
                    .PaginateAndSelect(args.Page, args.PageSize, x => TagResult.ConvertFromTag(x));


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

            var query = _db.Database.GetCollection<Tag>(ResourceType.Tag.Name).AsQueryable();

            var tag = query.FirstOrDefault(x => x.Id == id);

            if (tag == null)
                return NotFound();

            if (timestamp != null && tag.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var tagResult = TagResult.ConvertFromTag(tag);

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

            using (var transaction = _ev.BeginTransaction())
            {
                transaction.Append(ev);

                if (args.Image != null)
                    transaction.Append(new ReferenceAdded(ResourceType.Tag, id, ResourceType.Media, args.Image.Value));

                await transaction.CommitAsync();
            }

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

            using (var transaction = _ev.BeginTransaction())
            {
                transaction.Append(ev);

                var oldReferences = _referencesIndex
                    .ReferencesOf(ResourceType.Tag, ev.Id)
                    .Where(x => x.Type == ResourceType.Media)
                    .ToList();

                if (oldReferences.Count > 0)
                {
                    var oldRef = oldReferences.FirstOrDefault();
                    var oldRefEvent = new ReferenceRemoved(ResourceType.Tag, ev.Id, oldRef.Type, oldRef.Id);
                    transaction.Append(oldRefEvent);
                }

                if (args.Image != null)
                {
                    var newRefEvent = new ReferenceAdded(ResourceType.Tag, id, ResourceType.Media, args.Image.Value);
                    transaction.Append(newRefEvent);
                }

                await transaction.CommitAsync();
            }

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

            using (var transaction = _ev.BeginTransaction())
            {
                transaction.Append(ev);

                // Remove references
                foreach (var reference in _referencesIndex.ReferencesOf(ResourceType.Tag, id))
                    transaction.Append(new ReferenceRemoved(ResourceType.Tag, id, reference.Type, reference.Id));

                await transaction.CommitAsync();
            }

            return NoContent();
        }


        private void ValidateTagArgs(TagArgs args)
        {
            if (args == null)
                return;

            if (args.Image != null && !_mediaIndex.IsPublishedImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));
        }
    }

}

