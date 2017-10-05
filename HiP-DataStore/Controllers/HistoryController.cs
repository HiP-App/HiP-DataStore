using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
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

        public HistoryController(EventStoreClient eventStore) => _eventStore = eventStore;

        [HttpGet("/api/Exhibits/{id}/History")]
        public Task<IActionResult> GetExhibitSummary(int id) =>
            GetSummaryAsync(ResourceType.Exhibit, id);

        [HttpGet("/api/Exhibits/{id}/History/{timestamp}")]
        public Task<IActionResult> GetExhibitVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<Exhibit>(ResourceType.Exhibit, id, timestamp);

        [HttpGet("/api/Exhibits/Pages/{id}/History")]
        public Task<IActionResult> GetExhibitPageSummary(int id) =>
            GetSummaryAsync(ResourceType.ExhibitPage, id);

        [HttpGet("/api/Exhibits/Pages/{id}/History/{timestamp}")]
        public Task<IActionResult> GetExhibitPageVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<ExhibitPage>(ResourceType.ExhibitPage, id, timestamp);

        [HttpGet("/api/Media/{id}/History")]
        public Task<IActionResult> GetMediaSummary(int id) =>
            GetSummaryAsync(ResourceType.Media, id);

        [HttpGet("/api/Media/{id}/History/{timestamp}")]
        public Task<IActionResult> GetMediaVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<MediaElement>(ResourceType.Media, id, timestamp);

        [HttpGet("/api/Routes/{id}/History")]
        public Task<IActionResult> GetRouteSummary(int id) =>
            GetSummaryAsync(ResourceType.Route, id);

        [HttpGet("/api/Routes/{id}/History/{timestamp}")]
        public Task<IActionResult> GetRouteVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<Route>(ResourceType.Route, id, timestamp);

        [HttpGet("/api/Tags/{id}/History")]
        public Task<IActionResult> GetTagSummary(int id) =>
            GetSummaryAsync(ResourceType.Tag, id);

        [HttpGet("/api/Tags/{id}/History/{timestamp}")]
        public Task<IActionResult> GetTagVersion(int id, DateTimeOffset timestamp) =>
            GetVersionAsync<Route>(ResourceType.Tag, id, timestamp);


        private async Task<IActionResult> GetSummaryAsync(ResourceType type, int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TODO: Validate user permissions
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
