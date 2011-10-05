using System;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Models;

namespace TallyJ.Controllers
{
	public class ElectionsController : BaseController
	{
		public ActionResult Index()
		{
			return null;
		}

		public JsonResult SelectElection(Guid guid)
		{
			var model = new ElectionListModel();
			if (model.Select(guid))
			{
				return UserSession.CurrentElection.AsJsonResult();
			}
			return false.AsJsonResult();
		}
	}
}