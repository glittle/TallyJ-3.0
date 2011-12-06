using TallyJ.Code.Session;
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