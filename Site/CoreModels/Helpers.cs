using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.Models;

namespace TallyJ.CoreModels
{
  public static class Helpers
  {
    public static bool IsCurrentElection(IHasElectionGuid item)
    {
      return item.ElectionGuid == UserSession.CurrentElectionGuid;
    }
  }
}