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
        private readonly ReferencesIndex _referencesIndex;

        public RoutesController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _entityIndex = indices.OfType<EntityIndex>().First();
            _referencesIndex = indices.OfType<ReferencesIndex>().First();
        }
        
        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<RouteResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public IActionResult Get(RouteQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new RouteQueryArgs();

            var query = _db.Database.GetCollection<Route>(ResourceType.Route.Name).AsQueryable();

            try
            {
                // TODO: What to do with timestamp?
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
                    .PaginateAndSelect(args.Page, args.PageSize, x => new RouteResult
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
                    });

                return Ok(routes);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RouteResult), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            var route = _db.Database.GetCollection<Route>(ResourceType.Route.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == id);

            if (route == null)
                return NotFound();

            // Route instance wasn`t modified after timestamp
            if (timestamp != null && route.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new RouteResult
            {
                Id = route.Id,
                Title = route.Title,
                Description = route.Description,
                Duration = route.Duration,
                Distance = route.Distance,
                Image = (int?)route.Image.Id,
                Audio = (int?)route.Audio.Id,
                Exhibits = route.Exhibits.Select(i => (int)i).ToArray(),
                Status = route.Status,
                Tags = route.Tags.Select(i => (int)i).ToArray(),
                Timestamp = route.Timestamp
            };

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PostAsync([FromBody]RouteArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ensure referenced image exists and is published
            if (args.Image != null && !_mediaIndex.IsPublishedImage(args.Image.Value))
                return StatusCode(422, ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));

            // ensure referenced audio exists and is published
            if (args.Audio != null && !_mediaIndex.IsPublishedAudio(args.Audio.Value))
                return StatusCode(422, ErrorMessages.AudioNotFoundOrNotPublished(args.Audio.Value));

            // ensure referenced exhibits exist and are published
            if (args.Exhibits != null)
            {
                var invalidIds = args.Exhibits
                    .Where(id => _entityIndex.Status(ResourceType.Exhibit, id) != ContentStatus.Published)
                    .ToList();

                if (invalidIds.Count > 0)
                    return StatusCode(422, ErrorMessages.ExhibitNotFoundOrNotPublished(invalidIds[0]));
            }

            // ensure referenced tags exist and are published
            if (args.Tags != null)
            {
                var invalidIds = args.Tags
                    .Where(id => _entityIndex.Status(ResourceType.Tag, id) != ContentStatus.Published)
                    .ToList();

                if (invalidIds.Count > 0)
                    return StatusCode(422, ErrorMessages.TagNotFoundOrNotPublished(invalidIds[0]));
            }

            // validation passed, emit events (create route, add references to image, audio, exhibits and tags)
            var ev = new RouteCreated
            {
                Id = _entityIndex.NextId(ResourceType.Route),
                Properties = args
            };

            if (args.Image != null)
            {
                var imageRef = new ReferenceAdded(ResourceType.Route, ev.Id, ResourceType.Media, args.Image.Value);
                await _eventStore.AppendEventAsync(imageRef);
            }

            if (args.Audio != null)
            {
                var audioRef = new ReferenceAdded(ResourceType.Route, ev.Id, ResourceType.Media, args.Audio.Value);
                await _eventStore.AppendEventAsync(audioRef);
            }

            if (args.Exhibits != null)
            {
                foreach (var exhibitId in args.Exhibits)
                {
                    var exhibitRef = new ReferenceAdded(ResourceType.Route, ev.Id, ResourceType.Exhibit, exhibitId);
                    await _eventStore.AppendEventAsync(exhibitRef);
                }
            }

            if (args.Tags != null)
            {
                foreach (var tagId in args.Tags)
                {
                    var tagRef = new ReferenceAdded(ResourceType.Route, ev.Id, ResourceType.Tag, tagId);
                    await _eventStore.AppendEventAsync(tagRef);
                }
            }

            await _eventStore.AppendEventAsync(ev);
            return Ok(ev.Id);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutAsync(int id, RouteArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Media, id))
                NotFound();

            var ev = new RouteUpdated
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now,
            };

            await _eventStore.AppendEventAsync(ev);
            return StatusCode(204);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            if (!_entityIndex.Exists(ResourceType.Route, id))
                return NotFound();

            if (_referencesIndex.IsUsed(ResourceType.Route, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            var ev = new RouteDeleted { Id = id };
            await _eventStore.AppendEventAsync(ev);

            // remove references to image, audio, exhibits and tags
            foreach (var reference in _referencesIndex.ReferencesOf(ResourceType.Route, id))
            {
                var refRemoved = new ReferenceRemoved(ResourceType.Route, id, reference.Type, reference.Id);
                await _eventStore.AppendEventAsync(refRemoved);
            }

            return NoContent();
        }
    }
}
