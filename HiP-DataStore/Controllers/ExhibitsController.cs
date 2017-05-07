using Microsoft.AspNetCore.Mvc;
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
        private readonly IdIndex _idIndex;

        public ExhibitsController(EventStoreClient eventStore, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = indices.OfType<MediaIndex>().First();
            _idIndex = indices.OfType<IdIndex>().First();
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ExhibitResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public IActionResult Get(ExhibitQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitQueryArgs();

            var query = _db.Database.GetCollection<Exhibit>(Exhibit.CollectionName).AsQueryable();

            try
            {
                var exhibits = query
                    .FilterByIds(args.ExcludedIds, args.IncludedIds)
                    .FilterByStatus(args.Status)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Name.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .FilterIf(args.RouteIds != null,
                        x => true) // TODO: Filter by route
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("name", x => x.Name),
                        ("timestamp", x => x.Timestamp))
                    .Paginate(args.Page, args.PageSize)
                    .ToList();

                // TODO: What to do with timestamp?
                var results = exhibits.Select(x => new ExhibitResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Image = (int?)x.Image.Id,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude
                }).ToList();

                return Ok(results);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
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
            if (args.Image.HasValue && !_mediaIndex.IsPublishedImage(args.Image.Value))
                return StatusCode(422, $"ID '{args.Image}' does not refer to a published image");

            // TODO: ensure referenced tags exist and are published (+ return tags in ExhibitResult)

            var ev = new ExhibitCreated
            {
                Id = _idIndex.NextId<Exhibit>(),
                Properties = args
            };

            await _eventStore.AppendEventAsync(ev, Guid.NewGuid());
            return Ok();
        }
    }
}
