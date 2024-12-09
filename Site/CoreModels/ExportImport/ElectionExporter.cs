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
      var voters = Db.Voter.Where(p => p.ElectionGuid == _electionGuid);
      var tellers = Db.Teller.Where(t => t.ElectionGuid == _electionGuid);
      var results = Db.Result.Where(r => r.ElectionGuid == _electionGuid);
      var resultSummaries = Db.ResultSummary.Where(r => r.ElectionGuid == _electionGuid);
      var resultTies = Db.ResultTie.Where(r => r.ElectionGuid == _electionGuid);
      var logs = Db.C_Log.Where(log => log.ElectionGuid == _electionGuid);
      var onlineVoterInfo = Db.OnlineVotingInfo.Where(ovi => ovi.ElectionGuid == _electionGuid);

      var joinElectionUsers = Db.JoinElectionUser.Where(j => j.ElectionGuid == _electionGuid);
      var users = Db.Users.Where(u => joinElectionUsers.Select(j => j.UserId).Contains(u.UserId));

      var ballots = Db.Ballot.Where(b => locations.Select(l => l.LocationGuid).Contains(b.LocationGuid));
      var votes = Db.Vote.Where(v => ballots.Select(b => b.BallotGuid).Contains(v.BallotGuid));

      var site = new SiteInfo();


      var blob = new
      {
        Exported = DateTime.UtcNow.ToString("o"),
        ByUser = UserSession.MemberName,
        UserEmail = UserSession.MemberEmail,
        Server = site.ServerName,
        Version = UserSession.SiteVersion,
        Environment = site.CurrentEnvironment,
        // elements
        election = ExportElection(_election),
        resultSummary = ExportResultSummaries(resultSummaries),
        result = ExportResults(results),
        resultTie = ExportResultTies(resultTies),
        teller = ExportTellers(tellers),
        user = ExportUsers(users),
        onlineVoterInfo = ExportOnlineVoterInfos(onlineVoterInfo),
        location = ExportLocationBallotVote(locations, ballots, votes, logs),
        person = ExportPeople(people),
        personVoters = ExportVoters(voters),
        reason = ExportReasons(),
        log = ExportLogs(logs.Where(l => l.LocationGuid == null))
      };

      var date = _election.DateOfElection.HasValue ? _election.DateOfElection.Value.AsUtc().ToString("yyyy-MM-dd") : "NoDate";
      var exportName = $"{date} {_election.Name}.TallyJ";

      return new Exporter(blob, "TallyJ2", exportName);
    }

    private IList ExportUsers(IQueryable<Users> users)
    {
      //TODO - include Role?
      return users.OrderBy(u => u.UserName)
        .ToList()
        .Select(u => new
        {
          u.UserName,
          LastActivityDate = u.LastActivityDate.AsUtc().ToString("o")
        }).ToList();
    }

    private IList ExportOnlineVoterInfos(IQueryable<OnlineVotingInfo> onlineVotingInfos)
    {
      return onlineVotingInfos.OrderBy(u => u.PersonGuid)
        .ToList()
        .Select(ovi => new
        {
          ovi.PersonGuid,
          ovi.PoolLocked,
          ovi.Status,
          WhenStatus = ovi.WhenStatus.AsUtc()?.ToString("o"),
          WhenBallotCreated = ovi.WhenBallotCreated.AsUtc()?.ToString("o"),
          // ovi.ListPool, --> do not export the pool!
          ovi.HistoryStatus,
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
          AsOf = l.AsOf.AsUtc().ToString("o"),
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
          // following the SQL order for easier comparison
          p.PersonGuid,
          LastName = p.LastName.DefaultTo(""),
          p.FirstName,
          p.OtherLastNames,
          p.OtherNames,
          p.OtherInfo,
          p.Area,
          p.BahaiId,
          p.CombinedInfo,
          p.CombinedSoundCodes, // now used for extra fake columns
          p.CombinedInfoAtStart,
          p.AgeGroup,
          p.Email,
          p.Phone,
          p.UnitName,
          Changed = (p.CombinedInfoAtStart != p.CombinedInfo).OnlyIfTrue(),
        }).ToList();
    }

    private IList ExportVoters(IQueryable<Voter> personVoters)
    {
      return personVoters
        .OrderBy(p => p.PersonGuid)
        .ToList()
        .Select(p => new
        {
          // following the SQL order for easier comparison
          p.PersonGuid,
          CanVote = p.CanVote.OnlyIfFalse(),
          CanReceiveVotes = p.CanReceiveVotes.OnlyIfFalse(),
          p.IneligibleReasonGuid,
          RegistrationTime = p.RegistrationTime.AsUtc().AsString("o").OnlyIfHasContent(),
          p.VotingLocationGuid,
          p.VotingMethod,
          p.EnvNum,
          p.Teller1,
          p.Teller2,
          p.HasOnlineBallot,
          p.Flags,
          p.RegLog
          // p.KioskCode, -- ephemeral, don't export
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
        EnvelopesOnline = rs.OnlineBallots,
        EnvelopesImported = rs.ImportedBallots,
        EnvelopesCustom1 = rs.Custom1Ballots,
        EnvelopesCustom2 = rs.Custom2Ballots,
        EnvelopesCustom3 = rs.Custom3Ballots,
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
        // following the SQL order for easier comparison
        election.ElectionGuid,
        election.Name,
        election.Convenor,
        DateOfElection = election.DateOfElection.AsUtc().AsString("u").Split(' ')[0],
        election.ElectionType,
        election.ElectionMode,
        election.NumberToElect,
        election.NumberExtra,
        election.LastEnvNum,
        election.TallyStatus,
        ShowFullReport = election.ShowFullReport.OnlyIfTrue(),
        election.OwnerLoginId,
        election.ElectionPasscode,
        // election.ListedForPublicAsOf, -- only has use if currently loaded
        // _RowVersion
        ListForPublic = election.ListForPublic.OnlyIfTrue(),
        ShowAsTest = election.ShowAsTest.OnlyIfTrue(),
        UseCallInButton = election.UseCallInButton.OnlyIfTrue(),
        HidePreBallotPages = election.HidePreBallotPages.OnlyIfTrue(),
        MaskVotingMethod = election.MaskVotingMethod.OnlyIfTrue(),
        OnlineWhenOpen = election.OnlineWhenOpen.AsUtc(),
        OnlineWhenClose = election.OnlineWhenClose.AsUtc(),
        election.OnlineCloseIsEstimate,
        election.OnlineSelectionProcess,
        OnlineAnnounced = election.OnlineAnnounced.AsUtc().AsString("u").Split(' ')[0],
        election.EmailFromAddress,
        election.EmailFromName,
        election.EmailText,
        election.SmsText,
        election.EmailSubject,
        election.CustomMethods,
        VotingMethods = election.VotingMethodsAdjusted,
        election.Flags,
        election.ParentElectionGuid,
        election.PeopleElectionGuid,
        election.ParentTieBreakGroup,
        election.UnitName,
        election.TieBreakPersonGuids,
      };
    }
  }
}