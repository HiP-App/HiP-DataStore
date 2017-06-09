using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.DataStore.Model;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    public class StatusesController : Controller
    {
        [HttpGet]
        [ProducesResponseType(typeof(string[]), 200)]
        public IActionResult Index() => Ok(new[]
        {
            ContentStatus.Published,
            ContentStatus.In_Review,
            ContentStatus.Draft
        });
    }
}
