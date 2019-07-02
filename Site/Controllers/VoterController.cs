using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.CoreModels;

namespace TallyJ.Controllers
{
    public class VoterController : BaseController
    {
        public ActionResult Index()
        {
            return View("VoterHome", new VoterModel());
        }
    }
}