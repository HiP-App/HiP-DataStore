using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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
    public class ExhibitsController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;
        private readonly ReferencesIndex _referencesIndex;

        public ExhibitsController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
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
            return Ok(_entityIndex.AllIds(ResourceType.Exhibit, status ?? ContentStatus.Published));
        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<ExhibitResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public IActionResult Get(ExhibitQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitQueryArgs();

            var query = _db.Database.GetCollection<Exhibit>(ResourceType.Exhibit.Name).AsQueryable();

            try
            {
                var routeIds = args.OnlyRoute?.Select(id => (BsonValue)id).ToList();

                // TODO: What to do with timestamp?
                var exhibits = query
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByStatus(args.Status)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Name.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .FilterIf(args.OnlyRoute != null, x => x.Referencees
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
                return StatusCode(422, e.Message);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExhibitResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
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
        [ProducesResponseType(422)]
        public async Task<IActionResult> PostAsync([FromBody]ExhibitArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!IsExhibitArgsValid(args, out var validationError))
                return validationError;

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
        [ProducesResponseType(422)]
        public async Task<IActionResult> PutAsync(int id, [FromBody]ExhibitArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!IsExhibitArgsValid(args, out var validationError))
                return validationError;

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
            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound();

            if (_referencesIndex.IsUsed(ResourceType.Exhibit, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            var ev = new ExhibitDeleted { Id = id };
            await _eventStore.AppendEventAsync(ev);

            // remove references to image and tags
            foreach (var reference in _referencesIndex.ReferencesOf(ResourceType.Exhibit, id))
            {
                var refRemoved = new ReferenceRemoved(ResourceType.Exhibit, id, reference.Type, reference.Id);
                await _eventStore.AppendEventAsync(refRemoved);
            }

            return NoContent();
        }


        private bool IsExhibitArgsValid(ExhibitArgs args, out IActionResult response)
        {
            // ensure referenced image exists and is published
            if (args.Image != null && !_mediaIndex.IsPublishedImage(args.Image.Value))
            {
                response = StatusCode(422, ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));
                return false;
            }

            // ensure referenced tags exist and are published
            if (args.Tags != null)
            {
                var invalidIds = args.Tags
                    .Where(id => _entityIndex.Status(ResourceType.Tag, id) != ContentStatus.Published)
                    .ToList();

                if (invalidIds.Count > 0)
                {
                    response = StatusCode(422, ErrorMessages.TagNotFoundOrNotPublished(invalidIds[0]));
                    return false;
                }
            }

            response = null;
            return true;
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
