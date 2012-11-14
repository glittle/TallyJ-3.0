using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ElectionsListViewModel : DataConnectedModel
  {
    public IEnumerable<object> MyElectionsInfo
    {
      get
      {
        var locationModel = ContextItems.LocationModel;
        var locations = locationModel.Locations
          .OrderBy(l => l.SortOrder)
          .Select(l => new { l.Name, l.C_RowId, IsCurrent = l.LocationGuid == UserSession.CurrentLocationGuid });

        return
          MyElections()
            .OrderByDescending(e => e.DateOfElection)
            .Select(e => new
                           {
                             e.Name,
                             e.ElectionGuid,
                             e.DateOfElection,
                             e.ElectionType,
                             e.ElectionMode,
                             e.ShowAsTest,
                             e.NumVoters
                           })
            .Select(info =>
                      {
                        var isCurrent = info.ElectionGuid == UserSession.CurrentElectionGuid;
                        return new
                                    {
                                      info.Name,
                                      info.ElectionGuid,
                                      DateOfElection = info.DateOfElection.HasValue ? info.DateOfElection.AsString("yyyy MMMM d") : "[No Date]",
                                      IsCurrent = isCurrent,
                                      Locations = isCurrent ? locations : null,
                                      Type = ElectionTypeEnum.TextFor(info.ElectionType),
                                      Mode = ElectionModeEnum.TextFor(info.ElectionMode).SurroundContentWith(" (",")"),
                                      IsTest = info.ShowAsTest.AsBoolean(),
                                      info.NumVoters
                                    };
                      });
      }
    }

    private List<vElectionListInfo> MyElections()
    {
      if (UserSession.IsKnownTeller)
      {
        var userGuid = UserSession.UserGuid;
        return Db
          .vElectionListInfoes
          .SelectMany(e => Db.JoinElectionUsers.Where(j => j.UserId == userGuid),
                      (e, j) => new {e, j})
          .Where(joined => joined.j.ElectionGuid.Equals(joined.e.ElectionGuid))
          .Select(joined => joined.e)
          .ToList();
      }

      var currentElection = UserSession.CurrentElection;
      if (UserSession.IsGuestTeller && currentElection!=null)
      {
        return new List<vElectionListInfo>
          {
            Db.vElectionListInfoes.Single(e=>e.ElectionGuid== currentElection.ElectionGuid)
          };
      }

      return new List<vElectionListInfo>();
    }
  }
}