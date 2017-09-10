using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using Microsoft.AspNetCore.Authorization;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ScoreBoardController : Controller
    {
        private readonly EventStoreClient _eventStore;
        private readonly CacheDatabaseManager _db;
        private readonly ScoreBoardIndex _board;

        public ScoreBoardController(EventStoreClient ev, CacheDatabaseManager db, IEnumerable<IDomainIndex> indices)
        {
            _eventStore = ev;
            _db = db;
            _board = indices.OfType<ScoreBoardIndex>().First();
        }

        [HttpGet]
        [ProducesResponseType(typeof(AllItemsResult<ScoreRecordResult>), 200)]
        [ProducesResponseType(400)]
        public IActionResult GetAll(ScoreBoardArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var query = _board.AllRecords().AsQueryable();
            var result = query.PaginateAndSelect(null, args.Length, x => new ScoreRecordResult(x));

            return Ok(result);
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(ScoreResults), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetByUserId()
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = User.Identity.GetUserIdentity();

            if (!_board.AllRecords().Any(x => x.UserId == id))
                return NotFound();

            var query = _db.Database.GetCollection<ScoreRecord>(ResourceType.ScoreRecord.Name).AsQueryable();
            var allRecords = new ScoreResults(query.Where(x => x.UserId == id)
                                                   .OrderByDescending(x => x.Timestamp)
                                                   .PaginateAndSelect(null, null, x => new ScoreResult(x))); 

            allRecords.Rank = _board.AllRecords().ToList().FindIndex(x => x.UserId == id) + 1;
        
            return Ok(allRecords);
        }

        [HttpPut]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PutAsync(int score)
        {
            if (score < 0)
                ModelState.AddModelError("score", "Parameter is missing or value is negative");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var id = User.Identity.GetUserIdentity();

            var ev = new ScoreAdded
            {
                Id = _board.NewId(),
                UserId = id,
                Score = score,
                Timestamp = DateTimeOffset.Now,
            };

            await _eventStore.AppendEventAsync(ev);
            return StatusCode(204);
        }
    }
}
