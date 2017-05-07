using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/[controller]")]
    public class RoutesController : Controller
    {
        private readonly CacheDatabaseManager _db;

        public RoutesController(CacheDatabaseManager db)
        {
            _db = db;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RouteResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        public IActionResult Get(RouteQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new RouteQueryArgs();

            var query = _db.Database.GetCollection<Route>(Route.CollectionName).AsQueryable();

            try
            {
                var routes = query
                    .FilterByIds(args.ExcludedIds, args.IncludedIds)
                    .FilterByStatus(args.Status)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .Paginate(args.Page, args.PageSize)
                    .ToList();

                // TODO: What to do with timestamp?
                var results = routes.Select(x => new RouteResult
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    Duration = x.Duration,
                    Distance = x.Distance,
                    Image = (int?)x.Image.Id,
                    Audio = (int?)x.Audio.Id,
                    Exhibits = x.Exhibits.Select(id => (int)id).ToArray(),
                    Status = x.Status,
                    Tags = x.Tags.Select(id => (int)id).ToArray(),
                    Timestamp = x.Timestamp
                }).ToList();

                return Ok(results);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
        }
    }
}
