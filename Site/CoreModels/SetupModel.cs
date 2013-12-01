using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class SetupModel : DataConnectedModel
  {
    private Election _election;
    private ElectionModel _electionModel;

    public int NumberOfPeople
    {
      get { return Db.Person.Count(p => p.ElectionGuid == UserSession.CurrentElectionGuid); }
    }

    public string LocationsJson
    {
      get
      {
        return ContextItems.LocationModel.Locations
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
      get { return _election ?? (_election = UserSession.CurrentElection); }
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

    public object TellersJson
    {
      get
      {
        var tellerHelper = new TellerHelper();
        return tellerHelper.Tellers
          .OrderBy(l => l.Name)
          .ThenBy(l => l.C_RowId)
          .Select(l => new
            {
              l.Name,
              l.C_RowId
            })
          .SerializedAsJsonString();
      }
    }

    public bool HasBallots
    {
      get { return Ballot.AllBallotsCached.Any(); }
    }

    public string InvalidReasonsJsonString()
    {
      return IneligibleReasonEnum.Items
        .Select(r => new
          {
            Guid = r.Value,
            r.Group,
            Desc = r.Description
          }).SerializedAsJsonString();
    }

    //public HtmlString IneligibleReasonsForSelect()
    //{
    //  var reasons = IneligibleReasonEnum.Items.ToList();

    //  //var reasons = Db.Reasons.Where(r => r.ReasonGroup == BallotModelCore.ReasonGroupIneligible).OrderBy(r => r.SortOrder).ToList();
    //  reasons.Insert(0, new IneligibleReasonEnum(Guid.Empty, "", "-"));

    //  return reasons
    //    .Select(r => "<option value='{0}'>{1}</option>".FilledWith(r.Value, r.Description))
    //    .JoinedAsString().AsRawHtml();
    //}
  }
}