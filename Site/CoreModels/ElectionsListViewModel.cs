using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
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
        //                var locationModel = ContextItems.LocationModel;
        //                var locations = locationModel.Locations
        //                                             .OrderBy(l => l.SortOrder)
        //                                             .Select(
        //                                                 l =>
        //                                                 new
        //                                                     {
        //                                                         l.Name,
        //                                                         l.C_RowId,
        //                                                         IsCurrent = l.LocationGuid == UserSession.CurrentLocationGuid
        //                                                     });

        var list = MyElections()
          .OrderBy(e => e.ShowAsTest.AsBoolean())
          .ThenByDescending(e => e.DateOfElection)
          .ThenBy(e => e.Name)
          .Select(e => new
          {
            e.Name,
            e.ElectionGuid,
            e.DateOfElection,
            e.ElectionType,
            e.ElectionMode,
            e.ShowAsTest
          }).ToList();

        var electionGuids = list.Select(e => e.ElectionGuid).ToList();

        var personCount = Db.Person.Where(p => electionGuids.Contains(p.ElectionGuid))
          .GroupBy(p => p.ElectionGuid)
          .Select(g => new { ElectionGuid = g.Key, Num = g.Count() })
          .ToList();

        return list.Select(info =>
                      {
                        var isCurrent = info.ElectionGuid == UserSession.CurrentElectionGuid;
                        var personCounts = personCount.SingleOrDefault(c => c.ElectionGuid == info.ElectionGuid);
                        return new
                              {
                                info.Name,
                                info.ElectionGuid,
                                DateOfElection =
                                    info.DateOfElection.HasValue
                                        ? info.DateOfElection.AsString("yyyy-MMM-dd")
                                        : "",
                                IsCurrent = isCurrent,
                                // Locations = isCurrent ? locations : null,
                                Type = ElectionTypeEnum.TextFor(info.ElectionType),
                                Mode =
                                    ElectionModeEnum.TextFor(info.ElectionMode).SurroundContentWith(" (", ")"),
                                IsTest = info.ShowAsTest.AsBoolean(),
                                NumVoters = personCounts == null ? 0 : personCounts.Num
                              };
                      });
      }
    }

    private IEnumerable<Election> MyElections()
    {
      if (UserSession.IsKnownTeller)
      {
        var userGuid = UserSession.UserGuid;
        return Db.Election
          .SelectMany(e => Db.JoinElectionUser.Where(j => j.UserId == userGuid),
            (e, j) => new { e, j })
          .Where(joined => joined.j.ElectionGuid.Equals(joined.e.ElectionGuid))
          .Select(joined => joined.e)
          .ToList();
      }

      var currentElection = UserSession.CurrentElection;
      if (UserSession.IsGuestTeller && currentElection != null)
      {
        return new List<Election> { currentElection };
      }

      return new List<Election>();
    }

    public string VisibleElectionsOptions()
    {
      const string template = "<option value=\"{0}\">{1}</option>";
      var visibleElections = new ElectionCacher().PublicElections;
      var listing = visibleElections.OrderBy(e => e.Name).Select(x => template.FilledWith(x.C_RowId, x.Name)).JoinedAsString();
      return listing
        .DefaultTo(template.FilledWith(0, "(Sorry, no elections are active right now.)"));
    }

  }
}