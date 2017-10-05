using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    /// <summary>
    /// Provides methods to obtain a version history or specific versions of an entity.
    /// </summary>
    [Authorize]
    public class HistoryController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly EntityIndex _entityIndex;

        public HistoryController(EventStoreClient eventStore, InMemoryCache cache)
        {
            _eventStore = eventStore;
            _entityIndex = cache.Index<EntityIndex>();
        }

        // APIs to get a summary of creation/deletion/updates

        [HttpGet("/api/Exhibits/{id}/History")]
        [ProducesResponseType(typeof(HistorySummary), 200)]
        [ProducesResponseType(403)]
        public Task<IActionResult> GetExhibitSummary(int id) =>
            GetSummaryAsync(ResourceType.Exhibit, id);
        
        [HttpGet("/api/Exhibits/Pages/{id}/History")]
        [ProducesResponseType(typeof(HistorySummary), 200)]
        [ProducesResponseType(403)]
        public Task<IActionResult> GetExhibitPageSummary(int id) =>
            GetSummaryAsync(ResourceType.ExhibitPage, id);

        [HttpGet("/api/Media/{id}/History")]
        [ProducesResponseType(typeof(HistorySummary), 200)]
        [ProducesResponseType(403)]
        public Task<IActionResult> GetMediaSummary(int id) =>
            GetSummaryAsync(ResourceType.Media, id);

        [HttpGet("/api/Routes/{id}/History")]
        [ProducesResponseType(typeof(HistorySummary), 200)]
        [ProducesResponseType(403)]
        public Task<IActionResult> GetRouteSummary(int id) =>
           GetSummaryAsync(ResourceType.Route, id);

        [HttpGet("/api/Tags/{id}/History")]
        [ProducesResponseType(typeof(HistorySummary), 200)]
        [ProducesResponseType(403)]
        public Task<IActionResult> GetTagSummary(int id) =>
            GetSummaryAsync(ResourceType.Tag, id);


        // APIs to get the state of an entity at a specific point in time

        [HttpGet("/api/Exhibits/{id}/History/{timestamp}")]
        public Task<IActionResult> GetExhibitVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<Exhibit>(ResourceType.Exhibit, id, timestamp);

        [HttpGet("/api/Exhibits/Pages/{id}/History/{timestamp}")]
        public Task<IActionResult> GetExhibitPageVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<ExhibitPage>(ResourceType.ExhibitPage, id, timestamp);

        [HttpGet("/api/Media/{id}/History/{timestamp}")]
        public Task<IActionResult> GetMediaVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<MediaElement>(ResourceType.Media, id, timestamp);

        [HttpGet("/api/Routes/{id}/History/{timestamp}")]
        public Task<IActionResult> GetRouteVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<Route>(ResourceType.Route, id, timestamp);

        [HttpGet("/api/Tags/{id}/History/{timestamp}")]
        public Task<IActionResult> GetTagVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<Route>(ResourceType.Tag, id, timestamp);


        // Private helper methods

        private async Task<IActionResult> GetSummaryAsync(ResourceType type, int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToGetHistory(User.Identity, _entityIndex.Owner(type, id)))
                return Forbid();

            var summary = await HistoryUtil.GetSummaryAsync(_eventStore.EventStream, (type, id));
            return Ok(summary);
        }

        private async Task<IActionResult> GetVersionAsync<T>(ResourceType type, int id, DateTimeOffset timestamp)
            where T : ContentBase
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TODO: Validate user permissions
            var version = await HistoryUtil.GetVersionAsync<T>(_eventStore.EventStream, (type, id), timestamp);
            return Ok(version);
        }
    }
}
