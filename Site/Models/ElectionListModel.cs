using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.Models
{
  public class ElectionListModel : DataConnectedModel
  {
    public bool JoinIntoElection(Guid wantedElectionGuid)
    {
      var election = Db.Elections.SingleOrDefault(e => e.ElectionGuid == wantedElectionGuid);
      if (election == null)
      {
        return false;
      }

      SessionKey.CurrentElection.SetInSession(election);

      new ComputerModel().AddCurrentComputerIntoElection(election.ElectionGuid);
       
      return true;
    }

    public JsonResult Copy(Guid guidOfElectionToCopy)
    {
      var election = Db.Elections.SingleOrDefault(e => e.ElectionGuid == guidOfElectionToCopy);
      if (election == null)
      {
        return new
                 {
                   Success = false,
                   Message = "Not found"
                 }.AsJsonResult();
      }

      // copy in SQL
      var result = Db.CloneElection(election.ElectionGuid, UserSession.LoginId).SingleOrDefault();
      if (result == null)
      {
        return new
                 {
                   Success = false,
                   Message = "Unable to copy"
                 }.AsJsonResult();
      }
      if (!result.Success.AsBool())
      {
        return new
                 {
                   Success = false,
                   Message = "Sorry: " + result.Message
                 }.AsJsonResult();
      }
      election = Db.Elections.SingleOrDefault(e => e.ElectionGuid == result.NewElectionGuid);
      if (election == null)
      {
        return new
                 {
                   Success = false,
                   Message = "New election not found"
                 }.AsJsonResult();
      }
      SessionKey.CurrentElection.SetInSession(election);
      return new
               {
                 Success = true,
                 election.ElectionGuid
               }.AsJsonResult();
    }
  }
}