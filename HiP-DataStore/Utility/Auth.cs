using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public static class Auth
    {
        // Adds function to get User Id from Context.User.Identity
        public static UserIdentity GetUserIdentity(this IIdentity identity)
        {
            var sub = (identity as ClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == "https://hip.cs.upb.de/sub");
            return (sub == null) ? UserIdentity.Anonymous : new UserIdentity(sub.Value);
        }

        public static IReadOnlyList<Claim> GetUserRoles(this IIdentity identity)
        {
            return (identity as ClaimsIdentity)?.FindAll(c => c.Type == "https://hip.cs.upb.de/roles").ToList() ?? new List<Claim>();
        }
    }

    public struct UserIdentity : IEquatable<UserIdentity>
    {
        public static readonly UserIdentity Anonymous = new UserIdentity(null);

        public string Id { get; }

        public UserIdentity(string id) => Id = id;

        public bool Equals(UserIdentity other) => other.Id == Id;

        public override bool Equals(object obj) => obj is UserIdentity other && Equals(other);

        public override int GetHashCode() => Id?.GetHashCode() ?? 0;

        public static bool operator ==(UserIdentity a, UserIdentity b) => Equals(a, b);

        public static bool operator !=(UserIdentity a, UserIdentity b) => !Equals(a, b);

        // Allows implicit casting between string and UserIdentity
        public static implicit operator UserIdentity(string id) => new UserIdentity(id);
        public static implicit operator string(UserIdentity identity) => identity.Id;
    }
}
