using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using MongoDB.Driver;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/[controller]")]
    public class MediaController : Controller
    {
    
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly MediaIndex _mediaIndex;
        private readonly UploadFilesConfig _uploadConfig;

        public MediaController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices, UploadFilesConfig uploadConfig)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _uploadConfig = uploadConfig;
         
        }

        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostAsync([FromBody] MediaArgs args)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ev = new MediaCreated
            {
                Id = _mediaIndex.NextId(),
                Properties = args
            };
            await _eventStore.AppendEventAsync(ev, Guid.NewGuid());
            return Ok();
        }


        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<MediaResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public IActionResult Get(MediaQueryArgs args)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var query = _db.Database.GetCollection<MediaElement>(MediaElement.CollectionName).AsQueryable();

            try
            {
                var medias = query
                    .FilterByIds(args.ExcludedIds, args.IncludedIds)
                    .FilterByStatus(args.Status)
                    .FilterIf(args.Used != null, x => x.IsUsed == args.Used)
                    .FilterIf(args.Type != null, x => x.Type == args.Type)
                    .FilterIf(args.Timestamp != null, x => DateTimeOffset.Compare(x.Timestamp, args.Timestamp.GetValueOrDefault()) == 1)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .Paginate(args.Page, args.PageSize)
                    .Select(x => new MediaResult
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Description = x.Description,
                        Used = x.IsUsed,
                        Type = x.Type,
                        Status = x.Status,
                        Timestamp = x.Timestamp
                    })
                    .ToList();



                var output = new AllItemsResult<MediaResult> { Total = medias.Count, Items = medias };

                return Ok(output);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(MediaResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var query = _db.Database.GetCollection<MediaElement>(MediaElement.CollectionName).AsQueryable();


            var media = query.Where(x => x.Id == id)
                             .FirstOrDefault();

            if (media == null)
                return NotFound();

            //Media instance wasn`t modified after timestamp
            if (DateTimeOffset.Compare(media.Timestamp, timestamp.GetValueOrDefault()) != 1)
                return StatusCode(304);



            var result = new MediaResult
            {
                Id = media.Id,
                Title = media.Title,
                Description = media.Description,
                Used = media.IsUsed,
                Type = media.Type,
                Timestamp = media.Timestamp,
                Status = media.Status
            };

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeleteById(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var query = _db.Database.GetCollection<MediaElement>(MediaElement.CollectionName).AsQueryable();

            var media = query.Where(x => x.Id == id)
                             .FirstOrDefault();

            if (media == null || media.IsUsed == true)
                return BadRequest();

            var ev = new MediaDeleted
            {
                Id = id
            };

            await _eventStore.AppendEventAsync(ev, Guid.NewGuid());

            return StatusCode(204);

        }


        [HttpPut("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PutById(int id, [FromBody]MediaUpdateArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var elementWithId = _db.Database.GetCollection<MediaElement>(MediaElement.CollectionName).Find(x => x.Id == id).FirstOrDefault();

            if (elementWithId == null)
                BadRequest();


            var ev = new MediaUpdate
            {
                Id = id,
                Properties = args,
                Timestamp = DateTimeOffset.Now,
                Status = args.Status ?? elementWithId.Status
            };
            await _eventStore.AppendEventAsync(ev, Guid.NewGuid());

            return StatusCode(204);
        }



    
        [HttpGet("{id:int}/File")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult GetFileById(int id)
        {
            var query = _db.Database.GetCollection<MediaElement>(MediaElement.CollectionName).AsQueryable();
            var media = query.Where(x => x.Id == id)
                             .FirstOrDefault();

            if (media == null|| media.File == null || !System.IO.File.Exists(media.File))
                return NotFound();

            new FileExtensionContentTypeProvider().TryGetContentType(media.File, out string mimeType );
            mimeType = mimeType ?? "application/octet-stream";


            return File(new FileStream(media.File,FileMode.Open), mimeType,Path.GetFileName(media.File));


        }

        [HttpPut("{id:int}/File")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutFileById(int id, IFormFile file)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            var query = _db.Database.GetCollection<MediaElement>(MediaElement.CollectionName).AsQueryable();
            var media = query.Where(x => x.Id == id)
                             .FirstOrDefault();
            if (media == null)
                return NotFound();

            var extension = file.FileName.Split('.').Last();
            string fileType = Enum.GetName(typeof(MediaType), media.Type);

            /* Checking supported extensions
             * Configuration catalogue has to have same key name as on of MediaType constant names */
            if (_uploadConfig.Formats[fileType].FirstOrDefault(y => y == extension) == null)
                return BadRequest(new { Message = $"Extension: {extension} is not supported for type : {fileType}" });


            // Remove old file
            if (media.File != null && System.IO.File.Exists(media.File))
                System.IO.File.Delete(media.File);



            var fileDirectory = Path.Combine(_uploadConfig.Path, fileType);
            Directory.CreateDirectory(fileDirectory);
            var filePath = Path.Combine(fileDirectory, file.FileName);

            if (file.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            var ev = new MediaFileUpdated
            {
                Id = media.Id,
                File = filePath,
                Timestamp = DateTimeOffset.Now
            };
            await _eventStore.AppendEventAsync(ev, Guid.NewGuid());


            return StatusCode(204);
        }

   

    }
}
