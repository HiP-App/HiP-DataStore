using PaderbornUniversity.SILab.Hip.DataStore.Model;
using System;
using System.Linq;
using System.Security.Principal;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class UserPermissions
    {
        private static UserRoles AllowedToGetDeletedContent = UserRoles.Supervisor;

        public static bool IsAllowedToCreate(IIdentity identity,ContentStatus status)
        {
            if (status == ContentStatus.Deleted)
                return false;

            if (status != ContentStatus.Published && CheckRoles(identity, UserRoles.Student))
                return true;

            return CheckRoles(identity);
        }

        public static bool IsAllowedToEdit(IIdentity identity, ContentStatus status, string ownerId)
        {
            if (status == ContentStatus.Deleted)
                return false;

            bool isOwner = ownerId == identity.GetUserIdentity();

            if (status != ContentStatus.Published && isOwner)
                return true;

            return CheckRoles(identity);
        }

        public static bool IsAllowedToDelete(IIdentity identity, ContentStatus status, string ownerId)
        {
            bool isOwner = ownerId == identity.GetUserIdentity();
            if (status != ContentStatus.Published && isOwner)
                return true;

            return CheckRoles(identity);
        }

        /// <summary>
        /// Checks whether a certain user is allowed to get entities of a certain status and owner.
        /// </summary>
        /// <param name="identity">User identity</param>
        /// <param name="status">Current status of the queried entity</param>
        /// <param name="ownerId">Owner of the queried entity</param>
        public static bool IsAllowedToGet(IIdentity identity, ContentStatus status, string ownerId)
        {
            if (status == ContentStatus.Deleted)
                return CheckRoles(identity, AllowedToGetDeletedContent);

            bool isOwner = ownerId == identity.GetUserIdentity();
            if (status == ContentStatus.Published || isOwner)
                return true;

            return CheckRoles(identity);
        }

        public static bool IsAllowedToGet(IIdentity identity, string ownerId)
        {
            return (ownerId == identity.GetUserIdentity()) || CheckRoles(identity);
        }

        public static bool IsAllowedToGetAll(IIdentity identity, ContentStatus status)
        {
             if (status == ContentStatus.Published)
                return true;
            return CheckRoles(identity);
        }

        public static bool IsAllowedToGetStatistic(IIdentity identity)
        {
            return CheckRoles(identity, UserRoles.Administrator);
        }

        public static bool IsAllowedToGetDeleted(IIdentity identity)
        {
               return CheckRoles(identity, AllowedToGetDeletedContent);
        }

        public static bool IsAllowedToGetHistory(IIdentity identity, string ownerId)
        {
            // The entity owner as well as supervisors and administrators are allowed
            return (ownerId == identity.GetUserIdentity()) || CheckRoles(identity);
        }

        //Check if the user has the nessesary roles
        static bool CheckRoles(IIdentity identity, UserRoles allowedToProceed = UserRoles.Administrator | UserRoles.Supervisor)
        {
            return identity.GetUserRoles()
                           .Any(x => (Enum.TryParse(x.Value, out UserRoles role) && (allowedToProceed & role) != 0)); // Bitwise AND
        }
    }

    [Flags]
    public enum UserRoles
    {
        None = 1,
        Administrator = 2,
        Supervisor = 4,
        Student = 8,
    }
}
