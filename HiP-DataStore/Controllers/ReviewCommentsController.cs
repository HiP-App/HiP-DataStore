﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using PaderbornUniversity.SILab.Hip.UserStore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ReviewCommentsController : Controller
    {
        private readonly EventStoreService _eventStore;
        private readonly ReviewIndex _reviewIndex;
        private readonly ReviewCommentIndex _reviewCommentIndex;
        private readonly IMongoDbContext _db;
        private readonly UserStoreService _userStoreService;

        public ReviewCommentsController(EventStoreService eventStore, InMemoryCache cache, IMongoDbContext db, UserStoreService userStoreService)
        {
            _eventStore = eventStore;
            _reviewIndex = cache.Index<ReviewIndex>();
            _reviewCommentIndex = cache.Index<ReviewCommentIndex>();
            _db = db;
            _userStoreService = userStoreService;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ReviewCommentResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult Get(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_reviewCommentIndex.Exists(id))
                return NotFound(ErrorMessages.ReviewCommentNotFound(id));

            var reviewComment = _db.Get<ReviewComment>((ResourceTypes.ReviewComment, id));

            var result = new ReviewCommentResult(reviewComment);

            return Ok(result);
        }

        /// <summary>
        /// Adds a Comment to a review
        /// </summary>
        /// <param name="reviewId">The id of the review</param>
        /// <param name="args"></param>
        [HttpPost("id")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostAsync(int reviewId, ReviewCommentArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_reviewIndex.Exists(reviewId))
                return NotFound(ErrorMessages.NoReviewWithIdExists(reviewId));

            var reviewArgs = await _eventStore.EventStream.GetCurrentEntityAsync<ReviewArgs>(ResourceTypes.Review, reviewId);

            var newReviewArgs = new ReviewArgs(reviewArgs);

            var reviewCommentId = _reviewCommentIndex.NextId();

            // Add comment id to review
            newReviewArgs.Comments.Add(reviewCommentId);

            if (args.Approved)
                newReviewArgs.Approved = ReviewHelper.IsReviewApproved(newReviewArgs.Comments, args.Approved, newReviewArgs.StudentsToApprove, User.Identity, _reviewCommentIndex);

            await SendReviewFinishedNotification(reviewId);

            await EntityManager.CreateEntityAsync(_eventStore, args, ResourceTypes.ReviewComment, reviewCommentId, User.Identity.GetUserIdentity());
            await EntityManager.UpdateEntityAsync(_eventStore, reviewArgs, newReviewArgs, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());

            return Created($"{Request.Scheme}://{Request.Host}/api/Comments/{reviewCommentId}", reviewCommentId);
        }

        /// <summary>
        /// Changes the review comment
        /// </summary>
        /// <param name="id">The id of the review comment</param>
        /// /// <param name="text"></param>
        [HttpPut("id")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutAsync(int id, string text)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_reviewCommentIndex.Exists(id))
                return NotFound(ErrorMessages.ReviewCommentNotFound(id));

            var reviewCommentArgs = await _eventStore.EventStream.GetCurrentEntityAsync<ReviewCommentArgs>(ResourceTypes.ReviewComment, id);

            var args = new ReviewCommentArgs()
            {
                Text = text,
                Approved = reviewCommentArgs.Approved,
            };

            await EntityManager.UpdateEntityAsync(_eventStore, reviewCommentArgs, args, ResourceTypes.ReviewComment, id, User.Identity.GetUserIdentity());

            return StatusCode(204);
        }

        private async Task SendReviewFinishedNotification(int reviewId)
        {
            var review = _db.GetCollection<Review>(ResourceTypes.Review).FirstOrDefault(r => r.Id == reviewId);
            if (review != null)
            {
                var reviewEntitiyType = GetReviewEntityTypeFromCommentEntityType(review.EntityType);
                var entityText = ReviewHelper.GetEntityTextForEntityType(_db, reviewEntitiyType, review.EntityId);
                await ReviewHelper.SendReviewNotficationAsync(_userStoreService, review.EntityId, review.UserId, reviewEntitiyType, $"Someone finished their review of your {entityText}");
            }
        }

        private ReviewEntityType GetReviewEntityTypeFromCommentEntityType(string entityType)
        {
            switch (entityType)
            {
                case string _ when entityType == ResourceTypes.Exhibit.Name:
                    return ReviewEntityType.Exhibit;

                case string _ when entityType == ResourceTypes.ExhibitPage.Name:
                    return ReviewEntityType.ExhibitPage;

                case string _ when entityType == ResourceTypes.Route.Name:
                    return ReviewEntityType.Route;

                default:
                    throw new NotSupportedException($"No ReviewEntityType could be found for the entity type {entityType}");
            }
        }
    }
}
