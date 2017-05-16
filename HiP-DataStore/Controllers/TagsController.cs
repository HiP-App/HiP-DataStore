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

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/[controller]")]
    public class TagsController : Controller
    {
        private readonly EventStoreClient _ev;
        private readonly CacheDatabaseManager _db;
        private readonly EntityIndex _entityIndex;
        private readonly MediaIndex _mediaIndex;

        public TagsController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _ev = eventStore;
            _db = db;
            _entityIndex = indices.OfType<EntityIndex>().First();
            _mediaIndex = indices.OfType<MediaIndex>().First();
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> PostAsync([FromBody]TagArgs tag)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            if (tag.Image != null) {

                if (!_mediaIndex.ContainsId(tag.Image.GetValueOrDefault()))
                    return NotFound(new { Message = $"Media with {tag.Image} haven`t found" });

                if (!_mediaIndex.IsImage(tag.Image.GetValueOrDefault()))
                    return BadRequest(new { Message = $"Media with id: {tag.Image} is not of the Type: Audio" });
            }

            int id = _entityIndex.NextId<Tag>();
            var ev = new TagCreated()
            {
                Id = id,
                Properties = tag
            };

            await _ev.AppendEventAsync(ev, Guid.NewGuid());

            return Ok(new { Id = id });

        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<TagResult>), 200)]
        [ProducesResponseType(304)]
        public IActionResult GetAll(TagQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            var query = _db.Database.GetCollection<Tag>(Tag.CollectionName).AsQueryable();

            try
            {
                var tags = query
                    .FilterByIds(args.ExcludedIds, args.IncludedIds)
                    .FilterByStatus(args.Status)
                    .FilterIf(args.Used != null, x => x.IsUsed == args.Used)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .Paginate(args.Page, args.PageSize)
                    .ToList();
                    

                tags = tags.AsQueryable().FilterIf(args.Timestamp != null, x => DateTimeOffset.Compare(x.Timestamp, args.Timestamp.GetValueOrDefault()) == 1).ToList();
                if (args.Timestamp != null && tags.Count() == 0)
                    return StatusCode(304);


                var tagsResult = tags.Select(x => new TagResult
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    Used = x.IsUsed,
                    Image = x.Image.Id.AsNullableInt32,
                    Status = x.Status,
                    Timestamp = x.Timestamp
                }).ToList();

                var output = new AllItemsResult<TagResult> { Total = tagsResult.Count(), Items = tagsResult };

                return Ok(output);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
        }


    }

}

