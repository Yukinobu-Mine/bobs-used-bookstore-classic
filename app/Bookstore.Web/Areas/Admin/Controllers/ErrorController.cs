using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Areas.Admin.Controllers
{
    [AllowAnonymous]
    public class ErrorController : AdminAreaControllerBase
    {
        [Route("/Error/Index/{code:int}")]
        public IActionResult Index(int code)
        {
            return View();
        }

        [Route("/error")]
        public IActionResult Support()
        {
            return View("~/Views/Error/Index.cshtml");
        }
    }
}
