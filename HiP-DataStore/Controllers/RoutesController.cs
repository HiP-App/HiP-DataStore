using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using static PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Review;
using static PaderbornUniversity.SILab.Hip.DataStore.Model.Rest.ReviewResult;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class RoutesController : Controller
    {
        private readonly EventStoreService _eventStore;
        private readonly IMongoDbContext _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;
        private readonly ReferencesIndex _referencesIndex;
        private readonly RatingIndex _ratingIndex;
        private readonly ReviewIndex _reviewIndex;

        public RoutesController(EventStoreService eventStore, IMongoDbContext db, IEnumerable<IDomainIndex> indices)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _entityIndex = indices.OfType<EntityIndex>().First();
            _referencesIndex = indices.OfType<ReferencesIndex>().First();
            _ratingIndex = indices.OfType<RatingIndex>().First();
            _reviewIndex = indices.OfType<ReviewIndex>().First();
        }

        [HttpGet("ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public IActionResult GetIds(ContentStatus status = ContentStatus.Published)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            return Ok(_entityIndex.AllIds(ResourceTypes.Route, status, User.Identity));
        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<RouteResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public IActionResult Get([FromQuery]RouteQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new RouteQueryArgs();

            if (args.Status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            try
            {
                var routes = _db
                    .GetCollection<Route>(ResourceTypes.Route)
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByUser(args.Status, User.Identity)
                    .FilterByStatus(args.Status, User.Identity)
                    .FilterByTimestamp(args.Timestamp)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .PaginateAndSelect(args.Page, args.PageSize, x => new RouteResult(x)
                    {
                        Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.Route, x.Id)
                    });

                return Ok(routes);
            }
            catch (InvalidSortKeyException e)
            {
                ModelState.AddModelError(nameof(args.OrderBy), e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RouteResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceTypes.Route, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceTypes.Route, id)))
                return Forbid();

            var route = _db.Get<Route>((ResourceTypes.Route, id));

            if (route == null)
                return NotFound();

            // Route instance wasn`t modified after timestamp
            if (timestamp != null && route.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new RouteResult(route)
            {
                Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.Route, id)
            };

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostAsync([FromBody]RouteArgs args)
        {
            ValidateRouteArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToCreate(User.Identity, args.Status))
                return Forbid();

            // validation passed, emit event
            var id = _entityIndex.NextId(ResourceTypes.Route);
            await EntityManager.CreateEntityAsync(_eventStore, args, ResourceTypes.Route, id, User.Identity.GetUserIdentity());

            return Created($"{Request.Scheme}://{Request.Host}/api/Routes/{id}", id);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutAsync(int id, [FromBody]RouteArgs args)
        {
            ValidateRouteArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Route, id))
                return NotFound();

            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceTypes.Route, id)))
                return Forbid();

            var oldStatus = _entityIndex.Status(ResourceTypes.Route, id).GetValueOrDefault();
            if (args.Status == ContentStatus.Unpublished && oldStatus != ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeUnpublished(ResourceTypes.Route));

            // validation passed, emit event
            var oldArgs = await EventStreamExtensions.GetCurrentEntityAsync<RouteArgs>(_eventStore.EventStream, ResourceTypes.Route, _entityIndex.NextId(ResourceTypes.Route));
            await EntityManager.UpdateEntityAsync(_eventStore, oldArgs, args, ResourceTypes.Route, id, User.Identity.GetUserIdentity());
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

            if (!_entityIndex.Exists(ResourceTypes.Route, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceTypes.Route, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceTypes.Route, id)))
                return Forbid();

            if (status == ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.Route, id));

            if (_referencesIndex.IsUsed(ResourceTypes.Route, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            // validation passed, emit event
            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.Route, id, User.Identity.GetUserIdentity());
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

            if (!UserPermissions.IsAllowedToGet(User.Identity, _entityIndex.Owner(ResourceTypes.Route, id)))
                return Forbid();

            return ReferenceInfoHelper.GetReferenceInfo(ResourceTypes.Route, id, _entityIndex, _referencesIndex);
        }

        [HttpGet("Rating/{id}")]
        [ProducesResponseType(typeof(RatingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetRating(int id)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Route, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Route, id));

            var result = new RatingResult()
            {
                Id = id,
                Average = _ratingIndex.Average(ResourceTypes.Route, id),
                Count = _ratingIndex.Count(ResourceTypes.Route, id),
                RatingTable = _ratingIndex.Table(ResourceTypes.Route, id)
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

            if (!_entityIndex.Exists(ResourceTypes.Route, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Route, id));

            if (User.Identity.GetUserIdentity() == null)
                return Unauthorized();          

            var ev = new RatingAdded()
            {
                Id = _ratingIndex.NextId(ResourceTypes.Route),
                EntityId = id,
                UserId = User.Identity.GetUserIdentity(),
                Value = args.Rating.GetValueOrDefault(),
                RatedType = ResourceTypes.Route,
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

            if (!_entityIndex.Exists(ResourceTypes.Route, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Route, id));

            if (!_reviewIndex.Exists(ResourceTypes.Route.Name, id))
                return NotFound(ErrorMessages.ReviewNotFound(ResourceTypes.Route, id));

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.Route.Name, id);
            var review = _db.Get<Review>((ResourceTypes.Review, reviewId));

            if (!review.ReviewableByStudents && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                return Forbid();

            var result = new ReviewResult(review);

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

            if (!_entityIndex.Exists(ResourceTypes.Route, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Route, id));

            if (!_entityIndex.Status(ResourceTypes.Route, id).Equals(ContentStatus.In_Review))
                return BadRequest(ErrorMessages.CannotAddReviewToContentWithWrongStatus());

            if (_reviewIndex.Exists(ResourceTypes.Route.Name, id))
                return BadRequest(ErrorMessages.ContentAlreadyHasReview(ResourceTypes.Route, id));

            if (!UserPermissions.IsAllowedToCreateReview(User.Identity, _entityIndex.Owner(ResourceTypes.Route, id)))
                return Forbid();

            var newReview = new Review(args)
            {
                EntityId = id,
                EntityType = ResourceTypes.Route.Name
            };

            var reviewId = _reviewIndex.NextId(ResourceTypes.Route);

            await EntityManager.CreateEntityAsync(_eventStore, newReview, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());

            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Review/{reviewId}", reviewId);
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

            if (!_entityIndex.Exists(ResourceTypes.Route, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Route, id));

            if (!_reviewIndex.Exists(ResourceTypes.Route.Name, id))
                return NotFound(ErrorMessages.ReviewNotFound(ResourceTypes.Route, id));

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.Route.Name, id);
            var review = _db.Get<Review>((ResourceTypes.Review, reviewId));

            if (!review.ReviewableByStudents && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                return Forbid();
            // Only reviewers, owner and admins/supervisors are allowed to comment
            if (!UserPermissions.IsAllowedToCommentReview(User.Identity, review.Reviewers, _reviewIndex.Owner(reviewId)))
                return Forbid();
            // Only admins and supervisors are permitted to change the amount of students that are necessary to approve a review
            if (args.StudentsToApprove != review.StudentsToApprove && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                args.StudentsToApprove = review.StudentsToApprove;
            // Only admins and supervisors are permitted to change whether students are allowed to approve a review
            if (args.ReviewableByStudents != review.ReviewableByStudents && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                args.ReviewableByStudents = review.ReviewableByStudents;

            var updatedReview = new Review(args)
            {
                EntityId = review.EntityId,
                EntityType = review.EntityType,
                Comments = review.Comments
            };

            review.Comments = null;
            if (args.Comment != null)
                updatedReview.Comments.Add(new Comment(args.Comment, DateTimeOffset.Now, User.Identity.GetUserIdentity(), args.Approved));

            if (args.ReviewableByStudents == false)
                updatedReview.StudentsToApprove = 0;

            if (args.Approved)
                updatedReview.Approved = IsReviewApproved(updatedReview, args.Approved, User.Identity);

            // keep old values if no new ones are specified
            if (args.Description == null)
                updatedReview.Description = review.Description;
            if (args.ReviewableByStudents == null)
                updatedReview.ReviewableByStudents = review.ReviewableByStudents;
            if (args.StudentsToApprove == null)
                updatedReview.StudentsToApprove = review.StudentsToApprove;
            if (args.Reviewers == null)
                updatedReview.Reviewers = review.Reviewers;

            await EntityManager.UpdateEntityAsync(_eventStore, review, updatedReview, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());
            return StatusCode(204);
        }

        [HttpDelete("Review/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteReviewAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // only supervisors or admins are allowed to delete reviews
            if (!UserPermissions.IsSupervisorOrAdmin(User.Identity))
                return Forbid();

            if (!_entityIndex.Exists(ResourceTypes.Route, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Route, id));

            if (!_reviewIndex.Exists(ResourceTypes.Route.Name, id))
                return NotFound(ErrorMessages.ReviewNotFound(ResourceTypes.Route, id));

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.Route.Name, id);
            var review = _db.Get<Review>((ResourceTypes.Review, reviewId));

            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());
            return NoContent();
        }

        private bool IsReviewApproved(Review updatedReview, bool approved, IIdentity identity)
        {
            if (UserPermissions.IsSupervisorOrAdmin(User.Identity))
            {
                return approved;
            }
            else
            {
                var numberOfApproves = 0;
                foreach (Comment comment in updatedReview.Comments)
                {
                    if (comment.Approved)
                        numberOfApproves++;
                }
                return numberOfApproves >= updatedReview.StudentsToApprove ? true : false;
            }
        }

        private void ValidateRouteArgs(RouteArgs args)
        {
            if (args == null)
                return;

            // ensure referenced image exists
            if (args.Image != null && !_mediaIndex.IsImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFound(args.Image.Value));

            // ensure referenced audio exists
            if (args.Audio != null && !_mediaIndex.IsAudio(args.Audio.Value))
                ModelState.AddModelError(nameof(args.Audio),
                    ErrorMessages.AudioNotFound(args.Audio.Value));

            // ensure referenced exhibits exist
            if (args.Exhibits != null)
            {
                var invalidIds = args.Exhibits
                    .Where(id => !_entityIndex.Exists(ResourceTypes.Exhibit, id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Exhibits),
                        ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, id));
            }

            // ensure referenced tags exist
            if (args.Tags != null)
            {
                var invalidIds = args.Tags
                    .Where(id => !_entityIndex.Exists(ResourceTypes.Tag, id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Tags),
                        ErrorMessages.ContentNotFound(ResourceTypes.Tag, id));
            }
        }
    }
}
