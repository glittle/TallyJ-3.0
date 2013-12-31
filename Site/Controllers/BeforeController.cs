using System;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Hubs;

namespace TallyJ.Controllers
{
  public class BeforeController : BaseController
  {
    public ActionResult Index()
    {
      return null;
    }

    public ActionResult FrontDesk()
    {
      return View(new PeopleModel());
    }

    public ActionResult RollCall()
    {
      return View(new RollCallModel());
    }

    public JsonResult VotingMethod(int id, string type, int last)
    {
      return new PeopleModel().RegisterVotingMethodJson(id, type, last);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="op"></param>
    /// <param name="connId">The connection Id created in the browser</param>
    /// <returns></returns>
    public JsonResult FrontDeskHub(string op, string connId)
    {
      switch (op)
      {
        case "join":
          new FrontDeskHub().Join(connId);
          
          return true.AsJsonResult();
      }
      
      return null;
    }
  }
}