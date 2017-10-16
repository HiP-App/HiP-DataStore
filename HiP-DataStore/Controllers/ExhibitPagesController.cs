using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
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
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetAllIds(ContentStatus status = ContentStatus.Published)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            return Ok(_entityIndex.AllIds(ResourceType.ExhibitPage, status, User.Identity));
        }

        /// <summary>
        /// Gets all pages in no particular order, unless otherwise specified in the query arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [HttpGet("Pages")]
        [ProducesResponseType(typeof(AllItemsResult<ExhibitPageResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(422)]
        public IActionResult GetAllPages(ExhibitPageQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitPageQueryArgs();

            if (args.Status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            var query = _db.Database.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name).AsQueryable();
            return QueryExhibitPages(query, args);
        }

        [HttpGet("{exhibitId:int}/Pages/ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetIdsForExhibit(int exhibitId, ContentStatus status = ContentStatus.Published)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            var exhibit = _db.Database.GetCollection<Exhibit>(ResourceType.Exhibit.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == exhibitId);

            if (exhibit == null)
                return NotFound();

            var pageIds = exhibit.Pages
                .LoadAll(_db.Database)
                .AsQueryable()
                .Where(x => x.UserId == User.Identity.GetUserIdentity())
                .Where(p => status == ContentStatus.All || p.Status == status)
                .FilterIf(status == ContentStatus.All && !UserPermissions.IsAllowedToGetDeleted(User.Identity),
                                                                  x => x.Status != ContentStatus.Deleted)
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
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public IActionResult GetPagesForExhibit(int exhibitId, ExhibitPageQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitPageQueryArgs();

            if (args.Status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

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
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceType.ExhibitPage, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceType.ExhibitPage, id)))
                return Forbid();

            var page = _db.Database.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name)
                .AsQueryable()
                .Where(x => x.UserId == User.Identity.GetUserIdentity())
                .FirstOrDefault(x => x.Id == id);

            if (page == null)
                return NotFound();

            if (timestamp != null && page.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new ExhibitPageResult(page)
            {
                Timestamp = _referencesIndex.LastModificationCascading(ResourceType.ExhibitPage, id)
            };

            return Ok(result);
        }

        [HttpPost("Pages")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> PostAsync([FromBody]ExhibitPageArgs2 args)
        {
            // if font family is not specified, fallback to the configured default font family
            if (args != null && args.FontFamily == null)
                args.FontFamily = _exhibitPagesConfig.Value.DefaultFontFamily;

            ValidateExhibitPageArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ReSharper disable once PossibleNullReferenceException (args == null is handled through ModelState.IsValid)
            if (!UserPermissions.IsAllowedToCreate(User.Identity, args.Status))
                return Forbid();

            // validation passed, emit event
            var newPageId = _entityIndex.NextId(ResourceType.ExhibitPage);

            var ev = new ExhibitPageCreated3
            {
                Id = newPageId,
                UserId = User.Identity.GetUserIdentity(),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Pages/{newPageId}", newPageId);
        }

        [HttpPut("Pages/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PutAsync(int id, [FromBody]ExhibitPageArgs2 args)
        {
            // if font family is not specified, fallback to the configured default font family
            if (args != null && args.FontFamily == null)
                args.FontFamily = _exhibitPagesConfig.Value.DefaultFontFamily;

            ValidateExhibitPageArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.ExhibitPage, id))
                return NotFound();

            // ReSharper disable once PossibleNullReferenceException (args == null is handled through ModelState.IsValid)
            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceType.ExhibitPage, id)))
                return Forbid();

            // ReSharper disable once PossibleInvalidOperationException (.Value is safe here since we know the entity exists)
            var currentPageType = _exhibitPageIndex.PageType(id).Value;
            // ReSharper disable once PossibleNullReferenceException (args == null is handled through ModelState.IsValid)
            if (currentPageType != args.Type)
                return StatusCode(422, ErrorMessages.CannotChangeExhibitPageType(currentPageType, args.Type));

            // validation passed, emit event
            var ev = new ExhibitPageUpdated3
            {
                Id = id,
                UserId = User.Identity.GetUserIdentity(),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return StatusCode(204);
        }

        [HttpDelete("Pages/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.ExhibitPage, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceType.ExhibitPage, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceType.ExhibitPage, id)))
                return Forbid();

            if (_referencesIndex.IsUsed(ResourceType.ExhibitPage, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            var ev = new ExhibitPageDeleted2
            {
                Id = id,
                UserId = User.Identity.GetUserIdentity(),
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return NoContent();
        }

        [HttpGet("Pages/{id}/Refs")]
        [ProducesResponseType(typeof(ReferenceInfoResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetReferenceInfo(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToGet(User.Identity, _entityIndex.Owner(ResourceType.ExhibitPage, id)))
                return Forbid();

            return ReferenceInfoHelper.GetReferenceInfo(ResourceType.ExhibitPage, id, _entityIndex, _referencesIndex);
        }


        private IActionResult QueryExhibitPages(IQueryable<ExhibitPage> allPages, ExhibitPageQueryArgs args)
        {
            try
            {
                var pages = allPages
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByUser(args.Status,User.Identity)
                    .FilterByStatus(args.Status, User.Identity)
                    .FilterByTimestamp(args.Timestamp)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Text.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .FilterIf(args.Type != null, x => x.Type == args.Type)
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .PaginateAndSelect(args.Page, args.PageSize, x => new ExhibitPageResult(x)
                    {
                        Timestamp = _referencesIndex.LastModificationCascading(ResourceType.ExhibitPage, x.Id)
                    });

                return Ok(pages);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
        }

        private void ValidateExhibitPageArgs(ExhibitPageArgs2 args)
        {
            if (args == null)
                return;

            // constrain properties Image, Images and HideYearNumbers to their respective page types
            if (args.Image != null && args.Type != PageType.Appetizer_Page && args.Type != PageType.Image_Page)
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.Image), args.Type));

            if (args.Images != null && args.Type != PageType.Slider_Page)
                ModelState.AddModelError(nameof(args.Images),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.Images), args.Type));

            if (args.HideYearNumbers != null && args.Type != PageType.Slider_Page)
                ModelState.AddModelError(nameof(args.HideYearNumbers),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.HideYearNumbers), args.Type));

            // validate font family
            if (!_exhibitPagesConfig.Value.IsFontFamilyValid(args.FontFamily))
                ModelState.AddModelError(nameof(args.FontFamily), $"Font family must be null/unspecified (which defaults to {_exhibitPagesConfig.Value.DefaultFontFamily}) or one of the following: {string.Join(", ", _exhibitPagesConfig.Value.FontFamilies)}");

            // ensure referenced image exists
            if (args.Image != null && !_mediaIndex.IsImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFound(args.Image.Value));

            // ensure referenced audio exists
            if (args.Audio != null && !_mediaIndex.IsAudio(args.Audio.Value))
                ModelState.AddModelError(nameof(args.Audio),
                    ErrorMessages.AudioNotFound(args.Audio.Value));

            // ensure referenced slider page images exist
            if (args.Images != null)
            {
                var invalidIds = args.Images
                    .Select(img => img.Image)
                    .Where(id => !_mediaIndex.IsImage(id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Images),
                        ErrorMessages.ImageNotFound(id));
            }

            // ensure referenced additional info pages exist
            if (args.AdditionalInformationPages != null)
            {
                var invalidIds = args.AdditionalInformationPages
                    .Where(id => !_entityIndex.Exists(ResourceType.ExhibitPage, id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.AdditionalInformationPages),
                        ErrorMessages.ExhibitPageNotFound(id));
            }
        }
    }
}
