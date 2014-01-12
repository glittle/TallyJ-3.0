using System.Linq;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Hubs;

namespace TallyJ.EF
{
  public class ComputerCacher : CacherBase<Computer>
  {
    protected override IQueryable<Computer> MainQuery()
    {
      return CurrentDb.Computer.Where(c => c.ElectionGuid == UserSession.CurrentElectionGuid);
    }

    protected override void ItemChanged()
    {
      base.ItemChanged();

      var currentElection = UserSession.CurrentElection;
      var oldValue = currentElection.ListForPublicNow;

      currentElection.ListedForPublicAsOf = AllForThisElection
        .Where(c => c.AuthLevel == "Known")
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