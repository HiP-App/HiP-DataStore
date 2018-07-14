using Microsoft.Extensions.Logging;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using PaderbornUniversity.SILab.Hip.UserStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    public static class ReviewHelper
    {
        public static bool IsReviewApproved(List<int> comments, bool approved, int? studentsToApprove, IIdentity identity, ReviewCommentIndex reviewCommentIndex)
        {
            if (UserPermissions.IsSupervisorOrAdmin(identity))
            {
                return approved;
            }
            else if (studentsToApprove != null)
            {
                var numberOfApproves = 0;
                foreach (int id in comments)
                {
                    if (reviewCommentIndex.Approved(id))
                        numberOfApproves++;
                }
                return numberOfApproves >= studentsToApprove;
            }
            return false;
        }

        public static string CheckBadRequestPost(int id, ResourceType resourceType, EntityIndex entityIndex, ReviewIndex reviewIndex)
        {
            if (!entityIndex.Status(resourceType, id).Equals(Model.ContentStatus.In_Review))
                return ErrorMessages.CannotAddReviewToContentWithWrongStatus();

            if (reviewIndex.Exists(resourceType.Name, id))
                return ErrorMessages.ContentAlreadyHasReview(resourceType, id);

            return null;
        }

        public static string CheckNotFoundGet(int id, ResourceType resourceType, EntityIndex entityIndex, ReviewIndex reviewIndex)
        {
            if (!entityIndex.Exists(resourceType, id))
                return ErrorMessages.ContentNotFound(resourceType, id);

            if (!reviewIndex.Exists(resourceType.Name, id))
                return ErrorMessages.ReviewNotFound(resourceType, id);

            return null;
        }

        public static string CheckNotFoundPut(int id, ResourceType resourceType, EntityIndex entityIndex, ReviewIndex reviewIndex)
        {
            if (!entityIndex.Exists(resourceType, id))
                return ErrorMessages.ContentNotFound(resourceType, id);

            if (!reviewIndex.Exists(resourceType.Name, id))
                return ErrorMessages.ReviewNotFound(resourceType, id);

            return null;
        }

        public static bool CheckForbidPut(ReviewArgs oldReviewArgs, IIdentity identity, ReviewIndex reviewIndex, int reviewId)
        {
            if (!oldReviewArgs.ReviewableByStudents == true && !UserPermissions.IsSupervisorOrAdmin(identity))
                return true;
            // Only reviewers, owner and admins/supervisors are allowed to comment
            if (!UserPermissions.IsAllowedToCommentReview(identity, oldReviewArgs.Reviewers, reviewIndex.Owner(reviewId)))
                return true;

            return false;
        }

        public static ReviewArgs UpdateReviewArgs(ReviewArgs args, ReviewArgs oldReviewArgs, IIdentity identity)
        {
            // Only admins and supervisors are permitted to change the amount of students that are necessary to approve a review
            if (args.StudentsToApprove != oldReviewArgs.StudentsToApprove && !UserPermissions.IsSupervisorOrAdmin(identity))
                args.StudentsToApprove = oldReviewArgs.StudentsToApprove;
            // Only admins and supervisors are permitted to change whether students are allowed to approve a review
            if (args.ReviewableByStudents != oldReviewArgs.ReviewableByStudents && !UserPermissions.IsSupervisorOrAdmin(identity))
                args.ReviewableByStudents = oldReviewArgs.ReviewableByStudents;

            args.EntityType = oldReviewArgs.EntityType;
            args.EntityId = oldReviewArgs.EntityId;
            args.Comments = oldReviewArgs.Comments;
            args.Approved = oldReviewArgs.Approved;

            if (args.ReviewableByStudents == false)
                args.StudentsToApprove = 0;

            return args;
        }

        public static async Task SendReviewRequestNotificationsAsync(UserStoreService userStoreService, IMongoDbContext db, ILogger logger, int id, ReviewEntityType entityType, IEnumerable<string> reviewers)
        {
            var entityText = GetEntityTextForEntityType(db, entityType, id);
            try
            {
                foreach (var reviewer in reviewers)
                {
                    await ReviewHelper.SendReviewNotficationAsync(userStoreService, id, reviewer, entityType, $"Your review was requested on {entityText}");
                }
            }
            catch (Exception e)
            {
                logger.LogError("Sending review request notifiation failed!", e);
            }
        }

        public static async Task SendReviewNotficationAsync(UserStoreService service, int entityId, string recipient, ReviewEntityType entitiyType, string text)
        {
            await service.ReviewNotications.CreateNotificationAsync(new ReviewNotificationArgs()
            {
                EntityId = entityId,
                EntityType = entitiyType,
                Recipient = recipient,
                Text = text,
            });
        }

        public static string GetEntityTextForEntityType(IMongoDbContext db, ReviewEntityType entityType, int entityId)
        {
            switch (entityType)
            {
                case ReviewEntityType.Exhibit:
                    var exhibit = db.GetCollection<Exhibit>(ResourceTypes.Exhibit).FirstOrDefault(e => e.Id == entityId);
                    return $"Exhibit {exhibit.Name}";

                case ReviewEntityType.ExhibitPage:
                    var exhibitPage = db.GetCollection<ExhibitPage>(ResourceTypes.ExhibitPage).FirstOrDefault(e => e.Id == entityId);
                    return $"Exhibit page {exhibitPage.Title}";

                case ReviewEntityType.Route:
                    var route = db.GetCollection<Route>(ResourceTypes.Route).FirstOrDefault(r => r.Id == entityId);
                    return $"Route {route.Title}";

                default:
                    return "";
            }
        }
    }
}
