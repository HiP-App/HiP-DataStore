using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ExhibitsController : Controller
    {
        private readonly EventStoreService _eventStore;
        private readonly IMongoDbContext _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;
        private readonly ReferencesIndex _referencesIndex;
        private readonly RatingIndex _ratingIndex;
        private readonly QuizIndex _quizIndex;
        private readonly ReviewIndex _reviewIndex;

        public ExhibitsController(EventStoreService eventStore, IMongoDbContext db, InMemoryCache cache)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = cache.Index<MediaIndex>();
            _entityIndex = cache.Index<EntityIndex>();
            _referencesIndex = cache.Index<ReferencesIndex>();
            _ratingIndex = cache.Index<RatingIndex>();
            _quizIndex = cache.Index<QuizIndex>();
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

            return Ok(_entityIndex.AllIds(ResourceTypes.Exhibit, status, User.Identity));
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

            try
            {
                var result = FilterExhibitsByArgs(args);
                return Ok(result);
            }
            catch (InvalidSortKeyException e)
            {
                ModelState.AddModelError(nameof(args.OrderBy), e.Message);
                return BadRequest(ModelState);
            }
        }

        private AllItemsResult<ExhibitResult> FilterExhibitsByArgs(ExhibitQueryArgs args, bool onlyGetUserContent = false)
        {
            var routeIds = args.OnlyRoutes?.Select(id => (BsonValue)id).ToList();

            return _db
                .GetCollection<Exhibit>(ResourceTypes.Exhibit)
                .FilterByIds(args.Exclude, args.IncludeOnly)
                .FilterByLocation(args.Latitude, args.Longitude)
                .FilterByUser(args.Status, User.Identity)
                .FilterByStatus(args.Status, User.Identity)
                .FilterByTimestamp(args.Timestamp)
                .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                    x.Name.ToLower().Contains(args.Query.ToLower()) ||
                    x.Description.ToLower().Contains(args.Query.ToLower()))
                .FilterIf(args.OnlyRoutes != null, x => x.Referencers
                    .Any(r => r.Type == ResourceTypes.Route && routeIds.Contains(r.Id)))
                .Sort(args.OrderBy,
                    ("id", x => x.Id),
                    ("name", x => x.Name),
                    ("timestamp", x => x.Timestamp))
                 .FilterIf(onlyGetUserContent, ex => ex.UserId == User.Identity.GetUserIdentity())
                .PaginateAndSelect(args.Page, args.PageSize, x => new ExhibitResult(x)
                {
                    Quiz = _quizIndex.GetQuizId(x.Id),
                    Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.Exhibit, x.Id)
                });
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

            var status = _entityIndex.Status(ResourceTypes.Exhibit, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceTypes.Exhibit, id)))
                return Forbid();

            var exhibit = _db.Get<Exhibit>((ResourceTypes.Exhibit, id));

            if (exhibit == null)
                return NotFound();

            if (timestamp != null && exhibit.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new ExhibitResult(exhibit)
            {
                Quiz = _quizIndex.GetQuizId(id),
                Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.Exhibit, id)
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

            //// validation passed, emit event
            var id = _entityIndex.NextId(ResourceTypes.Exhibit);
            await EntityManager.CreateEntityAsync(_eventStore, args, ResourceTypes.Exhibit, id, User.Identity.GetUserIdentity());
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/{id}", id);
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

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, id))
                return NotFound();

            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceTypes.Exhibit, id)))
                return Forbid();

            var oldStatus = _entityIndex.Status(ResourceTypes.Exhibit, id).GetValueOrDefault();
            if (args.Status == ContentStatus.Unpublished && oldStatus != ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeUnpublished(ResourceTypes.Exhibit));

            // validation passed, emit event
            var oldExhibitArgs = await EventStreamExtensions.GetCurrentEntityAsync<ExhibitArgs>(_eventStore.EventStream, ResourceTypes.Exhibit, id);
            await EntityManager.UpdateEntityAsync(_eventStore, oldExhibitArgs, args, ResourceTypes.Exhibit, id, User.Identity.GetUserIdentity());

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

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceTypes.Exhibit, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceTypes.Exhibit, id)))
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.Exhibit, id));

            if (status == ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.Exhibit, id));

            // check if exhibit is in use and can't be deleted (it's in use if and only if it is contained in a route).
            if (_referencesIndex.IsUsed(ResourceTypes.Exhibit, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            // remove the exhibit
            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.Exhibit, id, User.Identity.GetUserIdentity());
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

            if (!UserPermissions.IsAllowedToGet(User.Identity, _entityIndex.Owner(ResourceTypes.Exhibit, id)))
                return Forbid();

            return ReferenceInfoHelper.GetReferenceInfo(ResourceTypes.Exhibit, id, _entityIndex, _referencesIndex);
        }

        /// <summary>
        /// Gets the exhibits where the current user is the owner
        /// </summary>
        [HttpGet("My")]
        [ProducesResponseType(typeof(AllItemsResult<ExhibitResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public IActionResult GetMyExhibits(ExhibitQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (args.Status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            try
            {
                var result = FilterExhibitsByArgs(args, onlyGetUserContent: true);
                return Ok(result);
            }
            catch (InvalidSortKeyException e)
            {
                ModelState.AddModelError(nameof(args.OrderBy), e.Message);
                return BadRequest(ModelState);
            }
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

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, id));

            var result = new RatingResult()
            {
                Id = id,
                Average = _ratingIndex.Average(ResourceTypes.Exhibit, id),
                Count = _ratingIndex.Count(ResourceTypes.Exhibit, id),
                RatingTable = _ratingIndex.Table(ResourceTypes.Exhibit, id)
            };

            return Ok(result);
        }

        /// <summary>
        /// Geting rating of the exhibit for the requested user
        /// </summary>
        /// <param name="id"> Id of the exhibit </param>
        /// <returns></returns>
        [HttpGet("Rating/My/{id}")]
        [ProducesResponseType(typeof(byte?), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetMyRating(int id)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, id));

            var result = _ratingIndex.UserRating(ResourceTypes.Exhibit, id, User.Identity);

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

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, id));

            if (User.Identity.GetUserIdentity() == null)
                return Unauthorized();

            var ev = new RatingAdded()
            {
                Id = _ratingIndex.NextId(ResourceTypes.Exhibit),
                EntityId = id,
                UserId = User.Identity.GetUserIdentity(),
                Value = args.Rating.GetValueOrDefault(),
                RatedType = ResourceTypes.Exhibit,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Rating/{ev.Id}", ev.Id);
        }

        [HttpGet("Quiz/{id}")]
        [ProducesResponseType(typeof(ExhibitQuizResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetQuizById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceTypes.Quiz, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceTypes.Quiz, id)))
                return Forbid();

            var quiz = _db.Get<Quiz>((ResourceTypes.Quiz, id));

            if (quiz == null)
                return NotFound();

            if (timestamp != null && quiz.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new ExhibitQuizResult(quiz)
            {
                Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.Quiz, id)
            };

            return Ok(result);
        }
        [HttpPost("Quiz")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostQuizAsync([FromBody]ExhibitQuizArgs args)
        {
            ValidateQuizArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int? oldQuiz = _quizIndex.GetQuizId(args.ExhibitId.GetValueOrDefault());
            if (oldQuiz != null)
                return BadRequest(ErrorMessages.QuizCannotBeCreated(oldQuiz.GetValueOrDefault(), args.ExhibitId.GetValueOrDefault()));

            if (User.Identity.GetUserIdentity() == null)
                return Unauthorized();

            var id = _entityIndex.NextId(ResourceTypes.Quiz);
            await EntityManager.CreateEntityAsync(_eventStore, args, ResourceTypes.Quiz, id, User.Identity.GetUserIdentity());
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Quiz/{id}", id);
        }

        [HttpPut("Quiz/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateQuizAsync(int id, [FromBody]ExhibitQuizArgs args)
        {
            ValidateQuizArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Quiz, id))
                return NotFound();

            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceTypes.Quiz, id)))
                return Forbid();

            var oldStatus = _entityIndex.Status(ResourceTypes.Quiz, id).GetValueOrDefault();
            if (args.Status == ContentStatus.Unpublished && oldStatus != ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeUnpublished(ResourceTypes.Quiz));

            // validation passed, emit event
            var oldQuizArgs = await EventStreamExtensions.GetCurrentEntityAsync<ExhibitQuizArgs>(_eventStore.EventStream, ResourceTypes.Quiz, id);
            await EntityManager.UpdateEntityAsync(_eventStore, oldQuizArgs, args, ResourceTypes.Quiz, id, User.Identity.GetUserIdentity());

            return StatusCode(204);
        }

        [HttpDelete("Quiz/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteQuizAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceTypes.Quiz, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceTypes.Quiz, id)))
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.Quiz, id));

            if (status == ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.Quiz, id));

            // remove the quiz
            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.Quiz, id, User.Identity.GetUserIdentity());
            return NoContent();
        }

        [HttpGet("Quiz/Rating/{id}")]
        [ProducesResponseType(typeof(RatingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetQuizRating(int id)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Quiz, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Quiz, id));

            var result = new RatingResult()
            {
                Id = id,
                Average = _ratingIndex.Average(ResourceTypes.Quiz, id),
                Count = _ratingIndex.Count(ResourceTypes.Quiz, id),
                RatingTable = _ratingIndex.Table(ResourceTypes.Quiz, id)
            };

            return Ok(result);
        }
        /// <summary>
        /// Geting rating of the quiz for the requested user
        /// </summary>
        /// <param name="id"> Id of the quiz </param>
        /// <returns></returns>
        [HttpGet("Quiz/Rating/My/{id}")]
        [ProducesResponseType(typeof(byte?), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetMyQuizRating(int id)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Quiz, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Quiz, id));

            var result = _ratingIndex.UserRating(ResourceTypes.Quiz, id, User.Identity);

            return Ok(result);
        }

        [HttpPost("Quiz/Rating/{id}")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostQuizRatingAsync(int id, RatingArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Quiz, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Quiz, id));

            if (User.Identity.GetUserIdentity() == null)
                return Unauthorized();

            var ev = new RatingAdded()
            {
                Id = _ratingIndex.NextId(ResourceTypes.Quiz),
                EntityId = id,
                UserId = User.Identity.GetUserIdentity(),
                Value = args.Rating.GetValueOrDefault(),
                RatedType = ResourceTypes.Quiz,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Quiz/Rating/{ev.Id}", ev.Id);
        }

        /// <summary>
        /// Returns the review to the exhibit with the given ID
        /// </summary>
        /// <param name="id">ID of the exhibit the review belongs to</param>
        [HttpGet("Review/{id}")]
        [ProducesResponseType(typeof(ReviewResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetRreview(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, id));

            if (!_reviewIndex.Exists(ResourceTypes.Exhibit.Name, id))
                return NotFound(ErrorMessages.ReviewNotFound(ResourceTypes.Exhibit, id));

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.Exhibit.Name, id);
            var review = _db.Get<Review>((ResourceTypes.Review, reviewId));

            if (!review.ReviewableByStudents && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                return Forbid();

            var result = new ReviewResult(review);

            return Ok(result);
        }

        /// <summary>
        /// Creates a review for the exhibit with the given ID
        /// </summary>
        /// <param name="id">ID of the exhibit the review belongs to</param>
        /// <param name="args">Arguments for the review</param>
        [HttpPost("Review/{id}")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostReviewAsync(int id, ReviewArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, id));

            if (!_entityIndex.Status(ResourceTypes.Exhibit, id).Equals(ContentStatus.In_Review))
                return BadRequest(ErrorMessages.CannotAddReviewToContentWithWrongStatus());

            if (_reviewIndex.Exists(ResourceTypes.Exhibit.Name, id))
                return BadRequest(ErrorMessages.ContentAlreadyHasReview(ResourceTypes.Exhibit, id));

            if (!UserPermissions.IsAllowedToCreateReview(User.Identity, _entityIndex.Owner(ResourceTypes.Exhibit, id)))
                return Forbid();

            var newReview = new Review(args)
            {
                EntityId = id,
                EntityType = ResourceTypes.Exhibit.Name
            };

            var reviewId = _reviewIndex.NextId(ResourceTypes.Exhibit);

            await EntityManager.CreateEntityAsync(_eventStore, newReview, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());

            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Review/{reviewId}", reviewId);
        }

        /// <summary>
        /// Changes the review that belongs to the exhibit with the given ID
        /// </summary>
        /// <param name="id">ID of the exhibit the review belongs to</param>
        /// <param name="args">Arguments for the review</param>
        [HttpPut("Review/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutReviewAsync(int id, ReviewUpdateArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, id));

            if (!_reviewIndex.Exists(ResourceTypes.Exhibit.Name, id))
                return NotFound(ErrorMessages.ReviewNotFound(ResourceTypes.Exhibit, id));

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.Exhibit.Name, id);
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
                updatedReview.Approved = IsReviewApproved(updatedReview, args.Approved);

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

        /// <summary>
        /// Deletes the review of the exhibit with the given ID
        /// </summary>
        /// <param name="id">ID of the exhibit the review belongs to</param>
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

            if (!_entityIndex.Exists(ResourceTypes.ExhibitPage, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.ExhibitPage, id));

            if (!_reviewIndex.Exists(ResourceTypes.ExhibitPage.Name, id))
                return NotFound(ErrorMessages.ReviewNotFound(ResourceTypes.ExhibitPage, id));

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.ExhibitPage.Name, id);

            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());
            return NoContent();
        }

        private bool IsReviewApproved(Review updatedReview, bool approved)
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
                return numberOfApproves >= updatedReview.StudentsToApprove;
            }
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
                    .Where(id => !_entityIndex.Exists(ResourceTypes.Tag, id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Tags),
                        ErrorMessages.ContentNotFound(ResourceTypes.Tag, id));
            }

            // ensure referenced pages exist
            if (args.Pages != null)
            {
                var invalidIds = args.Pages
                    .Where(id => !_entityIndex.Exists(ResourceTypes.ExhibitPage, id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Pages),
                        ErrorMessages.ExhibitPageNotFound(id));
            }
        }

        private void ValidateQuizArgs(ExhibitQuizArgs args)
        {
            // ensure images exist
            var invalidIds = args.Questions.Select(quest => quest.Image)
                   .Where(imageId => imageId != null && !_mediaIndex.IsImage(imageId.Value))
                   .ToList();

            foreach (var imageId in invalidIds)
                ModelState.AddModelError(nameof(args.Questions),
                    ErrorMessages.ImageNotFound(imageId.GetValueOrDefault()));

            // ensure exhibit exist
            if (!_entityIndex.Exists(ResourceTypes.Exhibit, args.ExhibitId.GetValueOrDefault()))
                ModelState.AddModelError(nameof(args.ExhibitId),
                ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, args.ExhibitId.GetValueOrDefault()));
        }
    }
}
