using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class SetupModel : DataConnectedModel
  {
    private ElectionModel _electionModel;

    public int NumberOfPeople
    {
      get { return Db.People.Count(p => p.ElectionGuid == UserSession.CurrentElectionGuid); }
    }

    public string LocationsJson
    {
      get
      {
        return CurrentElectionModel.Locations
          .OrderBy(l => l.SortOrder)
          .ThenBy(l => l.C_RowId)
          .Select(l => new
                         {
                           l.Name,
                           l.ContactInfo,
                           l.C_RowId
                         })
          .SerializedAsJson();
      }
    }

    public Election CurrentElection
    {
      get { return Db.Elections.Single(e => e.ElectionGuid == UserSession.CurrentElectionGuid); }
    }

    public ElectionModel CurrentElectionModel
    {
      get { return _electionModel ?? (_electionModel = new ElectionModel()); }
    }

    public object RulesForCurrentElection
    {
      get
      {
        var currentElection = CurrentElection;
        var rules = CurrentElectionModel.GetRules(currentElection.ElectionType, currentElection.ElectionMode);

        return new
                 {
                   type = currentElection.ElectionType,
                   mode = currentElection.ElectionMode,
                   rules = rules.SerializedAsJson()
                 };
      }
    }
  }
}