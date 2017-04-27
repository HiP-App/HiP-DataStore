using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System.Threading.Tasks;
using System;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using MongoDB.Bson;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using MongoDB.Driver;
using System.Linq;

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

        public ExhibitsController(EventStoreClient eventStore, CacheDatabaseManager db)
        {
            _eventStore = eventStore;
            _db = db;
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
