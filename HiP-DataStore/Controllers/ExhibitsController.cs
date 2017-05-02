using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
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

        /// <summary>
        /// An example of a "query" that gets all exhibits from the MongoDB cache database.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<ExhibitResult>> Get()
        {
            var exhibits = await _db.Database
                .GetCollection<Exhibit>(Exhibit.CollectionName)
                .Find(Builders<Exhibit>.Filter.Empty)
                .ToListAsync();

            return exhibits.Select(o => new ExhibitResult
            {
                Id = o.Id.ToString(),
                Name = o.Name,
                Description = o.Description,
                Image = o.Image.Id.ToString(),
                Latitude = o.Latitude,
                Longitude = o.Longitude
            });
        }
    }
}
