using System;
using System.Web.Mvc;
using TallyJ.Code;

namespace TallyJ.Models
{
  public class PulseModel : DataConnectedModel
  {
    public JsonResult ProcessPulse()
    {
      var isStillAllowed = new ComputerModel().ProcessPulse();


      var result = new 
                  {
                    Active = isStillAllowed,
                    //VersionNum = new Random().Next(1, 100),
                    PulseSeconds = isStillAllowed ? 0 : 60  // if 0, client will use its current pulse number
                  };

      // properties expected:
      //  Active  -- is this computer active in an election?
      //  PulseSeconds -- seconds to delay before next pulse
      //  VersionNum -- last version in db

      return result.AsJsonResult(JsonRequestBehavior.AllowGet);
    }
  }
}