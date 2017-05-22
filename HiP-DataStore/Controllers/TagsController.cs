using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Tag = PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Tag;
using PaderbornUniversity.SILab.Hip.DataStore.Model;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/[controller]")]
    public class TagsController : Controller
    {
        private readonly EventStoreClient _ev;
        private readonly CacheDatabaseManager _db;
        private readonly EntityIndex _entityIndex;
        private readonly MediaIndex _mediaIndex;
        private readonly ReferencesIndex _referencesIndex;

        public TagsController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _ev = eventStore;
            _db = db;
            _entityIndex = indices.OfType<EntityIndex>().First();
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _referencesIndex = indices.OfType<ReferencesIndex>().First();
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(422)]     
        public async Task<IActionResult> PostAsync([FromBody]TagArgs tag)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            if (tag.Image != null&& !_mediaIndex.IsPublishedImage(tag.Image.Value)) 
                return StatusCode(422, ErrorMessages.ImageNotFoundOrNotPublished(tag.Image.Value));

            int id = _entityIndex.NextId(ResourceType.Tag);
            var ev = new TagCreated()
            {
                Id = id,
                Properties = tag
            };

            await _ev.AppendEventAsync(ev);

            if (tag.Image != null)
            {
                var newRef = new ReferenceAdded(ResourceType.Tag, id, ResourceType.Media, tag.Image.Value);
                await _ev.AppendEventAsync(newRef);
            }

            return Ok(id);

        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<TagResult>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(422)]
        public IActionResult GetAll(TagQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var query = _db.Database.GetCollection<Tag>(ResourceType.Tag.Name).AsQueryable();

            try
            {
                // TODO Add filtering by timestamp
                var tags = query
                    .FilterByIds(args.ExcludedIds, args.IncludedIds)
                    .FilterByStatus(args.Status)
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
                return StatusCode(422, e.Message);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TagResult),200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id,DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var query = _db.Database.GetCollection<Tag>(ResourceType.Tag.Name).AsQueryable();

            var tag = query.FirstOrDefault(x => x.Id == id);

            if (tag == null)
                return NotFound();

            if (timestamp != null && DateTimeOffset.Compare(tag.Timestamp, timestamp.GetValueOrDefault()) != 1)
                return StatusCode(304);

            var tagResult = TagResult.ConvertFromTag(tag);

            return Ok(tagResult);

        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> UpdateById(int id,[FromBody]TagUpdateArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            if (!_entityIndex.Exists(ResourceType.Tag, id))
                return NotFound();

            if (args.Image != null && !_mediaIndex.IsPublishedImage(args.Image.Value))
                return StatusCode(422, ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));

            var ev = new TagUpdated
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now,
                Status = args.Status ?? _entityIndex.Status(ResourceType.Tag,id).Value
            };
            await _ev.AppendEventAsync(ev);

            if (args.Image != null)
            {
                var newRef = new ReferenceAdded(ResourceType.Tag, id, ResourceType.Media, args.Image.Value);
                await _ev.AppendEventAsync(newRef);
            }

            return NoContent();
        }

        [HttpDelete("id")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteById(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            if (!_entityIndex.Exists(ResourceType.Tag, id))
                return NotFound();

            if (_referencesIndex.IsUsed(ResourceType.Tag, id))
                return BadRequest(ErrorMessages.ResourceInUse);


            var ev = new TagDeleted { Id = id };
           await _ev.AppendEventAsync(ev);

            //Remove references
            foreach(var reference in _referencesIndex.ReferencesOf(ResourceType.Tag, id))
            {
                var refRemoved = new ReferenceRemoved(ResourceType.Tag, id, reference.Type, reference.Id);
               await _ev.AppendEventAsync(refRemoved);
            }

            return NoContent();
        }


    }

}

