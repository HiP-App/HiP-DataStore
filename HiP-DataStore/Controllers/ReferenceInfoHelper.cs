using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    /// <summary>
    /// Provides helper methods that simplify implementation of GET-methods that return reference info for resources.
    /// </summary>
    public static class ReferenceInfoHelper
    {
        /// <summary>
        /// Obtains reference information for a resource.
        /// </summary>
        /// <returns>
        /// "200 Ok" with reference info if successful.
        /// "404 Not Found" if no resource with the specified type and ID exists.
        /// </returns>
        public static IActionResult GetReferenceInfo(ResourceType type, int id, EntityIndex entityIndex, ReferencesIndex referencesIndex)
        {
            if (!entityIndex.Exists(type, id))
                return new NotFoundResult();

            return new OkObjectResult(new ReferenceInfoResult
            {
                Referencees = TransformToResult(referencesIndex.ReferencesOf(type, id)),
                Referencers = TransformToResult(referencesIndex.ReferencesTo(type, id))
            });

            IReadOnlyCollection<ReferenceInfoResult.ReferenceInfo> TransformToResult(IEnumerable<ReferencesIndex.Entry> refs)
            {
                return refs
                    .GroupBy(entry => entry.Type)
                    .Select(group => new ReferenceInfoResult.ReferenceInfo
                    {
                        Type = group.Key.Name,
                        Ids = group.Select(e => e.Id).OrderBy(i => i).ToList()
                    })
                    .OrderBy(group => group.Type)
                    .ToList();
            }
        }
    }
}
