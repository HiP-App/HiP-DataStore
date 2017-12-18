using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using System;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class StatusesController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(string[]), 200)]
        public IActionResult Index() => Json(Enum.GetValues(typeof(ContentStatus)),
            new JsonSerializerSettings()
            {
                Converters = { new StringEnumConverter() }
            });
    }
}
