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
using System.Threading.Tasks;

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

        public ExhibitsController(EventStoreService eventStore, IMongoDbContext db, InMemoryCache cache)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = cache.Index<MediaIndex>();
            _entityIndex = cache.Index<EntityIndex>();
            _referencesIndex = cache.Index<ReferencesIndex>();
            _ratingIndex = cache.Index<RatingIndex>();
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
        [HttpGet("Questions/{exhibitId}")]
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
                 var question = _db.Get<QuizQuestion>((ResourceTypes.Quiz, q));
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
            if (!_entityIndex.Exists(ResourceTypes.Quiz, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceTypes.Quiz, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceTypes.Quiz, id)))
                return Forbid();

            var quiz = _db.Get<QuizQuestion>((ResourceTypes.Quiz, id));

            if (timestamp != null && quiz.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new ExhibitQuizQuestionResult(quiz)
            {
                Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.Quiz, id)
            };

            return Ok(result);
        }
        [HttpPost("{exhibitId}/Question")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostQuestionAsync(int exhibitId, [FromBody]ExhibitQuizQuestionArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            ValidateQuestionArgs(args, exhibitId);

            if (User.Identity.GetUserIdentity() == null)
                return Unauthorized();

            var question = new QuizQuestion(args) { ExhibitId = exhibitId };
            var id = _entityIndex.NextId(ResourceTypes.Quiz);
            await EntityManager.CreateEntityAsync(_eventStore, question, ResourceTypes.Quiz, id, User.Identity.GetUserIdentity());
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Quiz/{id}", id);
        }

        [HttpPut("Question/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateQuestionAsync(int id, [FromBody]ExhibitQuizQuestionArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            ValidateQuestionArgs(args);

            if (!_entityIndex.Exists(ResourceTypes.Quiz, id))
                return NotFound();

            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceTypes.Quiz, id)))
                return Forbid();

            var oldStatus = _entityIndex.Status(ResourceTypes.Quiz, id).GetValueOrDefault();
            if (args.Status == ContentStatus.Unpublished && oldStatus != ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeUnpublished(ResourceTypes.Quiz));

            // validation passed, emit event
            var oldQuestion = await _eventStore.EventStream.GetCurrentEntityAsync<QuizQuestion>(ResourceTypes.Quiz, id);
            var newQuestion = new QuizQuestion(args);
            //we need to keep the exhibit id
            newQuestion.ExhibitId = oldQuestion.ExhibitId;
            await EntityManager.UpdateEntityAsync(_eventStore, oldQuestion, newQuestion, ResourceTypes.Quiz, id, User.Identity.GetUserIdentity());

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

            var status = _entityIndex.Status(ResourceTypes.Quiz, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceTypes.Quiz, id)))
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.Quiz, id));

            if (status == ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.Quiz, id));

            // remove the quiz
            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.Quiz, id, User.Identity.GetUserIdentity());
            return NoContent();
        }

        [HttpGet("Questions/Rating/{exhibitId}")]
        [ProducesResponseType(typeof(RatingResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
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
                Average = _ratingIndex.Average(ResourceTypes.Quiz, exhibitId),
                Count = _ratingIndex.Count(ResourceTypes.Quiz, exhibitId),
                RatingTable = _ratingIndex.Table(ResourceTypes.Quiz, exhibitId)
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
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetMyQuizRating(int exhibitId)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Exhibit, exhibitId))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, exhibitId));

            var result = _ratingIndex.UserRating(ResourceTypes.Quiz, exhibitId, User.Identity);

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

            var ev = new RatingAdded()
            {
                Id = _ratingIndex.NextId(ResourceTypes.Quiz),
                EntityId = exhibitId,
                UserId = User.Identity.GetUserIdentity(),
                Value = args.Rating.GetValueOrDefault(),
                RatedType = ResourceTypes.Quiz,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Questions/Rating/{ev.Id}", ev.Id);
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

        private void ValidateQuestionArgs(ExhibitQuizQuestionArgs args, int? exhibitId = null)
        {
            // ensure image exist
            if (args.Image != null && !_mediaIndex.IsImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFound(args.Image.GetValueOrDefault()));

            // ensure exhibit exist
            if (exhibitId != null && exhibitId.HasValue)
                if (!_entityIndex.Exists(ResourceTypes.Exhibit, exhibitId.Value))
                    ModelState.AddModelError(nameof(exhibitId),
                    ErrorMessages.ContentNotFound(ResourceTypes.Exhibit, exhibitId.Value));
        }
    }
}
