using System;
using System.Linq;
using System.Web;
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
        return CurrentElectionModel.LocationsForCurrentElection
          .OrderBy(l => l.SortOrder)
          .ThenBy(l => l.C_RowId)
          .Select(l => new
                         {
                           l.Name,
                           l.ContactInfo,
                           l.C_RowId
                         })
          .SerializedAsJsonString();
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
                   rules = rules.SerializedAsJsonString()
                 };
      }
    }

    public HtmlString IneligibleReasonsForSelect()
    {
      var reasons = Db.Reasons.Where(r => r.ReasonGroup == "Ineligible").OrderBy(r => r.SortOrder).ToList();
      reasons.Insert(0, new Reason {ReasonGuid = Guid.Empty, ReasonDescription = "-"});

      return reasons
        .Select(r => "<option value='{0}'>{1}</option>".FilledWith(r.ReasonGuid, r.ReasonDescription))
        .JoinedAsString().AsRawHtml();
    }
  }
}