using System;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.CoreModels;

namespace TallyJ.EF
{
  public class VoteInfo
  {
    public VoteInfo(Vote vote, Election election, Ballot ballot, Location location, Person person)
    {
      VoteId = vote.C_RowId;
      SingleNameElectionCount = vote.SingleNameElectionCount;
      PositionOnBallot = vote.PositionOnBallot;
      PersonCombinedInfoInVote = vote.PersonCombinedInfo;
      VoteIneligibleReasonGuid = vote.InvalidReasonGuid;
      VoteStatusCode = vote.StatusCode;

      //ballot
      BallotGuid = ballot.BallotGuid;
      BallotId = ballot.C_RowId;
      BallotStatusCode = ballot.StatusCode;
      C_BallotCode = ballot.C_BallotCode;

      //Location
      LocationId = location.C_RowId;
      //LocationTallyStatus = location.TallyStatus;
      ElectionGuid = location.ElectionGuid;

      if (person != null)
      {
        AssertAtRuntime.That(person.PersonGuid == vote.PersonGuid);
        
        var personCanReceiveVotes = person.CanReceiveVotes.AsBoolean(true);

        PersonId = person.C_RowId;
        PersonFullNameFL = person.FullNameAndArea;
        PersonCombinedInfo = person.CombinedInfo;
        PersonIneligibleReasonGuid = personCanReceiveVotes ? null : person.IneligibleReasonGuid;
        PersonCanReceiveVotes = personCanReceiveVotes;
        PersonGuid = person.PersonGuid;
      }
      else
      {
        PersonCanReceiveVotes = true;
      }
    }

    public VoteInfo()
    {
      // mostly for testing
      VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
    }

    public bool PersonCanReceiveVotes { get; set; }
    public int VoteId { get; set; }
    public string VoteStatusCode { get; set; }
    public int? SingleNameElectionCount { get; set; }
    //public bool? IsSingleNameElection { get; set; }
    public int PositionOnBallot { get; set; }
    public Guid? VoteIneligibleReasonGuid { get; set; }
    public string PersonCombinedInfoInVote { get; set; }
    public string PersonCombinedInfo { get; set; }
    public Guid? PersonGuid { get; set; }
    public int? PersonId { get; set; }
    //public string PersonFullName { get; set; }
    public string PersonFullNameFL { get; set; }
    public Guid? PersonIneligibleReasonGuid { get; set; }

    public Guid BallotGuid { get; set; }
    public int BallotId { get; set; }
    public string BallotStatusCode { get; set; }
    public string C_BallotCode { get; set; }
    public int LocationId { get; set; }
    //public string LocationTallyStatus { get; set; }
    public Guid ElectionGuid { get; set; }
  }
}