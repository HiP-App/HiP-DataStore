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

        [HttpGet("ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetIds(ContentStatus? status)
        {
            return Ok(_entityIndex.AllIds(ResourceType.Route, status ?? ContentStatus.Published));
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
                    .PaginateAndSelect(args.Page, args.PageSize, x => new RouteResult(x));

                return Ok(routes);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RouteResult), 200)]
        [ProducesResponseType(304)]
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

            var result = new RouteResult(route);
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PostAsync([FromBody]RouteArgs args)
        {
            ValidateRouteArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // validation passed, emit events (create route, add references to image, audio, exhibits and tags)
            var ev = new RouteCreated
            {
                Id = _entityIndex.NextId(ResourceType.Route),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            await AddRouteReferencesAsync(args, ev.Id);
            return Created($"{Request.Scheme}://{Request.Host}/api/Routes/{ev.Id}", ev.Id);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PutAsync(int id, RouteArgs args)
        {
            ValidateRouteArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Route, id))
                return NotFound();

            // validation passed, emit events (remove old references, update route, add new references)
            var ev = new RouteUpdated
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now,
            };

            await RemoveRouteReferencesAsync(ev.Id);
            await _eventStore.AppendEventAsync(ev);
            await AddRouteReferencesAsync(args, ev.Id);
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

            // validation passed, emit events (delete route, remove references to image, audio, exhibits and tags)
            var ev = new RouteDeleted { Id = id };
            await _eventStore.AppendEventAsync(ev);
            await RemoveRouteReferencesAsync(id);
            return NoContent();
        }


        private void ValidateRouteArgs(RouteArgs args)
        {
            if (args == null)
                return;

            // ensure referenced image exists and is published
            if (args.Image != null && !_mediaIndex.IsPublishedImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));

            // ensure referenced audio exists and is published
            if (args.Audio != null && !_mediaIndex.IsPublishedAudio(args.Audio.Value))
                ModelState.AddModelError(nameof(args.Audio),
                    ErrorMessages.AudioNotFoundOrNotPublished(args.Audio.Value));

            // ensure referenced exhibits exist and are published
            if (args.Exhibits != null)
            {
                var invalidIds = args.Exhibits
                    .Where(id => _entityIndex.Status(ResourceType.Exhibit, id) != ContentStatus.Published)
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Exhibits),
                        ErrorMessages.ExhibitNotFoundOrNotPublished(id));
            }

            // ensure referenced tags exist and are published
            if (args.Tags != null)
            {
                var invalidIds = args.Tags
                    .Where(id => _entityIndex.Status(ResourceType.Tag, id) != ContentStatus.Published)
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Tags),
                        ErrorMessages.TagNotFoundOrNotPublished(id));
            }
        }

        private async Task AddRouteReferencesAsync(RouteArgs args, int routeId)
        {
            if (args.Image != null)
            {
                var imageRef = new ReferenceAdded(ResourceType.Route, routeId, ResourceType.Media, args.Image.Value);
                await _eventStore.AppendEventAsync(imageRef);
            }

            if (args.Audio != null)
            {
                var audioRef = new ReferenceAdded(ResourceType.Route, routeId, ResourceType.Media, args.Audio.Value);
                await _eventStore.AppendEventAsync(audioRef);
            }

            foreach (var exhibitId in args.Exhibits ?? Enumerable.Empty<int>())
            {
                var exhibitRef = new ReferenceAdded(ResourceType.Route, routeId, ResourceType.Exhibit, exhibitId);
                await _eventStore.AppendEventAsync(exhibitRef);
            }

            foreach (var tagId in args.Tags ?? Enumerable.Empty<int>())
            {
                var tagRef = new ReferenceAdded(ResourceType.Route, routeId, ResourceType.Tag, tagId);
                await _eventStore.AppendEventAsync(tagRef);
            }
        }

        private async Task RemoveRouteReferencesAsync(int routeId)
        {
            foreach (var reference in _referencesIndex.ReferencesOf(ResourceType.Route, routeId))
            {
                var refRemoved = new ReferenceRemoved(ResourceType.Route, routeId, reference.Type, reference.Id);
                await _eventStore.AppendEventAsync(refRemoved);
            }
        }
    }
}
