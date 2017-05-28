using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/Exhibits")]
    public class ExhibitPagesController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;
        private readonly ReferencesIndex _referencesIndex;
        private readonly ExhibitPageIndex _exhibitPageIndex;

        public ExhibitPagesController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _entityIndex = indices.OfType<EntityIndex>().First();
            _referencesIndex = indices.OfType<ReferencesIndex>().First();
            _exhibitPageIndex = indices.OfType<ExhibitPageIndex>().First();
        }

        [HttpGet("Pages/ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetAllIds(ContentStatus? status)
        {
            return Ok(_entityIndex.AllIds(ResourceType.ExhibitPage, status ?? ContentStatus.Published));
        }

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

        [HttpGet("{exhibitId}/Pages/ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetIdsForExhibit(int exhibitId, ContentStatus? status)
        {
            var exhibit = _db.Database.GetCollection<Exhibit>(ResourceType.Exhibit.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == exhibitId);

            if (exhibit == null)
                return NotFound();

            var ids = exhibit.Pages.LoadAll(_db.Database)
                .Where(p => status == ContentStatus.All || p.Status == status)
                .Select(p => p.Id)
                .ToList();

            return Ok(ids);
        }

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
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
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

        [HttpPost("{exhibitId}/Pages")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PostForExhibitAsync(int exhibitId, [FromBody]ExhibitPageArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Exhibit, exhibitId))
                return NotFound();

            if (!IsExhibitPageArgsValid(args, out var validationError))
                return validationError;

            // validation passed, emit events (create page, add references to image(s) and additional info pages)
            var ev = new ExhibitPageCreated
            {
                Id = _entityIndex.NextId(ResourceType.ExhibitPage),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            await AddExhibitPageReferencesAsync(args, ev.Id);
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Pages/{ev.Id}", ev.Id);
        }

        [HttpPut("Pages/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PutAsync(int id, [FromBody]ExhibitPageArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!IsExhibitPageArgsValid(args, out var validationError))
                return validationError;

            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound();

            var currentPageType = _exhibitPageIndex.PageType(id).Value;
            if (currentPageType != args.Type)
                return StatusCode(422, ErrorMessages.CannotChangeExhibitPageType(currentPageType, args.Type));

            // validation passed, emit events (remove old references, update exhibit, add new references)
            var ev = new ExhibitPageUpdated
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await RemoveExhibitPageReferencesAsync(ev.Id);
            await _eventStore.AppendEventAsync(ev);
            await AddExhibitPageReferencesAsync(args, ev.Id);
            return StatusCode(204);
        }

        [HttpDelete("Pages/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            if (!_entityIndex.Exists(ResourceType.Exhibit, id))
                return NotFound();

            if (_referencesIndex.IsUsed(ResourceType.Exhibit, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            var ev = new ExhibitDeleted { Id = id };
            await _eventStore.AppendEventAsync(ev);

            // remove references to image and tags
            foreach (var reference in _referencesIndex.ReferencesOf(ResourceType.Exhibit, id))
            {
                var refRemoved = new ReferenceRemoved(ResourceType.Exhibit, id, reference.Type, reference.Id);
                await _eventStore.AppendEventAsync(refRemoved);
            }

            return NoContent();
        }


        private IActionResult QueryExhibitPages(IQueryable<ExhibitPage> allPages, ExhibitPageQueryArgs args)
        {
            try
            {
                var pages = allPages
                    .FilterByIds(args.ExcludedIds, args.IncludedIds)
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

        private bool IsExhibitPageArgsValid(ExhibitPageArgs args, out IActionResult response)
        {
            // constrain properties Image, Images and HideYearNumbers to their respective page types
            if (args.Image != null &&
                args.Type != PageType.AppetizerPage &&
                args.Type != PageType.ImagePage)
            {
                response = StatusCode(422, ErrorMessages.FieldNotAllowedForPageType(nameof(args.Image), args.Type));
                return false;
            }

            if (args.Images != null && args.Type != PageType.SliderPage)
            {
                response = StatusCode(422, ErrorMessages.FieldNotAllowedForPageType(nameof(args.Images), args.Type));
                return false;
            }

            if (args.HideYearNumbers != null && args.Type != PageType.SliderPage)
            {
                response = StatusCode(422, ErrorMessages.FieldNotAllowedForPageType(nameof(args.HideYearNumbers), args.Type));
                return false;
            }

            // ensure referenced image exists and is published
            if (args.Image != null && !_mediaIndex.IsPublishedImage(args.Image.Value))
            {
                response = StatusCode(422, ErrorMessages.ImageNotFoundOrNotPublished(args.Image.Value));
                return false;
            }

            // ensure referenced images exist and are published
            if (args.Images != null)
            {
                var invalidIds = args.Images
                    .Where(id => !_mediaIndex.IsPublishedImage(id))
                    .ToList();

                if (invalidIds.Count > 0)
                {
                    response = StatusCode(422, ErrorMessages.ImageNotFoundOrNotPublished(invalidIds[0]));
                    return false;
                }
            }

            // ensure referenced additional info pages exist and are published
            if (args.AdditionalInformationPages != null)
            {
                var invalidIds = args.AdditionalInformationPages
                    .Where(id => _entityIndex.Status(ResourceType.ExhibitPage, id) != ContentStatus.Published)
                    .ToList();

                if (invalidIds.Count > 0)
                {
                    response = StatusCode(422, ErrorMessages.ExhibitPageNotFoundOrNotPublished(invalidIds[0]));
                    return false;
                }
            }

            response = null;
            return true;
        }

        private async Task AddExhibitPageReferencesAsync(ExhibitPageArgs args, int pageId)
        {
            if (args.Audio != null)
            {
                var audioRef = new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Media, args.Audio.Value);
                await _eventStore.AppendEventAsync(audioRef);
            }

            if (args.Image != null)
            {
                var imageRef = new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Media, args.Image.Value);
                await _eventStore.AppendEventAsync(imageRef);
            }

            foreach (var imageId in args.Images ?? Enumerable.Empty<int>())
            {
                var imageRef = new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.Media, imageId);
                await _eventStore.AppendEventAsync(imageRef);
            }

            foreach (var id in args.AdditionalInformationPages ?? Enumerable.Empty<int>())
            {
                var pageRef = new ReferenceAdded(ResourceType.ExhibitPage, pageId, ResourceType.ExhibitPage, id);
                await _eventStore.AppendEventAsync(pageRef);
            }
        }

        private async Task RemoveExhibitPageReferencesAsync(int pageId)
        {
            foreach (var reference in _referencesIndex.ReferencesOf(ResourceType.ExhibitPage, pageId))
            {
                var refRemoved = new ReferenceRemoved(ResourceType.ExhibitPage, pageId, reference.Type, reference.Id);
                await _eventStore.AppendEventAsync(refRemoved);
            }
        }
    }
}
