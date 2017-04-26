using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System.Threading.Tasks;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    /// <summary>
    /// Controller for testing purposes.
    /// </summary>
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly EventStoreClient _eventStore;

        public ValuesController(EventStoreClient eventStore, CacheDatabaseManager asdf)
        {
            _eventStore = eventStore;
        }

        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            var ev = new ExhibitCreated
            {
                Name = "Uni Paderborn",
                Latitude = 17,
                Longitude = 5
            };

            await _eventStore.AppendEventAsync(ev, Guid.NewGuid());
            return new string[0];
        }
    }
}
