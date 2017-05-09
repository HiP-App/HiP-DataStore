using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/[controller]")]
    public class RoutesController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;

        public RoutesController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _entityIndex = indices.OfType<EntityIndex>().First();
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RouteResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public IActionResult Get(RouteQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new RouteQueryArgs();

            var query = _db.Database.GetCollection<Route>(Route.CollectionName).AsQueryable();

            try
            {
                var routes = query
                    .FilterByIds(args.ExcludedIds, args.IncludedIds)
                    .FilterByStatus(args.Status)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .Paginate(args.Page, args.PageSize)
                    .ToList();

                // TODO: What to do with timestamp?
                var results = routes.Select(x => new RouteResult
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    Duration = x.Duration,
                    Distance = x.Distance,
                    Image = (int?)x.Image.Id,
                    Audio = (int?)x.Audio.Id,
                    Exhibits = x.Exhibits.Select(id => (int)id).ToArray(),
                    Status = x.Status,
                    Tags = x.Tags.Select(id => (int)id).ToArray(),
                    Timestamp = x.Timestamp
                }).ToList();

                return Ok(results);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PostAsync([FromBody]RouteArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ensure referenced image exists and is published
            if (args.Image.HasValue && !_mediaIndex.IsPublishedImage(args.Image.Value))
                return StatusCode(422, ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));

            // ensure referenced audio exists and is published
            if (args.Audio.HasValue && !_mediaIndex.IsPublishedAudio(args.Audio.Value))
                return StatusCode(422, ErrorMessages.AudioNotFoundOrNotPublished(args.Audio.Value));

            // ensure referenced exhibits exist and are published
            if (args.Exhibits != null)
            {
                var invalidIds = args.Exhibits
                    .Where(id => _entityIndex.Status<Exhibit>(id) != ContentStatus.Published)
                    .ToList();

                if (invalidIds.Count > 0)
                    return StatusCode(422, ErrorMessages.ExhibitNotFoundOrNotPublished(invalidIds[0]));
            }

            // ensure referenced tags exist and are published
            if (args.Tags != null)
            {
                var invalidIds = args.Tags
                    .Where(id => _entityIndex.Status<Model.Entity.Tag>(id) != ContentStatus.Published)
                    .ToList();

                if (invalidIds.Count > 0)
                    return StatusCode(422, ErrorMessages.TagNotFoundOrNotPublished(invalidIds[0]));
            }

            var ev = new RouteCreated
            {
                Id = _entityIndex.NextId<Route>(),
                Properties = args
            };

            await _eventStore.AppendEventAsync(ev, Guid.NewGuid());
            return Ok();
        }
    }
}
