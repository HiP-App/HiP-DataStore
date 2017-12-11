using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tag = PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Tag;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TagsController : Controller
    {
        private readonly EventStoreService _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly EntityIndex _entityIndex;
        private readonly MediaIndex _mediaIndex;
        private readonly TagIndex _tagIndex;
        private readonly ReferencesIndex _referencesIndex;

        public TagsController(EventStoreService eventStore, CacheDatabaseManager db, InMemoryCache cache)
        {
            _eventStore = eventStore;
            _db = db;
            _entityIndex = cache.Index<EntityIndex>();
            _mediaIndex = cache.Index<MediaIndex>();
            _tagIndex = cache.Index<TagIndex>();
            _referencesIndex = cache.Index<ReferencesIndex>();
        }

        [HttpGet("ids")]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetIds(ContentStatus status = ContentStatus.Published)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            return Ok(_entityIndex.AllIds(ResourceTypes.Tag, status, User.Identity));
        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<TagResult>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public IActionResult GetAll([FromQuery]TagQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (args.Status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            var query = _db.Database.GetCollection<Tag>(ResourceTypes.Tag.Name).AsQueryable();

            try
            {
                var tags = query
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByUser(args.Status, User.Identity)
                    .FilterByStatus(args.Status, User.Identity)
                    .FilterByTimestamp(args.Timestamp)
                    .FilterByUsage(args.Used)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .PaginateAndSelect(args.Page, args.PageSize, x => new TagResult(x)
                    {
                        Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.Tag, x.Id)
                    });


                return Ok(tags);
            }
            catch (InvalidSortKeyException e)
            {
                ModelState.AddModelError(nameof(args.OrderBy), e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TagResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceTypes.Tag, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceTypes.Tag, id)))
                return Forbid();

            var tag = _db.Database.GetCollection<Tag>(ResourceTypes.Tag.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == id);

            if (tag == null)
                return NotFound();

            if (timestamp != null && tag.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var tagResult = new TagResult(tag)
            {
                Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.Tag, id)
            };

            return Ok(tagResult);

        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> PostAsync([FromBody]TagArgs args)
        {
            ValidateTagArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToCreate(User.Identity, args.Status))
                return Forbid();

            if (_tagIndex.IsTitleExist(args.Title))
                return StatusCode(409);

            int id = _entityIndex.NextId(ResourceTypes.Tag);
            await EntityManager.CreateEntity(_eventStore, args, ResourceTypes.Tag, id, User.Identity.GetUserIdentity());
            return Created($"{Request.Scheme}://{Request.Host}/api/Tags/{id}", id);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> UpdateByIdAsync(int id, [FromBody]TagArgs args)
        {
            ValidateTagArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Tag, id))
                return NotFound();

            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceTypes.Tag, id)))
                return Forbid();

            var oldStatus = _entityIndex.Status(ResourceTypes.Tag, id).GetValueOrDefault();
            if (args.Status == ContentStatus.Unpublished && oldStatus != ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeUnpublished(ResourceTypes.Tag));

            var tagIdWithSameTitle = _tagIndex.GetIdByTagTitle(args.Title);

            if (tagIdWithSameTitle != null && tagIdWithSameTitle != id)
                return StatusCode(409, ErrorMessages.TagNameAlreadyUsed);

            var oldArgs = await EventStreamExtensions.GetCurrentEntity<TagArgs>(_eventStore.EventStream, ResourceTypes.Tag, id);
            await EntityManager.UpdateEntity(_eventStore, oldArgs, args, ResourceTypes.Tag, id, User.Identity.GetUserIdentity());
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteByIdAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.Tag, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceTypes.Tag, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceTypes.Tag, id)))
                return Forbid();

            if (status == ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.Route, id));

            if (_referencesIndex.IsUsed(ResourceTypes.Tag, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            await EntityManager.DeleteEntity(_eventStore, ResourceTypes.Tag, id, User.Identity.GetUserIdentity());
            return NoContent();
        }

        [HttpGet("{id}/Refs")]
        [ProducesResponseType(typeof(ReferenceInfoResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetReferenceInfo(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToGet(User.Identity, _entityIndex.Owner(ResourceTypes.Tag, id)))
                return Forbid();

            return ReferenceInfoHelper.GetReferenceInfo(ResourceTypes.Tag, id, _entityIndex, _referencesIndex);
        }


        private void ValidateTagArgs(TagArgs args)
        {
            if (args == null)
                return;

            // ensure referenced image exists
            if (args.Image != null && !_mediaIndex.IsImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFound(args.Image.Value));
        }
    }

}

