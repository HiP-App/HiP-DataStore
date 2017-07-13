﻿using Microsoft.AspNetCore.Mvc;
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

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
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
        public IActionResult GetAll()
        {
            var allRecords = _board.AllRecords().Reverse().ToList();
            var result = new AllItemsResult<ScoreRecordResult>
            {
                Total = allRecords.Count,
                Items = allRecords.Select(x => new ScoreRecordResult(x))
                                  .ToList()
            };
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AllItemsResult<ScoreResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetByUserId(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            if (!_board.Exists(id))
                return NotFound();

            var query = _db.Database.GetCollection<ScoreRecord>(ResourceType.ScoreRecord.Name).AsQueryable();
            var allRecords = query.Where(x => x.UserId == id)
                                  .OrderBy(x => x.Timestamp)
                                  .PaginateAndSelect(null, null, x => new ScoreResult(x));
            allRecords.Items = allRecords.Items.Reverse().ToList();

            return Ok(allRecords);
        }

        //id = User ID
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PutAsync(int id, int? score)
        {
            if (score == null || score < 0)
                ModelState.AddModelError("score", "Parameter is missing or value is negative");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ev = new ScoreAdded
            {
                Id = _board.NewId(),
                UserId = id,
                Score = score.GetValueOrDefault(),
                Timestamp = DateTimeOffset.Now,
            };

            await _eventStore.AppendEventAsync(ev);
            return StatusCode(204);
        }
    }
}