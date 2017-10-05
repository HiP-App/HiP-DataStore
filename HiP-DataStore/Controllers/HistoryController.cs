using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    /// <summary>
    /// Provides methods to obtain a version history or specific versions of an entity.
    /// </summary>
    public class HistoryController : Controller
    {
        private readonly EventStoreClient _eventStore;

        protected HistoryController(EventStoreClient eventStore)
        {
            _eventStore = eventStore;
        }

        [HttpGet("/api/Exhibits/{id}/History")]
        public Task<IActionResult> GetExhibitSummary(int id) => GetSummaryAsync(ResourceType.Exhibit, id);

        [HttpGet("/api/Exhibits/{id}/History")]
        public Task<IActionResult> GetExhibitVersion(int id) => GetSummaryAsync(ResourceType.Exhibit, id);

        [HttpGet("/api/Exhibits/Pages/{id}/History")]
        public Task<IActionResult> GetExhibitPageSummary(int id) => GetSummaryAsync(ResourceType.ExhibitPage, id);

        [HttpGet("/api/Exhibits/Pages/{id}/History")]
        public Task<IActionResult> GetExhibitPageVersion(int id) => GetSummaryAsync(ResourceType.ExhibitPage, id);

        // TODO: Implement for all resource types


        private async Task<IActionResult> GetSummaryAsync(ResourceType type, int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TODO: Validate user permissions
            var summary = await HistoryUtil.GetSummaryAsync(_eventStore.EventStream, (type, id));
            return Ok(summary);
        }

        [HttpGet("{id}/History/{timestamp}")]
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
