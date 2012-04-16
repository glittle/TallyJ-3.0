using System;
using System.Linq;
using System.Web;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class SetupModel : DataConnectedModel
  {
    private ElectionModel _electionModel;
    private LocationModel _locationModel;
    private Election _election;

    public int NumberOfPeople
    {
      get { return Db.People.Count(p => p.ElectionGuid == UserSession.CurrentElectionGuid); }
    }

    public string LocationsJson
    {
      get
      {
        return CurrentLocationModel.LocationsForCurrentElection
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

    public LocationModel CurrentLocationModel
    {
      get { return _locationModel ?? (_locationModel = new LocationModel()); }
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