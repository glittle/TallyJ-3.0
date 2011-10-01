using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TallyJ.Controllers
{
    public class AfterController : Controller
    {
        //
        // GET: /Setup/

        public ActionResult Index()
        {
            return View();
        }
		public ActionResult Analyze()
		{
			return View();
		}

    	public ActionResult Reports()
    	{
    		return View();
    	}

    	public ActionResult Presenter()
    	{
    		return View();
    	}
    }
}
