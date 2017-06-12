using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel.Commands;
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
    public class ExhibitsController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;
        private readonly ReferencesIndex _referencesIndex;

        // DEBUG
        private readonly ILogger<ExhibitsController> _logger;

        public ExhibitsController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices, ILogger<ExhibitsController> logger)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _entityIndex = indices.OfType<EntityIndex>().First();
            _referencesIndex = indices.OfType<ReferencesIndex>().First();

            _logger = logger; // DEBUG
        }

        [HttpGet("ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetIds(ContentStatus? status)
        {
            return Ok(_entityIndex.AllIds(ResourceType.Exhibit, status ?? ContentStatus.Published));
        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<ExhibitResult>), 200)]
        [ProducesResponseType(400)]
        public IActionResult Get(ExhibitQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitQueryArgs();

            var query = _db.Database.GetCollection<Exhibit>(ResourceType.Exhibit.Name).AsQueryable();

            try
            {
                var routeIds = args.OnlyRoutes?.Select(id => (BsonValue)id).ToList();

                // DEBUG
                if (args.Timestamp != null)
                {
                    var times = query.Select(x => x.Timestamp).ToList();
                    _logger.LogWarning($"FilterByTimestamp: Request timestamp is '{args.Timestamp.Value}', item timestamps are '{string.Join("; ", times)}'");

                    foreach (var time in times)
                        _logger.LogWarning($"FilterByTimestamp: item({time}) > requested({args.Timestamp.Value}) == {time > args.Timestamp.Value}");
                }

                var exhibits = query
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByStatus(args.Status)
                    .FilterByTimestamp(args.Timestamp)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Name.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .FilterIf(args.OnlyRoutes != null, x => x.Referencees
                        .Any(r => r.Collection == ResourceType.Route.Name && routeIds.Contains(r.Id)))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("name", x => x.Name),
                        ("timestamp", x => x.Timestamp))
                    .PaginateAndSelect(args.Page, args.PageSize, x => new ExhibitResult(x));

                return Ok(exhibits);
            }
            catch (InvalidSortKeyException e)
            {
                ModelState.AddModelError(nameof(args.OrderBy), e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExhibitResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exhibit = _db.Database.GetCollection<Exhibit>(ResourceType.Exhibit.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == id);

            if (exhibit == null)
                return NotFound();

            if (timestamp != null && exhibit.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new ExhibitResult(exhibit);
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostAsync([FromBody]ExhibitArgs args)
        {
            ValidateExhibitArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // validation passed, emit events (create exhibit, add references to image and tags)
            var ev = new ExhibitCreated
            {
                Id = _entityIndex.NextId(ResourceType.Exhibit),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            await AddExhibitReferencesAsync(args, ev.Id);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/{ev.Id}", ev.Id);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutAsync(int id, [FromBody]ExhibitArgs args)
        {
            ValidateExhibitArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound();

            // validation passed, emit events (remove old references, update exhibit, add new references)
            var ev = new ExhibitUpdated
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await RemoveExhibitReferencesAsync(ev.Id);
            await _eventStore.AppendEventAsync(ev);
            await AddExhibitReferencesAsync(args, ev.Id);
            return StatusCode(204);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound();

            // Check if exhibit is in use and can't be deleted (it's in use if and only if it is contained in a route).
            // We can't use _referencesIndex.IsUsed(...) here as it would return true as soon as the exhibit has pages
            // (since pages have a reference to their containing exhibit)
            if (_referencesIndex.ReferencesTo(ResourceType.Exhibit, id).Any(r => r.Type == ResourceType.Route))
                return BadRequest(ErrorMessages.ResourceInUse);

            // pages should be deleted along with the exhibit (cascading deletion) => first, remove the pages
            var pageIds = _referencesIndex.ReferencesTo(ResourceType.Exhibit, id)
                .Where(reference => reference.Type.Name == ResourceType.ExhibitPage.Name)
                .Select(reference => reference.Id)
                .ToList();

            foreach (var pageId in pageIds)
            {
                if (_referencesIndex.IsUsed(ResourceType.ExhibitPage, pageId))
                    return BadRequest("The exhibit cannot be deleted because it contains pages that are referenced by other resources");

                var pageDeleteEvents = ExhibitPageCommands.Delete(pageId, _referencesIndex);
                await _eventStore.AppendEventsAsync(pageDeleteEvents);
            }

            // now remove the actual exhibit
            var ev = new ExhibitDeleted { Id = id };
            await _eventStore.AppendEventAsync(ev);
            await RemoveExhibitReferencesAsync(id);

            return NoContent();
        }


        private void ValidateExhibitArgs(ExhibitArgs args)
        {
            if (args == null)
                return;

            // ensure referenced image exists and is published
            if (args.Image != null && !_mediaIndex.IsPublishedImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));

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

        private async Task AddExhibitReferencesAsync(ExhibitArgs args, int exhibitId)
        {
            if (args.Image != null)
            {
                var imageRef = new ReferenceAdded(ResourceType.Exhibit, exhibitId, ResourceType.Media, args.Image.Value);
                await _eventStore.AppendEventAsync(imageRef);
            }

            foreach (var tagId in args.Tags ?? Enumerable.Empty<int>())
            {
                var tagRef = new ReferenceAdded(ResourceType.Exhibit, exhibitId, ResourceType.Tag, tagId);
                await _eventStore.AppendEventAsync(tagRef);
            }
        }

        private async Task RemoveExhibitReferencesAsync(int exhibitId)
        {
            foreach (var reference in _referencesIndex.ReferencesOf(ResourceType.Exhibit, exhibitId))
            {
                var refRemoved = new ReferenceRemoved(ResourceType.Exhibit, exhibitId, reference.Type, reference.Id);
                await _eventStore.AppendEventAsync(refRemoved);
            }
        }
    }
}
