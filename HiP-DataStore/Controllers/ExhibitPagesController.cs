using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel.Commands;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/Exhibits")]
    public class ExhibitPagesController : Controller
    {
        private readonly IOptions<ExhibitPagesConfig> _exhibitPagesConfig;
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;
        private readonly ReferencesIndex _referencesIndex;
        private readonly ExhibitPageIndex _exhibitPageIndex;

        public ExhibitPagesController(
            IOptions<ExhibitPagesConfig> exhibitPagesConfig,
            EventStoreClient eventStore,
            CacheDatabaseManager db,
            InMemoryCache cache)
        {
            _exhibitPagesConfig = exhibitPagesConfig;
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = cache.Index<MediaIndex>();
            _entityIndex = cache.Index<EntityIndex>();
            _referencesIndex = cache.Index<ReferencesIndex>();
            _exhibitPageIndex = cache.Index<ExhibitPageIndex>();
        }

        [HttpGet("Pages/ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetAllIds(ContentStatus? status)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(_entityIndex.AllIds(ResourceType.ExhibitPage, status ?? ContentStatus.Published));
        }

        /// <summary>
        /// Gets all pages in no particular order, unless otherwise specified in the query arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [HttpGet("Pages")]
        [ProducesResponseType(typeof(AllItemsResult<ExhibitPageResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public IActionResult GetAllPages(ExhibitPageQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitPageQueryArgs();

            var query = _db.Database.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name).AsQueryable();
            return QueryExhibitPages(query, args);
        }

        [HttpGet("{exhibitId:int}/Pages/ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetIdsForExhibit(int exhibitId, ContentStatus? status)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            status = status ?? ContentStatus.Published;

            var exhibit = _db.Database.GetCollection<Exhibit>(ResourceType.Exhibit.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == exhibitId);

            if (exhibit == null)
                return NotFound();

            var pageIds = exhibit.Pages
                .LoadAll(_db.Database)
                .Where(p => status == ContentStatus.All || p.Status == status)
                .Select(p => p.Id)
                .ToList();

            return Ok(pageIds);
        }

        /// <summary>
        /// Gets the pages of an exhibit in the correct order (as specified in the exhibit),
        /// unless otherwise specified in the query arguments.
        /// </summary>
        [HttpGet("{exhibitId}/Pages")]
        [ProducesResponseType(typeof(AllItemsResult<ExhibitPageResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public IActionResult GetPagesForExhibit(int exhibitId, ExhibitPageQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitPageQueryArgs();

            var exhibit = _db.Database.GetCollection<Exhibit>(ResourceType.Exhibit.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == exhibitId);

            if (exhibit == null)
                return NotFound();
            
            var query = exhibit.Pages.LoadAll(_db.Database).AsQueryable();

            return QueryExhibitPages(query, args);
        }

        [HttpGet("Pages/{id}")]
        [ProducesResponseType(typeof(ExhibitPageResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var page = _db.Database.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == id);

            if (page == null)
                return NotFound();

            if (timestamp != null && page.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new ExhibitPageResult(page);
            return Ok(result);
        }

        [HttpPost("Pages")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostAsync([FromBody]ExhibitPageArgs2 args)
        {
            // if font family is not specified, fallback to the configured default font family
            if (args != null && args.FontFamily == null)
                args.FontFamily = _exhibitPagesConfig.Value.DefaultFontFamily;

            ExhibitPageCommands.ValidateExhibitPageArgs(args, ModelState.AddModelError, _entityIndex, _mediaIndex, _exhibitPagesConfig.Value);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            // validation passed, emit events (create page, add references to image(s) and additional info pages)
            var newPageId = _entityIndex.NextId(ResourceType.ExhibitPage);
            var events = ExhibitPageCommands.Create(newPageId, args);
            await _eventStore.AppendEventsAsync(events);

            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Pages/{newPageId}", newPageId);
        }

        [HttpPut("Pages/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PutAsync(int id, [FromBody]ExhibitPageArgs2 args)
        {
            // if font family is not specified, fallback to the configured default font family
            if (args != null && args.FontFamily == null)
                args.FontFamily = _exhibitPagesConfig.Value.DefaultFontFamily;

            ExhibitPageCommands.ValidateExhibitPageArgs(args, ModelState.AddModelError, _entityIndex, _mediaIndex, _exhibitPagesConfig.Value);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            if (!_entityIndex.Exists(ResourceType.ExhibitPage, id))
                return NotFound();

            // ReSharper disable once PossibleInvalidOperationException (.Value is safe here since we know the entity exists)
            var currentPageType = _exhibitPageIndex.PageType(id).Value;
            // ReSharper disable once PossibleNullReferenceException (args == null is handled through ModelState.IsValid)
            if (currentPageType != args.Type)
                return StatusCode(422, ErrorMessages.CannotChangeExhibitPageType(currentPageType, args.Type));

            // validation passed, emit events (remove old references, update exhibit, add new references)
            var events = ExhibitPageCommands.Update(id, args, _referencesIndex);
            await _eventStore.AppendEventsAsync(events);

            return StatusCode(204);
        }

        [HttpDelete("Pages/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.ExhibitPage, id))
                return NotFound();

            if (_referencesIndex.IsUsed(ResourceType.ExhibitPage, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            var events = ExhibitPageCommands.Delete(id, _referencesIndex);
            await _eventStore.AppendEventsAsync(events);

            return NoContent();
        }

        [HttpGet("Pages/{id}/Refs")]
        [ProducesResponseType(typeof(ReferenceInfoResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetReferenceInfo(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return ReferenceInfoHelper.GetReferenceInfo(ResourceType.ExhibitPage, id, _entityIndex, _referencesIndex);
        }


        private IActionResult QueryExhibitPages(IQueryable<ExhibitPage> allPages, ExhibitPageQueryArgs args)
        {
            try
            {
                var pages = allPages
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByStatus(args.Status)
                    .FilterByTimestamp(args.Timestamp)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Text.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .PaginateAndSelect(args.Page, args.PageSize, x => new ExhibitPageResult(x));

                return Ok(pages);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
        }   
    }
}
