using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    /// <summary>
    /// Controller for testing purposes.
    /// </summary>
    [Route("api/[controller]")]
    public class ExhibitsController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly MediaIndex _mediaIndex;

        public ExhibitsController(EventStoreClient eventStore, CacheDatabaseManager db, MediaIndex mediaIndex)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = mediaIndex;
        }

        [HttpGet]
        public IActionResult Get([FromBody]ExhibitQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            IQueryable<Exhibit> query = _db.Database.GetCollection<Exhibit>(Exhibit.CollectionName).AsQueryable();

            // filter by IDs
            query = query.Where(x => args.IncludesId(x.Id.ToString()));

            // filter by status
            if (args.Status != ContentStatus.All)
                query = query.Where(x => x.Status == args.Status);

            // filter by query string
            if (!string.IsNullOrEmpty(args.Query))
                query = query.Where(x => x.Name.Contains(args.Query) || x.Description.Contains(args.Query));

            // filter by route (TODO)
            if (args.RouteIds != null)
            {

            }
            
            // order results
            switch (args.OrderBy)
            {
                case "id": query = query.OrderBy(x => x.Id); break;
                case "name": query = query.OrderBy(x => x.Name); break;
                case "timestamp": query = query.OrderBy(x => x.Timestamp); break;
            }

            // paging
            query = query.Skip(args.Page * args.PageSize).Take(args.PageSize);

            // TODO: What to do with timestamp?

            var results = query.Select(x => new ExhibitResult
            {
                Id = x.Id.ToString(),
                Name = x.Name,
                Description = x.Description,
                Image = x.Image.Id.ToString(),
                Latitude = x.Latitude,
                Longitude = x.Longitude
            }).ToList();

            return Ok(results);
        }














        /// <summary>
        /// An example of a "command" that issues a "create new exhibit"-event to the EventStore.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Post([FromBody]ExhibitArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ensure referenced image exists and is published
            if (!_mediaIndex.IsPublishedImage(args.Image))
                return StatusCode(422, $"ID '{args.Image}' does not refer to a published image");

            var ev = new ExhibitCreated
            {
                Name = args.Name,
                Description = args.Description,
                ImageId = ObjectId.TryParse(args.Image, out var id) ? id : ObjectId.Empty,
                Latitude = args.Latitude,
                Longitude = args.Longitude
            };

            await _eventStore.AppendEventAsync(ev, Guid.NewGuid());
            return Ok();
        }
    }
}
