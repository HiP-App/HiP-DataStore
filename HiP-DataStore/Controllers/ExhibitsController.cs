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
    /// <summary>
    /// Controller for testing purposes.
    /// </summary>
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

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ExhibitResult>), 200)]
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
                var exhibits = query
                    .FilterByIds(args.ExcludedIds, args.IncludedIds)
                    .FilterByStatus(args.Status)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Name.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .FilterIf(args.RouteIds != null,
                        x => true) // TODO: Filter by route
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("name", x => x.Name),
                        ("timestamp", x => x.Timestamp))
                    .Paginate(args.Page, args.PageSize)
                    .ToList();

                // TODO: What to do with timestamp?
                var results = exhibits.Select(x => new ExhibitResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Image = (int?)x.Image.Id,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Used = x.Referencees.Count > 0,
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
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PostAsync([FromBody]ExhibitArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ensure referenced image exists and is published
            if (args.Image != null && !_mediaIndex.IsPublishedImage(args.Image.Value))
                return StatusCode(422, ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));

            // ensure referenced tags exist and are published
            if (args.Tags != null)
            {
                var invalidIds = args.Tags
                    .Where(id => _entityIndex.Status(ResourceType.Tag, id) != ContentStatus.Published)
                    .ToList();

                if (invalidIds.Count > 0)
                    return StatusCode(422, ErrorMessages.TagNotFoundOrNotPublished(invalidIds[0]));
            }

            // validation passed, emit events (create exhibit, add references to image and tags)
            var ev = new ExhibitCreated
            {
                Id = _entityIndex.NextId(ResourceType.Exhibit),
                Properties = args
            };
            await _eventStore.AppendEventAsync(ev);

            if (args.Image != null)
            {
                var imageRef = new ReferenceAdded(ResourceType.Exhibit, ev.Id, ResourceType.Media, args.Image.Value);
                await _eventStore.AppendEventAsync(imageRef);
            }

            if (args.Tags != null)
            {
                foreach (var tagId in args.Tags)
                {
                    var tagRef = new ReferenceAdded(ResourceType.Exhibit, ev.Id, ResourceType.Tag, tagId);
                    await _eventStore.AppendEventAsync(tagRef);
                }
            }

            return Ok(ev.Id);
        }

        [HttpDelete]
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
            return NoContent();
        }
    }
}
