using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Models
{
  public static class Helpers
  {
    public static bool IsCurrentElection(IHasElectionGuid item)
    {
      return item.ElectionGuid == UserSession.CurrentElectionGuid;
    }
  }
}