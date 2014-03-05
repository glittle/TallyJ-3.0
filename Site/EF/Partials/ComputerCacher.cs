using System;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.CoreModels.Hubs;

namespace TallyJ.EF
{
  public class ComputerCacher : NonDbCacherBase<Computer>
  {
    private static object _lockObject;
    protected override object LockNonDbCacheBaseObject
    {
      get
      {
        return _lockObject ?? (_lockObject = new object());
      }
    }

    protected override void ItemChanged()
    {
      var currentElection = UserSession.CurrentElection;
      var oldValue = currentElection.ListForPublicNow;

      var lastContactOfTeller = AllForThisElection
                                    .Where(c => c.AuthLevel == "Known")
                                    .Max(c => c.LastContact);
      if (lastContactOfTeller != null && (currentElection.ListedForPublicAsOf == null || lastContactOfTeller > currentElection.ListedForPublicAsOf))
      {
        currentElection.ListedForPublicAsOf = lastContactOfTeller;
        new ElectionCacher().UpdateItemAndSaveCache(currentElection);
      }

      // changed from shown to hidden or back
      bool updateList = currentElection.ListForPublicNow != oldValue;

      if (!currentElection.ListForPublicNow)
      {
        new MainHub().DisconnectGuests();
        updateList = true;
      }
      if (updateList)
      {
        new PublicHub().ElectionsListUpdated();
      }
    }

  }
}