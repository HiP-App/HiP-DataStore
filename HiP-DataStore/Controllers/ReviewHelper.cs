using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System.Collections.Generic;
using System.Security.Principal;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    public static class ReviewHelper
    {
        public static bool IsReviewApproved(List<int> comments, bool approved, int? studentsToApprove, IIdentity identity, ReviewCommentIndex _reviewCommentIndex)
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
                    if (_reviewCommentIndex.Approved(id))
                        numberOfApproves++;
                }
                return numberOfApproves >= studentsToApprove;
            }
            return false;
        }

        public static string CheckBadRequestPost(int id, ResourceType resourceType, EntityIndex entityIndex, ReviewIndex reviewIndex)
        {
            if (!entityIndex.Status(resourceType, id).Equals(ContentStatus.In_Review))
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

            // keep old values if no new ones are specified
            if (args.Description == null)
                args.Description = oldReviewArgs.Description;
            if (args.ReviewableByStudents == null)
                args.ReviewableByStudents = oldReviewArgs.ReviewableByStudents;
            if (args.StudentsToApprove == null)
                args.StudentsToApprove = oldReviewArgs.StudentsToApprove;
            if (args.Reviewers == null)
                args.Reviewers = oldReviewArgs.Reviewers;

            return args;
        }
    }
}
