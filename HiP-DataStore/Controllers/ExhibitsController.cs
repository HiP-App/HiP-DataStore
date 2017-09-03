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
using Microsoft.AspNetCore.Authorization;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ExhibitsController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;
        private readonly ReferencesIndex _referencesIndex;
        private readonly RatingIndex _ratingIndex;

        public ExhibitsController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _entityIndex = indices.OfType<EntityIndex>().First();
            _referencesIndex = indices.OfType<ReferencesIndex>().First();
            _ratingIndex = indices.OfType<RatingIndex>().First();
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

                var exhibits = query
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByStatus(args.Status)
                    .FilterByTimestamp(args.Timestamp)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Name.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .FilterIf(args.OnlyRoutes != null, x => x.Referencers
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
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostAsync([FromBody]ExhibitArgs args)
        {
            ValidateExhibitArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToCreate(User.Identity, args.Status))
                return Forbid();

            // validation passed, emit events (create exhibit, add references to image and tags)
            var ev = new ExhibitCreated
            {
                Id = _entityIndex.NextId(ResourceType.Exhibit),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            using (var transaction = _eventStore.BeginTransaction())
            {
                transaction.Append(ev);
                transaction.Append(AddExhibitReferences(args, ev.Id));
                await transaction.CommitAsync();
            }

            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/{ev.Id}", ev.Id);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutAsync(int id, [FromBody]ExhibitArgs args)
        {
            ValidateExhibitArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound();

            ///TO DO Check the owner of the item (last parameter)
            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, true))
                return Forbid();

            // validation passed, emit events (remove old references, update exhibit, add new references)
            var ev = new ExhibitUpdated
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            using (var transaction = _eventStore.BeginTransaction())
            {
                transaction.Append(RemoveExhibitReferences(ev.Id));
                transaction.Append(ev);
                transaction.Append(AddExhibitReferences(args, ev.Id));
                await transaction.CommitAsync();
            }

            return StatusCode(204);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound();

            ///TO DO Check the owner of the item (last parameter)
            if (!UserPermissions.IsAllowedToDelete(User.Identity, _entityIndex.Status(ResourceType.Exhibit, id).GetValueOrDefault(), false))
                return Forbid();

            // check if exhibit is in use and can't be deleted (it's in use if and only if it is contained in a route).
            if (_referencesIndex.IsUsed(ResourceType.Exhibit, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            // remove the exhibit
            var ev = new ExhibitDeleted { Id = id };

            using (var transaction = _eventStore.BeginTransaction())
            {
                transaction.Append(ev);
                transaction.Append(RemoveExhibitReferences(id));
                await transaction.CommitAsync();
            }

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

            return ReferenceInfoHelper.GetReferenceInfo(ResourceType.Exhibit, id, _entityIndex, _referencesIndex);
        }

        [HttpGet("Rating/{id}")]
        [ProducesResponseType(typeof(RatingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetRating(int id) {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound(ErrorMessages.ExhibitNotFound(id));

            var result = new RatingResult()
            {
                Id = id,
                Average = _ratingIndex.Average(ResourceType.Exhibit, id),
                Count = _ratingIndex.Count(ResourceType.Exhibit, id)
            };

            return Ok(result);
        }

        [HttpPost("Rating/{id}")]
        [ProducesResponseType(typeof(int),201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostRatingAsync(int id,RatingArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound(ErrorMessages.ExhibitNotFound(id));

            // TODO When AUTH service will work change the UserID
            var ev = new RatingAdded()
            {
                Id = _ratingIndex.NextId(ResourceType.Exhibit),
                EntityId = id,
                UserId = args.UserId.GetValueOrDefault(),
                Value = args.Rating.GetValueOrDefault(),
                RatedType = ResourceType.Exhibit,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Rating/{ev.Id}", ev.Id);
        }

        private void ValidateExhibitArgs(ExhibitArgs args)
        {
            if (args == null)
                return;

            // ensure referenced image exists
            if (args.Image != null && !_mediaIndex.IsImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFound(args.Image.Value));

            // ensure referenced tags exist
            if (args.Tags != null)
            {
                var invalidIds = args.Tags
                    .Where(id => !_entityIndex.Exists(ResourceType.Tag, id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Tags),
                        ErrorMessages.TagNotFound(id));
            }
        }
        
        private IEnumerable<IEvent> AddExhibitReferences(ExhibitArgs args, int exhibitId)
        {
            if (args.Image != null)
                yield return new ReferenceAdded(ResourceType.Exhibit, exhibitId, ResourceType.Media, args.Image.Value);

            foreach (var pageId in args.Pages?.Distinct() ?? Enumerable.Empty<int>())
                yield return new ReferenceAdded(ResourceType.Exhibit, exhibitId, ResourceType.ExhibitPage, pageId);

            foreach (var tagId in args.Tags?.Distinct() ?? Enumerable.Empty<int>())
                yield return new ReferenceAdded(ResourceType.Exhibit, exhibitId, ResourceType.Tag, tagId);
        }
        
        private IEnumerable<IEvent> RemoveExhibitReferences(int exhibitId)
        {
            foreach (var reference in _referencesIndex.ReferencesOf(ResourceType.Exhibit, exhibitId))
                yield return new ReferenceRemoved(ResourceType.Exhibit, exhibitId, reference.Type, reference.Id);
        }
    }
}
