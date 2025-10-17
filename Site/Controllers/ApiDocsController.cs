using System.Web.Mvc;

namespace TallyJ.Controllers
{
    /// <summary>
    /// Controller for API documentation pages
    /// </summary>
    public class ApiDocsController : Controller
    {
        /// <summary>
        /// Display API documentation page
        /// </summary>
        /// <returns>API documentation view</returns>
        public ActionResult Index()
        {
            return View();
        }
    }
}