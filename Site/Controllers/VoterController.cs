using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;

namespace TallyJ.Controllers
{
  [ForVoter]
  public class VoterController : BaseController
  {
    public ActionResult Index()
    {
      return View("VoterHome", new VoterModel());
    }



    public JsonResult GetVoterElections()
    {
      var email = UserSession.VoterEmail;
      if (email.HasNoContent())
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var list = Db.Person
        .Where(p => p.Email == email)
        .GroupBy(p => p.ElectionGuid, pList => pList)
        .Select(g => new
        {
          ElectionGuid = g.Key,
          // only one entry per election
          g.OrderBy(p => p.C_RowId).FirstOrDefault().PersonGuid
        })
        .Concat(Db.OnlineVotingInfo.Select(ovi => new
        {
          ovi.ElectionGuid,
          ovi.PersonGuid
        }))
        .Distinct()
        .GroupJoin(Db.OnlineElection, ep => ep.ElectionGuid, oe => oe.ElectionGuid,
          (ep, oeList) => new { ep.ElectionGuid, ep.PersonGuid, onlineElection = oeList.FirstOrDefault() })
        .GroupJoin(Db.Election, g => g.ElectionGuid, e => e.ElectionGuid, (g, eList) => new { g.ElectionGuid, g.PersonGuid, g.onlineElection, fullElection = eList.FirstOrDefault() })
        .GroupJoin(Db.Person, j => j.PersonGuid, p => p.PersonGuid, (j, pList) => new { j.ElectionGuid, j.PersonGuid, j.onlineElection, j.fullElection, p = pList.FirstOrDefault() })
        .OrderByDescending(j => j.onlineElection.HistoryWhen)
        .ThenByDescending(j => j.onlineElection.WhenOpen)
        .ThenByDescending(j => j.fullElection.DateOfElection)
        .ThenBy(j => j.p.C_RowId)
        .Select(j => new
        {
          id = j.fullElection != null ? j.fullElection.ElectionGuid : j.onlineElection.ElectionGuid,
          name = j.fullElection != null ? j.fullElection.Name : j.onlineElection.ElectionName,
          online = j.onlineElection != null ? new
          {
            j.onlineElection.WhenOpen,
            j.onlineElection.WhenClose,
            j.onlineElection.CloseIsEstimate,
            j.onlineElection.AllowResultView
          } : null,
          person = new
          {
            name = j.p.C_FullNameFL,
            j.p.VotingMethod,
            j.p.RegistrationTime
          }
        });

      return new
      {
        list
      }.AsJsonResult();
    }
  }
}