using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class StatusesController : Controller
    {
        [HttpGet]
        [ProducesResponseType(typeof(string[]), 200)]
        public IActionResult Index()
        {
            var id = User.Identity.GetUserIdentity();

            return Ok(new[]
            {
                ContentStatus.Published,
                ContentStatus.In_Review,
                ContentStatus.Draft
            });
        }
    }
}
