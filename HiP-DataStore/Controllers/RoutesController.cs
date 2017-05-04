using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Route("api/[controller]")]
    public class RoutesController : Controller
    {
        [HttpGet]
        public IActionResult Get(RoutesQueryArgs args)
        {
            return Ok();
        }
    }
}
