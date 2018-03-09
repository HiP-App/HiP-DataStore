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
using PaderbornUniversity.SILab.Hip.UserStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentStatus = PaderbornUniversity.SILab.Hip.DataStore.Model.ContentStatus;

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
        private readonly UserStoreService _userStoreService;
        private readonly ReviewIndex _reviewIndex;

        public ExhibitsController(EventStoreService eventStore, IMongoDbContext db, InMemoryCache cache, UserStoreService userStoreService)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = cache.Index<MediaIndex>();
            _entityIndex = cache.Index<EntityIndex>();
            _referencesIndex = cache.Index<ReferencesIndex>();
            _ratingIndex = cache.Index<RatingIndex>();
            _userStoreService = userStoreService;
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

        /// <summary>
        /// Returns all questions for an exhibit
        /// </summary>
        /// <param name="exhibitId">Id of the exhibit</param>
        /// <returns></returns>
        [HttpGet("{exhibitId}/Questions/")]
        [ProducesResponseType(typeof(IEnumerable<ExhibitQuizQuestionResult>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetQuestionsForExhibitId(int exhibitId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceTypes.Exhibit, exhibitId) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceTypes.Exhibit, exhibitId)))
                return Forbid();

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, exhibitId))
                return NotFound();

            var exhibit = _db.Get<Exhibit>((ResourceTypes.Exhibit, exhibitId));
            var results = exhibit.Questions.Select(q =>
             {
                 var question = _db.Get<QuizQuestion>((ResourceTypes.QuizQuestion, q));
                 return new ExhibitQuizQuestionResult(question);
             });

            return Ok(results);
        }

        [HttpGet("Question/{id}")]
        [ProducesResponseType(typeof(ExhibitQuizQuestionResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetQuestionById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (!_entityIndex.Exists(ResourceTypes.QuizQuestion, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceTypes.QuizQuestion, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceTypes.QuizQuestion, id)))
                return Forbid();

            var quiz = _db.Get<QuizQuestion>((ResourceTypes.QuizQuestion, id));

            if (timestamp != null && quiz.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new ExhibitQuizQuestionResult(quiz)
            {
                Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.QuizQuestion, id)
            };

            return Ok(result);
        }
        [HttpPost("{exhibitId}/Question")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> PostQuestionAsync(int exhibitId, [FromBody]ExhibitQuizQuestionRestArgs args)
        {
            ValidateQuestionArgs(args, exhibitId);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (User.Identity.GetUserIdentity() == null)
                return Unauthorized();

            var exhibit = _db.Get<Exhibit>((ResourceTypes.Exhibit, exhibitId));
            if (exhibit.Questions.Count == 10)
                return BadRequest(ErrorMessages.QuestionCannotBeCreated(exhibitId));

            var questionArgs = new ExhibitQuizQuestionArgs(exhibitId, args);
            var id = _entityIndex.NextId(ResourceTypes.QuizQuestion);
            await EntityManager.CreateEntityAsync(_eventStore, questionArgs, ResourceTypes.QuizQuestion, id, User.Identity.GetUserIdentity());
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Quiz/{id}", id);
        }

        [HttpPut("Question/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateQuestionAsync(int id, [FromBody]ExhibitQuizQuestionRestArgs args)
        {
            ValidateQuestionArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.QuizQuestion, id))
                return NotFound();

            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceTypes.QuizQuestion, id)))
                return Forbid();

            var oldStatus = _entityIndex.Status(ResourceTypes.QuizQuestion, id).GetValueOrDefault();
            if (args.Status == ContentStatus.Unpublished && oldStatus != ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeUnpublished(ResourceTypes.QuizQuestion));

            // validation passed, emit event
            var oldQuestionArgs = await _eventStore.EventStream.GetCurrentEntityAsync<ExhibitQuizQuestionArgs>(ResourceTypes.QuizQuestion, id);
            var newQuestionArgs = new ExhibitQuizQuestionArgs(oldQuestionArgs.ExhibitId, args);

            await EntityManager.UpdateEntityAsync(_eventStore, oldQuestionArgs, newQuestionArgs, ResourceTypes.QuizQuestion, id, User.Identity.GetUserIdentity());

            return StatusCode(204);
        }

        [HttpDelete("Question/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteQuestionAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.QuizQuestion, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceTypes.QuizQuestion, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceTypes.QuizQuestion, id)))
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.QuizQuestion, id));

            if (status == ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.QuizQuestion, id));

            // remove the quiz
            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.QuizQuestion, id, User.Identity.GetUserIdentity());
            return NoContent();
        }

        [HttpGet("Questions/Rating/{exhibitId}")]
        [ProducesResponseType(typeof(RatingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetQuizRating(int exhibitId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, exhibitId))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, exhibitId));           

            var result = new RatingResult()
            {
                Id = exhibitId,
                Average = _ratingIndex.Average(ResourceTypes.QuizQuestion, exhibitId),
                Count = _ratingIndex.Count(ResourceTypes.QuizQuestion, exhibitId),
                RatingTable = _ratingIndex.Table(ResourceTypes.QuizQuestion, exhibitId)
            };

            return Ok(result);
        }
        /// <summary>
        /// Gets the rating of the current user for the questions that are connected to the exhibit
        /// /// </summary>
        /// <param name="exhibitId"> Id of the exhibit </param>
        /// <returns></returns>
        [HttpGet("Questions/Rating/My/{exhibitId}")]
        [ProducesResponseType(typeof(byte?), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetMyQuizRating(int exhibitId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, exhibitId))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, exhibitId));

            var result = _ratingIndex.UserRating(ResourceTypes.QuizQuestion, exhibitId, User.Identity);

            return Ok(result);
        }

        [HttpPost("Questions/Rating/{exhibitId}")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostQuizRatingAsync(int exhibitId, RatingArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, exhibitId))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, exhibitId));

            if (User.Identity.GetUserIdentity() == null)
                return Unauthorized();

            var exhibit = _db.Get<Exhibit>((ResourceTypes.Exhibit, exhibitId));
            if (!exhibit.Questions.Any())
                return BadRequest(ErrorMessages.ExhibitHasNoQuestions(exhibitId));

            var ev = new RatingAdded()
            {
                Id = _ratingIndex.NextId(ResourceTypes.QuizQuestion),
                EntityId = exhibitId,
                UserId = User.Identity.GetUserIdentity(),
                Value = args.Rating.GetValueOrDefault(),
                RatedType = ResourceTypes.QuizQuestion,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Questions/Rating/{ev.Id}", ev.Id);
        }

        /// <summary>
        /// Get information about amount of exhibit visitors
        /// </summary>
        /// <param name="exhibitId">Id of the exhibit</param>
        /// <returns></returns>
        [HttpGet("Statistic/{exhibitId}")]
        [ProducesResponseType(typeof(ExhibitStatisticResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStatistic(int exhibitId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, exhibitId))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, exhibitId));

            if (!UserPermissions.IsAllowedToGetStatistic(User.Identity))
                return Forbid();
            
            var exhibitVisitedList = await _userStoreService.ExhibitVisitedAction.GetAllAsync(exhibitId, DateTime.Now.AddYears(-1));
            int year = exhibitVisitedList.Total;
            int month = 0;
            int day = 0;
            foreach(var exhibitVisited in exhibitVisitedList.Items)
            {
                if (exhibitVisited.Timestamp >= DateTime.Now.AddMonths(-1))
                    month++;

                if (exhibitVisited.Timestamp >= DateTime.Now.AddDays(-1))
                    day++;
            }

            return Ok(new ExhibitStatisticResult() { Year = year, Month = month, Day = day} );
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
        public IActionResult GetReview(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (ReviewHelper.CheckNotFoundGet(id, ResourceTypes.Exhibit, _entityIndex, _reviewIndex) is string errorMessage)
                return NotFound(errorMessage);

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

            if (ReviewHelper.CheckBadRequestPost(id, ResourceTypes.Exhibit, _entityIndex, _reviewIndex) is string errorMessage)
                return BadRequest(errorMessage);

            if (!UserPermissions.IsAllowedToCreateReview(User.Identity, _entityIndex.Owner(ResourceTypes.Exhibit, id)))
                return Forbid();

            var reviewId = _reviewIndex.NextId(ResourceTypes.Exhibit);

            args.EntityId = id;
            args.EntityType = ResourceTypes.Exhibit.Name;

            await EntityManager.CreateEntityAsync(_eventStore, args, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());

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
        public async Task<IActionResult> PutReviewAsync(int id, ReviewArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (ReviewHelper.CheckNotFoundPut(id, ResourceTypes.Exhibit, _entityIndex, _reviewIndex) is string errorMessage)
                return NotFound(errorMessage);

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.Exhibit.Name, id);
            var oldReviewArgs = await _eventStore.EventStream.GetCurrentEntityAsync<ReviewArgs>(ResourceTypes.Review, reviewId);

            if (ReviewHelper.CheckForbidPut(oldReviewArgs, User.Identity, _reviewIndex, reviewId))
                return Forbid();

            args = ReviewHelper.UpdateReviewArgs(args, oldReviewArgs, User.Identity);

            await EntityManager.UpdateEntityAsync(_eventStore, oldReviewArgs, args, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());
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

            if (ReviewHelper.CheckNotFoundGet(id, ResourceTypes.Exhibit, _entityIndex, _reviewIndex) is string errorMessage)
                return NotFound(errorMessage);

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.ExhibitPage.Name, id);

            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());
            return NoContent();
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

        private void ValidateQuestionArgs(ExhibitQuizQuestionRestArgs args, int? exhibitId = null)
        {
            // ensure image exist
            if (args.Image != null && !_mediaIndex.IsImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFound(args.Image.GetValueOrDefault()));

            // ensure exhibit exist
            if (exhibitId != null && !_entityIndex.Exists(ResourceTypes.Exhibit, exhibitId.Value))
                ModelState.AddModelError(nameof(exhibitId),
                ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, exhibitId.Value));
        }
    }
}
