using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/[controller]")]
    public class MediaController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly UploadFilesConfig _uploadConfig;
        private readonly EntityIndex _entityIndex;
        private readonly MediaIndex _mediaIndex;
        private readonly ReferencesIndex _referencesIndex;

        public MediaController(EventStoreClient eventStore, CacheDatabaseManager db, InMemoryCache cache, IOptions<UploadFilesConfig> uploadConfig)
        {
            _eventStore = eventStore;
            _db = db;
            _entityIndex = cache.Index<EntityIndex>();
            _mediaIndex = cache.Index<MediaIndex>();
            _referencesIndex = cache.Index<ReferencesIndex>();
            _uploadConfig = uploadConfig.Value;
        }

        [HttpGet("ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetIds(ContentStatus? status)
        {
            return Ok(_entityIndex.AllIds(ResourceType.Media, status ?? ContentStatus.Published));
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostAsync([FromBody]MediaArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ev = new MediaCreated
            {
                Id = _entityIndex.NextId(ResourceType.Media),
                Properties = args,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return Created($"{Request.Scheme}://{Request.Host}/api/Media/{ev.Id}", ev.Id);
        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<MediaResult>), 200)]
        [ProducesResponseType(400)]
        public IActionResult Get(MediaQueryArgs args)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var query = _db.Database.GetCollection<MediaElement>(ResourceType.Media.Name).AsQueryable();

            try
            {
                var medias = query
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByStatus(args.Status)
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
                    .PaginateAndSelect(args.Page, args.PageSize, x => new MediaResult
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Description = x.Description,
                        Used = x.Referencers.Count > 0,
                        Type = x.Type,
                        Status = x.Status,
                        Timestamp = x.Timestamp
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
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var media = _db.Database.GetCollection<MediaElement>(ResourceType.Media.Name)
                .AsQueryable()
                .FirstOrDefault(x => x.Id == id);

            if (media == null)
                return NotFound();

            // Media instance wasn`t modified after timestamp
            if (timestamp != null && media.Timestamp <= timestamp)
                return StatusCode(304);

            var result = new MediaResult
            {
                Id = media.Id,
                Title = media.Title,
                Description = media.Description,
                Used = media.Referencers.Count > 0,
                Type = media.Type,
                Timestamp = media.Timestamp,
                Status = media.Status
            };

            return Ok(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteById(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Media, id))
                return NotFound();

            if (_referencesIndex.IsUsed(ResourceType.Media, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            // Remove file
            string directoryPath = Path.GetDirectoryName(_mediaIndex.GetFilePath(id));
            if (directoryPath != null && System.IO.Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);

            var ev = new MediaDeleted { Id = id };
            await _eventStore.AppendEventAsync(ev);
            return StatusCode(204);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutById(int id, [FromBody]MediaArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Media, id))
                return NotFound();

            var ev = new MediaUpdate
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now,
            };

            await _eventStore.AppendEventAsync(ev);
            return StatusCode(204);
        }

        [HttpGet("{id}/File")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult GetFileById(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutFileById(int id, IFormFile file)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceType.Media, id))
                return NotFound();

            var extension = file.FileName.Split('.').Last();
            var fileType = Enum.GetName(typeof(MediaType), _mediaIndex.GetMediaType(id));

            /* Checking supported extensions
             * Configuration catalogue has to have same key name as on of MediaType constant names */
            if (!_uploadConfig.SupportedFormats[fileType].Contains(extension.ToLower()))
                return BadRequest(new { Message = $"Extension '{extension}' is not supported for type '{fileType}'" });

            // Remove old file
            string oldFilePath = _mediaIndex.GetFilePath(id);
            if (oldFilePath != null && System.IO.File.Exists(oldFilePath))
                System.IO.File.Delete(oldFilePath);

            var fileDirectory = Path.Combine(_uploadConfig.Path, fileType, id.ToString());
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
                File = filePath,
                Timestamp = DateTimeOffset.Now
            };

            await _eventStore.AppendEventAsync(ev);
            return StatusCode(204);
        }

        [HttpGet("{id}/Refs")]
        [ProducesResponseType(typeof(ReferenceInfoResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetReferenceInfo(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return ReferenceInfoHelper.GetReferenceInfo(ResourceType.Media, id, _entityIndex, _referencesIndex);
        }
    }
}
