using System;
using System.Collections;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.ExportImport
{
  public class ElectionExporter : DataConnectedModel
  {
    private readonly Guid _electionGuid;
    private Election _election;

    public ElectionExporter(Guid electionGuid)
    {
      _electionGuid = electionGuid;
    }

    public Exporter Export()
    {
      // collect all the info
      _election = Db.Election.SingleOrDefault(e => e.ElectionGuid == _electionGuid);

      var logger = new LogHelper(_electionGuid);
      logger.Add("Save to file started");

      if (_election == null) return null;

      // don't use Cached versions - this may not be for the current election

      var locations = Db.Location.Where(l => l.ElectionGuid == _electionGuid);
      //var computers = Db.Computer.Where(c => c.ElectionGuid == _electionGuid);

      var people = Db.Person.Where(p => p.ElectionGuid == _electionGuid);
      var tellers = Db.Teller.Where(t => t.ElectionGuid == _electionGuid);
      var results = Db.Result.Where(r => r.ElectionGuid == _electionGuid);
      var resultSummaries = Db.ResultSummary.Where(r => r.ElectionGuid == _electionGuid);
      var resultTies = Db.ResultTie.Where(r => r.ElectionGuid == _electionGuid);
      var logs = Db.C_Log.Where(log => log.ElectionGuid == _electionGuid);

      var joinElectionUsers = Db.JoinElectionUser.Where(j => j.ElectionGuid == _electionGuid);
      var users = Db.Users.Where(u => joinElectionUsers.Select(j => j.UserId).Contains(u.UserId));

      var ballots = Db.Ballot.Where(b => locations.Select(l => l.LocationGuid).Contains(b.LocationGuid));
      var votes = Db.Vote.Where(v => ballots.Select(b => b.BallotGuid).Contains(v.BallotGuid));

      var site = new SiteInfo();

      var blob = new
      {
        Exported = DateTime.Now.ToString("o"),
        ByUser = UserSession.MemberName,
        UserEmail = UserSession.MemberEmail,
        Server = site.ServerName,
        Environment = site.CurrentEnvironment,
        // elements
        election = ExportElection(_election),
        resultSummary = ExportResultSummaries(resultSummaries),
        result = ExportResults(results),
        resultTie = ExportResultTies(resultTies),
        teller = ExportTellers(tellers),
        user = ExportUsers(users),
        location = ExportLocationBallotVote(locations, ballots, votes, logs),
        person = ExportPeople(people),
        reason = ExportReasons(),
        //log = ExportLogs(logs)
      };

      var exportName = string.Format("{0} {1}.TallyJ",
        _election.DateOfElection.GetValueOrDefault(DateTime.Today)
          .ToString("yyyy-MM-dd"),
        _election.Name);

      return new Exporter(blob, "TallyJ2", exportName);
    }

    private IList ExportUsers(IQueryable<Users> users)
    {
      return users.OrderBy(u => u.UserName)
        .ToList()
        .Select(u => new
        {
          u.UserName,
          LastActivityDate = u.LastActivityDate.ToString("o")
        }).ToList();
    }

    private IList ExportReasons()
    {
      return IneligibleReasonEnum.Items.OrderBy(i => i.DisplayText).ThenBy(i => i.Description).Select(i => new
      {
        i.Value,
        i.DisplayText,
        i.Description,
        i.CanVote,
        i.CanReceiveVotes
      }).ToList();
    }

    private IList ExportLogs(IQueryable<C_Log> logs)
    {
      return logs.OrderBy(l => l.AsOf)
        .ToList()
        .Select(l => new
        {
          AsOf = l.AsOf.ToString("o"),
          //l.LocationGuid,
          l.ComputerCode,
          l.Details,
        }).ToList();
    }

    private IList ExportPeople(IQueryable<Person> people)
    {
      return people
        .OrderBy(p => p.LastName)
        .ThenBy(p => p.FirstName)
        .ToList()
        .Select(p => new
        {
          p.PersonGuid,
          p.FirstName,
          LastName = p.LastName.DefaultTo(""),
          p.OtherNames,
          p.OtherLastNames,
          p.OtherInfo,
          p.BahaiId,
          p.CombinedInfoAtStart,
          p.AgeGroup,
          p.Area,
          CanReceiveVotes = p.CanReceiveVotes.OnlyIfFalse(),
          CanVote = p.CanVote.OnlyIfFalse(),
          p.IneligibleReasonGuid,
          p.VotingMethod,
          p.EnvNum,
          RegistrationTime = p.RegistrationTime.AsString("o").OnlyIfHasContent(),
          p.VotingLocationGuid,
          TellerAtKeyboard = p.Teller1,
          TellerAssisting = p.Teller2,
          Changed = (p.CombinedInfoAtStart != p.CombinedInfo).OnlyIfTrue(),
        }).ToList();
    }

    private IList ExportTellers(IQueryable<Teller> tellers)
    {
      return tellers.ToList().Select(t => new
      {
        t.Name,
        IsHeadTeller = t.IsHeadTeller.OnlyIfTrue(),
//        t.TellerGuid,
      }).ToList();
    }

    private IList ExportLocationBallotVote(IQueryable<Location> locations,
      IQueryable<Ballot> ballots, IQueryable<Vote> votes,
      IQueryable<C_Log> logs)
    {
      return locations
        .OrderBy(l => l.SortOrder)
        .ToList()
        .Select(location => new
        {
          location.Name,
          location.TallyStatus,
          location.BallotsCollected,
          location.ContactInfo,
          location.Lat,
          location.Long,
          location.LocationGuid,
          //computer = ExportComputer(computers.Where(c => c.LocationGuid == location.LocationGuid)),
          ballot = ExportBallotVotes(ballots.Where(b => b.LocationGuid == location.LocationGuid), votes),
          log = ExportLogs(logs.Where(l => l.LocationGuid == location.LocationGuid))
        }).ToList();
    }

//    private IList ExportComputer(IQueryable<Computer> computers)
//    {
//      return computers
//        .OrderBy(computer => computer.ComputerCode)
//        .ToList()
//        .Select(computer => new
//        {
//          computer.ComputerCode,
//          computer.BrowserInfo,
//          computer.ComputerInternalCode,
//        }).ToList();
//    }

    private IList ExportBallotVotes(IQueryable<Ballot> ballots, IQueryable<Vote> votes)
    {
      return ballots
        .OrderBy(ballot => ballot.ComputerCode)
        .ThenBy(ballot => ballot.BallotNumAtComputer)
        .ToList()
        .Select(ballot => new
        {
          ballot.StatusCode,
          ballot.ComputerCode,
          ballot.BallotNumAtComputer,
          TellerAtKeyboard = ballot.Teller1,
          TellerAssisting = ballot.Teller2,
          vote = ExportVotes(votes.Where(v => v.BallotGuid == ballot.BallotGuid))
        }).ToList();
    }

    private IList ExportVotes(IQueryable<Vote> votes)
    {
      return votes
        .OrderBy(v => v.PositionOnBallot)
        .ThenBy(v => v.StatusCode)
        // for single name elections, position may be all 1, so group together by status
        .ThenBy(v => v.SingleNameElectionCount)
        .Select(vote => new
        {
          //PositionOnBallot = _election.IsSingleNameElection ? null : (int?)vote.PositionOnBallot,
          vote.StatusCode,
          vote.PersonGuid,
          vote.InvalidReasonGuid,
          SingleNameElectionCount = _election.IsSingleNameElection ? vote.SingleNameElectionCount : null,
        }).ToList();
    }

    private IList ExportResults(IQueryable<Result> results)
    {
      return results
        .OrderBy(r => r.Rank)
        .ToList()
        .Select(r => new
        {
          r.Rank,
          r.Section,
          r.RankInExtra,
          r.VoteCount,
          r.PersonGuid,
          IsTied = r.IsTied.OnlyIfTrue(),
          IsTieResolved = r.IsTied.OnlyIfTrue(),
          r.TieBreakGroup,
          TieBreakRequired = r.IsTied.AsBoolean() ? r.TieBreakRequired : null,
          r.TieBreakCount,
          ForceShowInOther = r.ForceShowInOther.OnlyIfTrue(),
          CloseToNext = r.CloseToNext.OnlyIfTrue(),
          CloseToPrev = r.CloseToPrev.OnlyIfTrue()
        }).ToList();
    }

    private IList ExportResultTies(IQueryable<ResultTie> resultTies)
    {
      return resultTies
        .OrderBy(rt => rt.TieBreakGroup)
        .Select(rt => new
        {
          rt.TieBreakGroup,
          rt.NumInTie,
          rt.IsResolved,
          rt.TieBreakRequired,
          NumToElect = rt.NumToElect == 0 ? null : (int?)rt.NumToElect,
        }).ToList();
    }

    private IList ExportResultSummaries(IQueryable<ResultSummary> resultSummaries)
    {
      return resultSummaries.OrderBy(rs => rs.ResultType).Select(rs => new
      {
        rs.ResultType,
        rs.NumVoters,
        rs.NumEligibleToVote,
        NumBallotsEntered = rs.BallotsReceived,
        EnvelopesInPerson = rs.InPersonBallots,
        EnvelopesDroppedOff = rs.DroppedOffBallots,
        EnvelopesMailedIn = rs.MailedInBallots,
        EnvelopesCalledIn = rs.CalledInBallots,
        rs.BallotsNeedingReview,
        rs.SpoiledBallots,
        rs.SpoiledVotes,
        rs.TotalVotes,
        rs.UseOnReports,
        rs.SpoiledManualBallots
      }).ToList();
    }

    private object ExportElection(Election election)
    {
      return new
      {
        election.TallyStatus,
        election.ElectionType,
        election.ElectionMode,
        election.DateOfElection,
        election.NumberToElect,
        election.NumberExtra,
        election.Name,
        election.Convenor,
        ShowAsTest = election.ShowAsTest.OnlyIfTrue(),
        ListForPublic = election.ListForPublic.OnlyIfTrue(),
        ListedForPublicAsOf = election.ListedForPublicAsOf.AsString("o"),
        election.ElectionPasscode,
        election.CanVote,
        election.CanReceive,
        election.LastEnvNum,
        election.OwnerLoginId,
        ShowFullReport = election.ShowFullReport.OnlyIfTrue()
      };
    }
  }
}