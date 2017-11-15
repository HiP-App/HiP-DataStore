using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class MediaController : Controller
    {
        private readonly EventStoreService _eventStore;
        private readonly ILogger<MediaController> _logger;
        private readonly CacheDatabaseManager _db;
        private readonly UploadFilesConfig _uploadConfig;
        private readonly EndpointConfig _endpointConfig;
        private readonly EntityIndex _entityIndex;
        private readonly MediaIndex _mediaIndex;
        private readonly ReferencesIndex _referencesIndex;

        public MediaController(EventStoreService eventStore, CacheDatabaseManager db, InMemoryCache cache,
            IOptions<UploadFilesConfig> uploadConfig, IOptions<EndpointConfig> endpointConfig,
            ILogger<MediaController> logger)
        {
            _logger = logger;
            _eventStore = eventStore;
            _db = db;
            _entityIndex = cache.Index<EntityIndex>();
            _mediaIndex = cache.Index<MediaIndex>();
            _referencesIndex = cache.Index<ReferencesIndex>();
            _uploadConfig = uploadConfig.Value;
            _endpointConfig = endpointConfig.Value;
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

            return Ok(_entityIndex.AllIds(ResourceType.Media, status, User.Identity));
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> PostAsync([FromBody]MediaArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToCreate(User.Identity, args.Status))
                return Forbid();

            var ev = new MediaCreated
            {
                Id = _entityIndex.NextId(ResourceType.Media),
                UserId = User.Identity.GetUserIdentity(),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Media/{ev.Id}", ev.Id);
        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<MediaResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public IActionResult Get([FromQuery]MediaQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (args.Status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            var query = _db.Database.GetCollection<MediaElement>(ResourceType.Media.Name).AsQueryable();

            try
            {
                var medias = query
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByUser(args.Status, User.Identity)
                    .FilterByStatus(args.Status, User.Identity)
                    .FilterByTimestamp(args.Timestamp)
                    .FilterByUsage(args.Used)
                    .FilterIf(args.Type != null, x => x.Type == args.Type)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .PaginateAndSelect(args.Page, args.PageSize, x => new MediaResult(x)
                    {
                        File = GenerateFileUrl(x),
                        Timestamp = _referencesIndex.LastModificationCascading(ResourceType.Media, x.Id)
                    });

                return Ok(medias);
            }
            catch (InvalidSortKeyException e)
            {
                ModelState.AddModelError(nameof(args.OrderBy), e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MediaResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceType.Media, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceType.Media, id)))
                return Forbid();

            var media = _db.Database.GetCollection<MediaElement>(ResourceType.Media.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == id);

            if (media == null)
                return NotFound();

            // Media instance wasn`t modified after timestamp
            if (timestamp != null && media.Timestamp <= timestamp)
                return StatusCode(304);

            var result = new MediaResult(media)
            {
                File = GenerateFileUrl(media),
                Timestamp = _referencesIndex.LastModificationCascading(ResourceType.Media, id)
            };

            return Ok(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteById(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Media, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceType.Media, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceType.Media, id)))
                return Forbid();

            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceType.Media, id)))
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceType.Media, id));

            if (status == ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceType.Media, id));

            if (_referencesIndex.IsUsed(ResourceType.Media, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            // Remove file
            var directoryPath = Path.GetDirectoryName(_mediaIndex.GetFilePath(id));
            if (directoryPath != null && Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);

            var ev = new MediaDeleted
            {
                Id = id,
                UserId = User.Identity.GetUserIdentity(),
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            await InvalidateThumbnailCacheAsync(id);
            return StatusCode(204);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutById(int id, [FromBody]MediaArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Media, id))
                return NotFound();
            
            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceType.Media, id)))
                return Forbid();

            var oldStatus = _entityIndex.Status(ResourceType.Media, id).GetValueOrDefault();
            if (args.Status == ContentStatus.Unpublished && oldStatus != ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeUnpublished(ResourceType.Media));

            var ev = new MediaUpdate
            {
                Id = id,
                UserId = User.Identity.GetUserIdentity(),
                Properties = args,
                Timestamp = DateTimeOffset.Now,
            };

            await _eventStore.AppendEventAsync(ev);
            return StatusCode(204);
        }

        [HttpGet("{id}/File")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetFileById(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceType.Media, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceType.Media, id)))
                return Forbid();

            if (!_entityIndex.Exists(ResourceType.Media, id))
                return NotFound();

            var media = _db.Database.GetCollection<MediaElement>(ResourceType.Media.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == id);

            if (media?.File == null || !System.IO.File.Exists(media.File))
                return NotFound();

            new FileExtensionContentTypeProvider().TryGetContentType(media.File, out string mimeType);
            mimeType = mimeType ?? "application/octet-stream";

            return File(new FileStream(media.File, FileMode.Open), mimeType, Path.GetFileName(media.File));
        }

        [HttpPut("{id}/File")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutFileById(int id, IFormFile file)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Media, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceType.Media, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToEdit(User.Identity, status, _entityIndex.Owner(ResourceType.Media, id)))
                return Forbid();

            var extension = file.FileName.Split('.').Last();
            var fileType = _mediaIndex.GetMediaType(id);

            /* Checking supported extensions
             * Configuration catalogue has to have same key name as on of MediaType constant names */
            if (!_uploadConfig.SupportedFormats[fileType.ToString()].Contains(extension.ToLower()))
                return BadRequest(new { Message = $"Extension '{extension}' is not supported for type '{fileType}'" });

            // Remove old file
            var oldFilePath = _mediaIndex.GetFilePath(id);
            if (oldFilePath != null && System.IO.File.Exists(oldFilePath))
                System.IO.File.Delete(oldFilePath);

            var fileDirectory = Path.Combine(_uploadConfig.Path, fileType.ToString(), id.ToString());
            Directory.CreateDirectory(fileDirectory);

            var filePath = Path.Combine(fileDirectory, Path.GetFileName(file.FileName));

            if (file.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            var ev = new MediaFileUpdated
            {
                Id = id,
                UserId = User.Identity.GetUserIdentity(),
                File = filePath,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);

            if (fileType == MediaType.Image)
                await InvalidateThumbnailCacheAsync(id);

            return StatusCode(204);
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

            if (!UserPermissions.IsAllowedToGet(User.Identity, _entityIndex.Owner(ResourceType.Media, id)))
                return Forbid();

            return ReferenceInfoHelper.GetReferenceInfo(ResourceType.Media, id, _entityIndex, _referencesIndex);
        }



        private string GenerateFileUrl(MediaElement mediaElement)
        {
            if (mediaElement.Type == MediaType.Image &&
                !string.IsNullOrWhiteSpace(_endpointConfig.ThumbnailUrlPattern))
            {
                // Generate thumbnail URL (if a thumbnail URL pattern is configured)
                return string.Format(_endpointConfig.ThumbnailUrlPattern, mediaElement.Id);
            }
            else
            {
                // Return direct URL
                return $"{Request.Scheme}://{Request.Host}/api/Media/{mediaElement.Id}/File";
            }
        }

        private async Task InvalidateThumbnailCacheAsync(int id)
        {
            if (!string.IsNullOrWhiteSpace(_endpointConfig.ThumbnailUrlPattern))
            {
                var url = string.Format(_endpointConfig.ThumbnailUrlPattern, id);

                try
                {
                    using (var http = new HttpClient())
                    {
                        http.DefaultRequestHeaders.Add("Authorization", Request.Headers["Authorization"].ToString());
                        var response = await http.DeleteAsync(url);
                        response.EnsureSuccessStatusCode();
                    }
                }
                catch (HttpRequestException e)
                {
                    _logger.LogWarning(e,
                        $"Request to clear thumbnail cache failed for media '{id}'; " +
                        $"thumbnail service might return outdated images (request URL was '{url}').");
                }
            }
        }
    }
}
