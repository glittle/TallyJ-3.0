using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.Models
{
  public static class BallotModelFactory
  {
    public static IBallotModel GetForCurrentElection()
    {
      var isSingle = UserSession.CurrentElection.IsSingleNameElection.AsBool();
      return isSingle ? (IBallotModel) new BallotSingleModel() : new BallotNormalModel();
    }
  }
}