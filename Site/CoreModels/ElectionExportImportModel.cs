using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ElectionExportImportModel : DataConnectedModel
  {
    private readonly Guid _electionGuid;
    private Election _election;

    public ElectionExportImportModel(Guid electionGuid)
    {
      _electionGuid = electionGuid;
    }

    public Exporter Export()
    {
      // collect all the info
      _election = Db.Elections.SingleOrDefault(e => e.ElectionGuid == _electionGuid);

      if (_election == null) return null;

      var locations = Db.Locations.Where(l => l.ElectionGuid == _electionGuid);
      var computers = Db.Computers.Where(c => c.ElectionGuid == _electionGuid);

      var people = Db.People.Where(p => p.ElectionGuid == _electionGuid);
      var tellers = Db.Tellers.Where(t => t.ElectionGuid == _electionGuid);
      var results = Db.Results.Where(r => r.ElectionGuid == _electionGuid);
      var resultSummaries = Db.ResultSummaries.Where(r => r.ElectionGuid == _electionGuid);
      var resultTies = Db.ResultTies.Where(r => r.ElectionGuid == _electionGuid);
      var logs = Db.C_Log.Where(log => log.ElectionGuid == _electionGuid);

      var joinElectionUsers = Db.JoinElectionUsers.Where(j => j.ElectionGuid == _electionGuid);
      var users = Db.Users.Where(u => joinElectionUsers.Select(j => j.UserId).Contains(u.UserId));

      var ballots = Db.Ballots.Where(b => locations.Select(l => l.LocationGuid).Contains(b.LocationGuid));
      var votes = Db.Votes.Where(v => ballots.Select(b => b.BallotGuid).Contains(v.BallotGuid));

      var site = new SiteInfo();

      var blob = new
        {
          Exported = DateTime.Now.ToString("o"),
          ByUser = UserSession.UserGuid,
          Server = site.ServerName,
          Environment = site.CurrentEnvironment,

          election = ExportElection(_election),
          resultSummary = ExportResultSummaries(resultSummaries),
          result = ExportResults(results),
          resultTie = ExportResultTies(resultTies),
          teller = ExportTellers(tellers),
          user = ExportUsers(users),
          location = ExportLocationComputerBallotVote(locations, computers, ballots, votes, logs),
          person = ExportPeople(people),
          reason = ExportReasons(),
          //log = ExportLogs(logs)
        };

      var exportName = string.Format("{0} {1}.TallyJ", _election.DateOfElection.GetValueOrDefault(DateTime.Today).ToString("yyyy-MM-dd"), _election.Name);

      return new Exporter(blob, "TallyJ2", exportName);
    }

    private IList ExportUsers(IQueryable<User> users)
    {
      return users.OrderBy(u => u.UserName).Select(u => new
        {
          u.UserName,
          u.LastActivityDate
        }).ToList();
    }

    private IList ExportReasons()
    {
      return IneligibleReasonEnum.Items.OrderBy(i => i.DisplayText).ThenBy(i => i.Description).Select(i => new
        {
          i.Value,
          i.DisplayText,
          i.Description
        }).ToList();
    }

    private IList ExportLogs(IQueryable<C_Log> logs)
    {
      return logs.OrderBy(l => l.AsOf)
        .Select(l => new
          {
            l.AsOf,
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
        .Select(p => new
          {
            p.PersonGuid,
            p.FirstName,
            p.LastName,
            p.OtherNames,
            p.OtherLastNames,
            p.OtherInfo,
            p.BahaiId,
            p.AgeGroup,
            p.Area,
            CanReceiveVotes = p.CanReceiveVotes.Value ? null : (bool?)false,
            CanVote = p.CanVote.Value ? null : (bool?)false,
            p.IneligibleReasonGuid,
            p.VotingMethod,
            p.EnvNum,
            p.RegistrationTime,
            p.VotingLocationGuid,
            p.TellerAtKeyboard,
            p.TellerAssisting,
            ChangedAfterLoad = (p.CombinedInfoAtStart != p.CombinedInfo) ? (bool?)true : null
          }).ToList();
    }

    private IList ExportTellers(IQueryable<Teller> tellers)
    {
      return tellers.Select(t => new
        {
          t.Name,
          IsHeadTeller = t.IsHeadTeller.Value ? (bool?)true : null,
          t.TellerGuid,
        }).ToList();
    }

    private IList ExportLocationComputerBallotVote(IQueryable<Location> locations, IQueryable<Computer> computers, IQueryable<Ballot> ballots, IQueryable<Vote> votes, IQueryable<C_Log> logs)
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
            //location.LocationGuid,
            computer = ExportComputer(computers.Where(c => c.LocationGuid == location.LocationGuid)),
            ballot = ExportBallotVotes(ballots.Where(b => b.LocationGuid == location.LocationGuid), votes),
            log = ExportLogs(logs.Where(l => l.LocationGuid == location.LocationGuid))
          }).ToList();
    }

    private IList ExportComputer(IQueryable<Computer> computers)
    {
      return computers
        .OrderBy(computer => computer.ComputerCode)
        .ToList()
        .Select(computer => new
          {
            computer.ComputerCode,
            computer.BrowserInfo,
            computer.ComputerInternalCode,
          }).ToList();
    }

    private IList ExportBallotVotes(IQueryable<Ballot> ballots, IQueryable<Vote> votes)
    {
      return ballots
        .OrderBy(ballot => ballot.ComputerCode)
        .ThenBy(ballot => ballot.BallotNumAtComputer)
        .ToList()
        .Select(ballot => new
          {
            BallotCode = ballot.C_BallotCode,
            ballot.StatusCode,
            ballot.BallotNumAtComputer,
            ballot.TellerAtKeyboard,
            ballot.TellerAssisting,
            vote = ExportVotes(votes.Where(v => v.BallotGuid == ballot.BallotGuid))
          }).ToList();
    }

    private IList ExportVotes(IQueryable<Vote> votes)
    {
      return votes
        .OrderBy(v => v.PositionOnBallot)
        .ThenBy(v => v.StatusCode) // for single name elections, position may be all 1, so group together by status
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
        .Select(r => new
          {
            r.Rank,
            r.Section,
            r.RankInExtra,
            r.VoteCount,
            r.PersonGuid,
            IsTied = r.IsTied.Value == false ? null : (bool?)true,
            IsTieResolved = r.IsTied.Value ? r.IsTieResolved : null,
            r.TieBreakGroup,
            TieBreakRequired = r.TieBreakRequired.Value == false ? null : (bool?)true,
            r.TieBreakCount,
            ForceShowInOther = r.ForceShowInOther.Value == false ? null : (bool?)true,
            CloseToNext = r.CloseToNext.Value == false ? null : (bool?)true,
            CloseToPrev = r.CloseToPrev.Value == false ? null : (bool?)true
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
            TieBreakRequired = rt.TieBreakRequired == false ? null : rt.TieBreakRequired,
            NumToElect = rt.NumToElect == 0 ? null : (int?)rt.NumToElect,
          }).ToList();
    }

    private IList ExportResultSummaries(IQueryable<ResultSummary> resultSummaries)
    {
      return resultSummaries.Select(rs => new
        {
          rs.ResultType,
          rs.NumVoters,
          rs.NumEligibleToVote,
          rs.BallotsReceived,
          rs.InPersonBallots,
          rs.DroppedOffBallots,
          rs.MailedInBallots,
          rs.CalledInBallots,
          rs.BallotsNeedingReview,
          rs.SpoiledBallots,
          rs.SpoiledVotes,
          rs.TotalVotes,
          rs.UseOnReports
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
          IsSingleNameElection = election.IsSingleNameElection == false ? null : (bool?)true,
          ShowAsTest = election.ShowAsTest == null || election.ShowAsTest.Value == false ? null : (bool?)true,
          ListForPublic = election.ListForPublic == null || election.ListForPublic.Value == false ? null : (bool?)true,
          election.ListedForPublicAsOf,
          election.ElectionPasscode,
          election.CanVote,
          election.CanReceive,
          //election.ElectionGuid,
          election.LastEnvNum,
          election.OwnerLoginId,
          ShowFullReport = election.ShowFullReport == null || election.ShowFullReport.Value == false ? null : (bool?)true
        };
    }
  }

  public class Exporter : ActionResult
  {
    private readonly object _blob;
    private readonly string _rootName;
    private readonly string _exportName;
    private XmlWriter _writer;

    public Exporter(object blob, string rootName, string exportName)
    {
      _blob = blob;
      _rootName = rootName;
      _exportName = exportName;
    }

    public override void ExecuteResult(ControllerContext context)
    {
      var response = context.HttpContext.Response;
      response.ClearContent();
      response.ContentType = "text/xml";
      response.AddHeader("Content-Disposition", string.Format("attachment; filename={0}.xml", _exportName));

      var settings = new XmlWriterSettings
        {
          Indent = true,
          Encoding = Encoding.UTF8,
          ConformanceLevel = ConformanceLevel.Document,
          CheckCharacters = true
        };
      _writer = XmlWriter.Create(response.OutputStream, settings);
      _writer.WriteStartDocument();

      AddItem(_blob, _rootName);

      _writer.WriteEndDocument();

      _writer.Flush();
      _writer = null;
    }

    private void AddItem(object blob, string name)
    {
      var started = false;

      foreach (
        var property in
          blob.GetAllProperties().Where(property => property.Value != null && property.Value.ToString() != string.Empty)
        )
      {
        if (!started)
        {
          _writer.WriteStartElement(name);
          started = true;
        }

        var value = property.Value;
        if (value is String || value is Int32 || value is DateTime || value is Guid)
        {
          _writer.WriteAttributeString(property.Key, value.ToString());
        }
        else if (value is Boolean)
        {
          _writer.WriteAttributeString(property.Key, (bool)value ? "true" : "false");
        }
        else if (value is IList)
        {
          foreach (var item in (IList)value)
          {
            AddItem(item, property.Key);
          }
        }
        else
        {
          AddItem(value, property.Key);
        }
      }

      if (started)
      {
        _writer.WriteEndElement();
      }
    }
  }

  public static class LocalExtensions
  {
    public static IList FilterNulls<T>(this IEnumerable<T> list)
    {
      return list.Select(FilterNullProperties).ToList();
    }

    public static object NullIfEquals(this bool? input, bool defaultValue)
    {
      if (input.HasValue)
      {
        return input.Value.NullIfEquals(defaultValue);
      }
      return null;
    }

    public static object NullIfEquals(this bool input, bool defaultValue)
    {
      if (input == defaultValue)
      {
        return null;
      }
      return input;
    }

    private static object FilterNullProperties<T>(T o)
    {
      var target = Activator.CreateInstance<T>();
      foreach (var keyValuePair in o.GetAllProperties().Where(keyValuePair => keyValuePair.Value != null))
      {
        target.SetPropertyValue(keyValuePair.Key, keyValuePair.Value);
      }
      return target;
    }
  }
}