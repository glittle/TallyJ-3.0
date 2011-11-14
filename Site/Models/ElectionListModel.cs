using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.Models
{
	public class ElectionListModel : DataAccessibleModel
	{
		public bool Select(Guid wantedElectionGuid)
		{
			var election = Db.Elections.Where(e => e.ElectionGuid == wantedElectionGuid).SingleOrDefault();
			if (election == null)
			{
				return false;
			}
			UserSession.CurrentElection = election;
			return true;
		}

	  public JsonResult Copy(Guid guidOfElectionToCopy)
	  {
      var election = Db.Elections.Where(e => e.ElectionGuid == guidOfElectionToCopy).SingleOrDefault();
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
;
      }
      if (!result.Success.AsBool())
      {
        return new
        {
          Success = false,
          Message = "Sorry: " + result.Message
        }.AsJsonResult();

      }
	    election = Db.Elections.Where(e => e.ElectionGuid == result.NewElectionGuid).SingleOrDefault();
      if (election == null)
      {
        return new
        {
          Success = false,
          Message = "New election not found"
        }.AsJsonResult();
        
      }
      UserSession.CurrentElection = election;
      return new
      {
        Success = true,
        election.ElectionGuid
      }.AsJsonResult();

    }
	}
}