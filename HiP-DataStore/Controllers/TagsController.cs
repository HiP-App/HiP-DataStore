using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/[controller]")]
    public class TagsController :Controller
    {
        private readonly EventStoreClient _ev;
        private readonly CacheDatabaseManager _db;
        private readonly EntityIndex _entityIndex;

        public TagsController(EventStoreClient eventStore, CacheDatabaseManager db,ICollection<IDomainIndex> indices)
        {

        }
        
    }
}
