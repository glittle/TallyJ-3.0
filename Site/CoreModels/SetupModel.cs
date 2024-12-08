using System.Collections.Generic;
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
    private ElectionHelper _electionHelper;

    public int NumberOfPeople
    {
      get { return new PersonCacher(Db).AllForThisElection.Count(); }
    }

    public string LocationsJson
    {
      get
      {
        return ContextItems.LocationModel.GetLocations_Physical()
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
      get { return _election ??= UserSession.CurrentElection; }
    }

    /// <summary>
    ///  child and sibling elections. For 2 stage and tie-break elections
    /// </summary>
    public object ChildAndSiblingElectionInfo
    {
      get
      {
        return Db.Election
          .Where(e => e.ParentElectionGuid == CurrentElection.ElectionGuid || e.ParentElectionGuid != null && e.ParentElectionGuid == CurrentElection.ParentElectionGuid)
          .ToList()
          .Select(e => new { Name = e.UnitName ?? e.Name, guid = e.ElectionGuid, e.ElectionType })
          .OrderBy(e => e.Name)
          .ToList();
      }
    }
    public object ParentElectionInfo
    {
      get
      {
        if (CurrentElection.ParentElectionGuid != null)
        {
          return Db.Election
            .Where(e => e.ElectionGuid == CurrentElection.ParentElectionGuid)
            .Select(e => new { e.Name, guid = e.ElectionGuid, e.ElectionType })
            .FirstOrDefault();
        }

        return null;
      }
    }

    public ElectionHelper CurrentElectionHelper
    {
      get { return _electionHelper ??= new ElectionHelper(); }
    }

    public object RulesForCurrentElection
    {
      get
      {
        var currentElection = CurrentElection;
        var rules = ElectionHelper.GetRules(currentElection.ElectionType, currentElection.ElectionMode);

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
        return new TellerCacher(Db).AllForThisElection
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

    public bool HasBallots => new BallotCacher(Db).AllForThisElection.Any();
    public bool HasOnlineBallots => Db.OnlineVotingInfo
      .Any(ovi => ovi.ElectionGuid == UserSession.CurrentElectionGuid && ovi.Status == OnlineBallotStatusEnum.Processed);


  }
}