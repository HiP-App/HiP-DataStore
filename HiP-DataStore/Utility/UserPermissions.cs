using PaderbornUniversity.SILab.Hip.DataStore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class UserPermissions
    {

        public static bool IsAllowedToCreate(IIdentity identity,ContentStatus Status)
        {
            if (Status != ContentStatus.Published && CheckRoles(identity, UserRoles.Student))
                return true;

            return CheckRoles(identity);

        }

        public static bool IsAllowedToEdit(IIdentity identity, ContentStatus Status, bool isOwner)
        {
            if (Status != ContentStatus.Published && isOwner)
                return true;

            return CheckRoles(identity);

        }

        public static bool IsAllowedToDelete(IIdentity identity, ContentStatus Status, bool isOwner)
        {
            if (Status != ContentStatus.Published && isOwner)
                return true;

            return CheckRoles(identity);          
        }

        public static bool IsAllowedToGet(IIdentity identity, ContentStatus Status, bool isOwner)
        {
            if (Status == ContentStatus.Published || isOwner)
                return true;

            return CheckRoles(identity);
        }

        public static bool IsAllowedToGetAll(IIdentity identity, ContentStatus Status)
        {
            if (Status == ContentStatus.Published)
                return true;

            return CheckRoles(identity);
        }

        //Check if the user has the nessesary roles
        static bool CheckRoles(IIdentity identity ,UserRoles allowedToProceed = UserRoles.Administrator | UserRoles.Supervisor)
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
