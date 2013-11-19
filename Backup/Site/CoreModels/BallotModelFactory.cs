using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.CoreModels
{
  public static class BallotModelFactory
  {
    public static IBallotModel GetForCurrentElection()
    {
      var isSingle = UserSession.CurrentElection.IsSingleNameElection;
      return isSingle ? (IBallotModel) new BallotSingleModel() : new BallotNormalModel();
    }
  }
}