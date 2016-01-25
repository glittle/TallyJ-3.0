using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.EF;

namespace Tests.BusinessTests
{

  static class BallotTestHelper
  {
    static int _rowId = 1;
    static Guid _locationGuid;
    static Guid _electionGuid;

    public static ITallyJDbContext Db { get; private set; }

    public static void SaveElectionGuidForTests(Guid guid) {
      _electionGuid = guid;
    }

    public static void ForTests(this ITallyJDbContext context) {
      Db = context;
    }

    public static Election ForTests(this Election election)
    {
      election.ElectionGuid = _electionGuid;
      Db.Election.Add(election);
      ItemKey.CurrentElection.SetInPageItems(election);

      new Location().ForTests();
      return election;
    }
    public static void ForTests(this Location location) {
      _locationGuid = location.LocationGuid = Guid.NewGuid();
      location.ElectionGuid = _electionGuid;
      Db.Location.Add(location);
      SessionKey.CurrentLocationGuid.SetInSession(_locationGuid);
    }


    public static Person ForTests(this Person person)
    {
      person.PersonGuid = Guid.NewGuid();
      person.CombinedInfo = "abc";
      person.CanVote = true;
      person.CanReceiveVotes = true;
      person.C_RowId = _rowId++;
      person.ElectionGuid = _electionGuid;
      Db.Person.Add(person);
      return person;
    }
    public static Vote ForTests(this Vote vote, Person person, Ballot ballot, string status = VoteHelper.VoteStatusCode.Ok)
    {
      vote.PersonGuid = person.PersonGuid;
      vote.PersonCombinedInfo = person.CombinedInfo;
      vote.InvalidReasonGuid = person.IneligibleReasonGuid;
      vote.BallotGuid = ballot.BallotGuid;
      vote.StatusCode = status;
      vote.C_RowId = _rowId++;
      Db.Vote.Add(vote);
      return vote;
    }
    public static Vote ForTests(this Vote vote, Guid ineligibleReasonGuid, Ballot ballot, string status = VoteHelper.VoteStatusCode.Ok)
    {
      vote.InvalidReasonGuid = ineligibleReasonGuid;
      vote.BallotGuid = ballot.BallotGuid;
      vote.StatusCode = status;
      vote.C_RowId = _rowId++;
      Db.Vote.Add(vote);
      return vote;
    }
    public static Vote Status(this Vote vote, Guid ineligible)
    {
      vote.InvalidReasonGuid = ineligible;
      return vote;
    }

    public static Ballot ForTests(this Ballot ballot, BallotStatusEnum status)
    {
      ballot.BallotGuid = Guid.NewGuid();
      ballot.C_RowId = _rowId++;
      ballot.StatusCode = status;
      ballot.LocationGuid = _locationGuid;
      Db.Ballot.Add(ballot);
      return ballot;
    }
    public static Ballot ForTests(this Ballot ballot)
    {
      return ballot.ForTests(BallotStatusEnum.Ok);
    }

    public static ResultSummary ForTests(this ResultSummary resultSummary) {
      resultSummary.ElectionGuid = _electionGuid;
      resultSummary.C_RowId = _rowId++;
      Db.ResultSummary.Add(resultSummary);
      return resultSummary;
    }
  }
}
