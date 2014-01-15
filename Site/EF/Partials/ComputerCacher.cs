using System;
using System.Linq;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.CoreModels.Hubs;

namespace TallyJ.EF
{
  public class ComputerCacher : NonDbCacherBase<Computer>
  {
    protected override void ItemChanged()
    {
      var currentElection = UserSession.CurrentElection;
      var oldValue = currentElection.ListForPublicNow;

      if (currentElection.ListedForPublicAsOf.HasValue &&
          (DateTime.Now - currentElection.ListedForPublicAsOf < new TimeSpan(0, 0, 60)))
      {
        return;
      }

      currentElection.ListedForPublicAsOf = AllForThisElection
        .Where(c => c.TempAuthLevel == "Known")
        .Max(c => c.LastContact);

      new ElectionCacher().UpdateItemAndSaveCache(currentElection);

      // changed from shown to hidden or back
      if (currentElection.ListForPublicNow != oldValue)
      {
        new PublicHub().ElectionsListUpdated();
      }

    }
  }
}