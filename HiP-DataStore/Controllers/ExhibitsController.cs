using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ExhibitsController : Controller
    {
        private readonly EventStoreService _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;
        private readonly ReferencesIndex _referencesIndex;
        private readonly RatingIndex _ratingIndex;
        private readonly ReviewIndex _reviewIndex;

        public ExhibitsController(EventStoreService eventStore, CacheDatabaseManager db, InMemoryCache cache)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = cache.Index<MediaIndex>();
            _entityIndex = cache.Index<EntityIndex>();
            _referencesIndex = cache.Index<ReferencesIndex>();
            _ratingIndex = cache.Index<RatingIndex>();
            _reviewIndex = cache.Index<ReviewIndex>();
        }

        [HttpGet("ids")]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetIds(ContentStatus status = ContentStatus.Published)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            return Ok(_entityIndex.AllIds(ResourceType.Exhibit, status , User.Identity));
        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<ExhibitResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public IActionResult Get([FromQuery]ExhibitQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitQueryArgs();

            if (args.Status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            var query = _db.Database.GetCollection<Exhibit>(ResourceType.Exhibit.Name).AsQueryable();

            try
            {
                var routeIds = args.OnlyRoutes?.Select(id => (BsonValue)id).ToList();

                var exhibits = query
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByLocation(args.Latitude, args.Longitude)
                    .FilterByUser(args.Status,User.Identity)
                    .FilterByStatus(args.Status, User.Identity)
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
                    .PaginateAndSelect(args.Page, args.PageSize, x => new ExhibitResult(x)
                    {
                        Timestamp = _referencesIndex.LastModificationCascading(ResourceType.Exhibit, x.Id)
                    });

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
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceType.Exhibit, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceType.Exhibit, id)))
                return Forbid();

            var exhibit = _db.Database.GetCollection<Exhibit>(ResourceType.Exhibit.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == id);

            if (exhibit == null)
                return NotFound();

            if (timestamp != null && exhibit.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new ExhibitResult(exhibit)
            {
                Timestamp = _referencesIndex.LastModificationCascading(ResourceType.Exhibit, id)
            };

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> PostAsync([FromBody]ExhibitArgs args)
        {
            ValidateExhibitArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToCreate(User.Identity, args.Status))
                return Forbid();

            // validation passed, emit event
            var ev = new ExhibitCreated
            {
                Id = _entityIndex.NextId(ResourceType.Exhibit),
                UserId = User.Identity.GetUserIdentity(),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
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

            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceType.Exhibit, id)))
                return Forbid();

            var oldStatus = _entityIndex.Status(ResourceType.Exhibit, id).GetValueOrDefault();
            if (args.Status == ContentStatus.Unpublished && oldStatus != ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeUnpublished(ResourceType.Exhibit));

            // validation passed, emit event
            var ev = new ExhibitUpdated
            {
                Id = id,
                UserId = User.Identity.GetUserIdentity(),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
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

            var status = _entityIndex.Status(ResourceType.Exhibit, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceType.Exhibit, id)))
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceType.Exhibit,id));

            if (status == ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceType.Exhibit, id));

            // check if exhibit is in use and can't be deleted (it's in use if and only if it is contained in a route).
            if (_referencesIndex.IsUsed(ResourceType.Exhibit, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            // remove the exhibit
            var ev = new ExhibitDeleted
            {
                Id = id,
                UserId = User.Identity.GetUserIdentity(),
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return NoContent();
        }

        [HttpGet("{id}/Refs")]
        [ProducesResponseType(typeof(ReferenceInfoResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetReferenceInfo(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToGet(User.Identity, _entityIndex.Owner(ResourceType.Exhibit, id)))
                return Forbid();

            return ReferenceInfoHelper.GetReferenceInfo(ResourceType.Exhibit, id, _entityIndex, _referencesIndex);
        }

        [HttpGet("Rating/{id}")]
        [ProducesResponseType(typeof(RatingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetRating(int id)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceType.Exhibit,id));

            var result = new RatingResult()
            {
                Id = id,
                Average = _ratingIndex.Average(ResourceType.Exhibit, id),
                Count = _ratingIndex.Count(ResourceType.Exhibit, id),
                RatingTable = _ratingIndex.Table(ResourceType.Exhibit, id)
            };

            return Ok(result);
        }

        [HttpPost("Rating/{id}")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostRatingAsync(int id, RatingArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceType.Exhibit, id));

            var ev = new RatingAdded()
            {
                Id = _ratingIndex.NextId(ResourceType.Exhibit),
                EntityId = id,
                UserId = User.Identity.GetUserIdentity(),
                Value = args.Rating.GetValueOrDefault(),
                RatedType = ResourceType.Exhibit,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Rating/{ev.Id}", ev.Id);
        }

        [HttpGet("Review/{id}")]
        [ProducesResponseType(typeof(ReviewResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetRreview(int id)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceType.Exhibit, id));

            if (!_reviewIndex.Exists(ResourceType.Exhibit, id))
                return NotFound(ErrorMessages.ReviewNotFound(ResourceType.Exhibit, id));

            if (!_reviewIndex.ReviewableByStudents(ResourceType.Exhibit, id) && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                return Forbid();

            var result = new ReviewResult()
            {
                Id = id,
                Description = _reviewIndex.Description(ResourceType.Exhibit, id),
                Approved = _reviewIndex.Approved(ResourceType.Exhibit, id),
                Reviewers = _reviewIndex.Reviewers(ResourceType.Exhibit, id),
                Comments = _reviewIndex.Comments(ResourceType.Exhibit, id),
                Timestamp = _reviewIndex.Timestamp(ResourceType.Exhibit, id)
            };

            return Ok(result);
        }

        [HttpPost("Review/{id}")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostReviewAsync(int id, ReviewArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceType.Exhibit, id));

            if (!_entityIndex.Status(ResourceType.Exhibit, id).Equals(ContentStatus.In_Review))
                return BadRequest(ErrorMessages.CannotAddReviewToContentWithWrongStatus());

            if (_reviewIndex.Exists(ResourceType.Exhibit, id))
                return BadRequest(ErrorMessages.ContentAlreadyHasReview(ResourceType.Exhibit, id));

            if (!UserPermissions.IsAllowedToCreateReview(User.Identity, _reviewIndex.Owner(ResourceType.Exhibit, id)))
                return Forbid();

            var ev = new ReviewCreated()
            {
                Id = _reviewIndex.NextId(ResourceType.Exhibit),
                EntityId = id,
                StudentsToApprove = args.StudentsToApprove,
                ReviewableByStudents = args.ReviewableByStudents,
                Description = args.Description,
                Reviewers = args.Reviewers,
                UserId = User.Identity.GetUserIdentity(),
                ReviewType = ResourceType.Exhibit,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Review/{ev.Id}", ev.Id);
        }

        [HttpPut("Review/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutReviewAsync(int id, ReviewUpdateArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceType.Exhibit, id));

            if (!_reviewIndex.Exists(ResourceType.Exhibit, id))
                return NotFound(ErrorMessages.ReviewNotFound(ResourceType.Exhibit, id));

            if (!_reviewIndex.ReviewableByStudents(ResourceType.Exhibit, id) && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                return Forbid();

            // Only reviewers, owner and admins/supervisors are allowed to comment
            if (!UserPermissions.IsAllowedToCommentReview(User.Identity, args.Reviewers, 
                _reviewIndex.Owner(ResourceType.Exhibit, id)))
                return Forbid();

            if (args.StudentsToApprove != _reviewIndex.StudentsToApprove(ResourceType.Exhibit, id)
                && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                return Forbid();

            if (_reviewIndex.ReviewableByStudents(ResourceType.Exhibit, id) && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                args.ReviewableByStudents = true;

            var ev = new ReviewUpdated()
            {
                Approved = _reviewIndex.ReviewApproved(ResourceType.Exhibit, id, User.Identity, args.Approved),
                ApprovedComment = args.Approved,
                Comment = args.Comment,
                StudentsToApprove = args.StudentsToApprove,
                ReviewableByStudents = args.ReviewableByStudents,
                EntityId = id,
                Reviewers = args.Reviewers,
                UserId = User.Identity.GetUserIdentity(),
                ReviewType = ResourceType.Exhibit,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return StatusCode(204);
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
                        ErrorMessages.ContentNotFound(ResourceType.Tag,id));
            }

            // ensure referenced pages exist
            if (args.Pages != null)
            {
                var invalidIds = args.Pages
                    .Where(id => !_entityIndex.Exists(ResourceType.ExhibitPage, id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Pages),
                        ErrorMessages.ExhibitPageNotFound(id));
            }
        }
    }
}
