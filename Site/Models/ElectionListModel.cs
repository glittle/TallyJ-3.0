using System;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.Models
{
	public class ElectionListModel : BaseViewModel
	{
		public bool Select(Guid wantedElectionGuid)
		{
			var election = DbContext.Elections.Where(e => e.ElectionGuid == wantedElectionGuid).SingleOrDefault();
			if (election == null)
			{
				return false;
			}
			UserSession.CurrentElection = election;
			return true;
		}
	}
}