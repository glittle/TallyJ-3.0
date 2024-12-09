using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using EntityFramework.Extensions;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class PeopleModel : DataConnectedModel
  {
    #region FrontDeskSortEnum enum

    public enum FrontDeskSortEnum
    {
      ByArea,
      ByName
    }

    #endregion

    private const int NotSet = -1;
    private const int WantAllLocations = -2;
    private Election _peopleElection;

    private List<Location> _locations;
    private List<Person> _people;
    private readonly string _unit;

    public PeopleModel()
    {
    }

    public PeopleModel(Election peopleElection)
    {
      _peopleElection = peopleElection;
    }

    private Election PeopleElection => _peopleElection ??= UserSession.CurrentPeopleElection;

    private Guid PeopleElectionGuid => PeopleElection.ElectionGuid;

    private IEnumerable<Location> Locations => _locations ??= new LocationCacher(Db).AllForThisElection;

    private List<Person> PeopleInElection => _people ??= new PersonCacher(Db).AllForThisElection;

    private int NumberOfPeople => new PersonCacher(Db).AllForThisElection.Count;

    public List<Person> PeopleWhoCanVote() //, bool includeIneligible = true
    {
      {
        return PeopleInElection
          .Where(p => p.Voter.CanVote.AsBoolean())
          .ToList();
      }
    }


    // /// <summary>
    // ///   Process each person record, preparing it BEFORE the election starts. Altered... too dangerous to wipe information!
    // /// </summary>
    // public void SetInvolvementFlagsToDefault()
    // {
    //   var personCacher = new PersonCacher();
    //   personCacher.DropThisCache();
    //
    //   var peopleInElection = personCacher.AllForThisElection;
    //   var reason = new ElectionModel().GetDefaultIneligibleReason();
    //   var counter = 0;
    //   foreach (var person in peopleInElection)
    //   {
    //     SetInvolvementFlagsToDefault(person, reason);
    //
    //     if (counter++ > 500)
    //     {
    //       Db.SaveChanges();
    //       counter = 0;
    //     }
    //   }
    //
    //   Db.SaveChanges();
    // }

    /// <Summary>Only to be done before an election</Summary>
    public void SetCombinedInfoAtStart(Person person)
    {
      person.CombinedInfoAtStart = person.CombinedInfo = person.MakeCombinedInfo();

      // person.UpdateCombinedSoundCodes();
    }

    public void SetCombinedInfos(Person person)
    {
      person.CombinedInfo = person.MakeCombinedInfo();
      //person.UpdateCombinedSoundCodes();
    }

    /// <summary>
    ///   Ensure the flags match the Guid
    /// </summary>
    /// <param name="people"></param>
    /// <param name="hub"></param>
    /// <param name="personSaver"></param>
    public void EnsureFlagsAreRight(List<Person> people, IStatusUpdateHub hub, Action<DbAction, Person> personSaver)
    {
      hub.StatusUpdate("Reviewing people", true);
      var peopleElectionGuid = UserSession.CurrentElectionGuid;

      var numDone = 0;
      foreach (var person in people)
      {
        var changesMade = false;

        numDone++;
        if (numDone % 10 == 0) hub.StatusUpdate("Reviewed {0} people".FilledWith(numDone), true);

        if (peopleElectionGuid != person.ElectionGuid)
          hub.StatusUpdate("Found unexpected person. Please review. Name: " + person.C_FullName);

        var matchedReason = IneligibleReasonEnum.Get(person.Voter.IneligibleReasonGuid);
        if (person.Voter.IneligibleReasonGuid.HasValue && matchedReason == null)
        {
          personSaver(DbAction.Attach, person);
          person.Voter.IneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Other;

          hub.StatusUpdate("Found unknown ineligible reason. Set to Unknown. Name: " + person.C_FullName);

          changesMade = true;
        }

        if (ApplyVoteReasonFlags(person))
        {
          personSaver(DbAction.Attach, person);
          changesMade = true;
        }
        // var reason = IneligibleReasonEnum.Get(person.IneligibleReasonGuid);
        // if (reason == null)
        // {
        //   unknownIneligibleGuid = true;
        // }
        // else
        // {
        //   var canVote = reason.CanVote;
        //   var canReceiveVotes = reason.CanReceiveVotes;
        //
        //   if (canVote != person.CanVote || canReceiveVotes != person.CanReceiveVotes)
        //   {
        //     personSaver(DbAction.Attach, person);
        //     person.CanVote = canVote;
        //     person.CanReceiveVotes = canReceiveVotes;
        //   }
        // }
        // else
        // {
        //   if (defaultCanVote != person.CanVote || defaultCanReceiveVotes != person.CanReceiveVotes)
        //   {
        //     personSaver(DbAction.Attach, person);
        //     person.CanVote = defaultCanVote;
        //     person.CanReceiveVotes = defaultCanReceiveVotes;
        //     changesMade = true;
        //   }
        // }

        if (changesMade)
        {
          //          personSaver(DbAction.Save, person);
          Db.SaveChanges();
        }
      }

      hub.StatusUpdate("Reviewed {0} people".FilledWith(numDone));
    }

    /// <summary>
    ///   Set person's flag based on what is default for this election
    /// </summary>
    public bool ApplyVoteReasonFlags(Person person, Election election = null)
    {
      election ??= UserSession.CurrentElection;

      // using only what they have
      var changed = false;

      var reason = IneligibleReasonEnum.Get(person.Voter.IneligibleReasonGuid);

      if (reason == null)
      {
        // no reason given
        var canParticipate = !election.IsLsa2 || election.UnitName == person.UnitName;

        if (person.Voter.CanVote.GetValueOrDefault() != canParticipate)
        {
          person.Voter.CanVote = canParticipate;
          changed = true;
        }

        if (person.Voter.CanReceiveVotes.GetValueOrDefault() != canParticipate)
        {
          person.Voter.CanReceiveVotes = canParticipate;
          changed = true;
        }
      }
      else
      {
        // update to match what this reason implies
        if (person.Voter.CanVote != reason.CanVote)
        {
          person.Voter.CanVote = reason.CanVote;
          changed = true;
        }

        if (person.Voter.CanReceiveVotes != reason.CanReceiveVotes)
        {
          person.Voter.CanReceiveVotes = reason.CanReceiveVotes;
          changed = true;
        }
      }

      return changed;
    }


    public JsonResult DetailsFor(int personId)
    {
      var person = PeopleInElection.SingleOrDefault(p => p.C_RowId == personId);

      if (person == null)
        return new
        {
          Error = "Unknown person"
        }.AsJsonResult();

      //var whoCanVote = CurrentElection.CanVote;
      //var whoCanReceiveVotes = CurrentElection.CanReceive;
      // var voteCacher = new VoteCacher(Db);
      // var votedFor = voteCacher.AllForThisElection.Any(v => v.PersonGuid == person.PersonGuid);

      return new
      {
        Person = PersonForEdit(person),
        // CanDelete = person.VotingMethod == null && !votedFor
      }.AsJsonResult();
    }

    private static object PersonForEdit(Person person)
    {
      return new
      {
        person.C_RowId,
        //person.AgeGroup,
        person.BahaiId,
        CanReceiveVotes = person.Voter.CanReceiveVotes,
        CanVote = person.Voter.CanVote,
        person.FirstName,
        person.Voter.IneligibleReasonGuid,
        person.LastName,
        person.OtherInfo,
        person.OtherLastNames,
        person.OtherNames,
        person.Area,
        person.Email,
        person.Phone,
        person.C_FullName,
        person.Voter.VotingMethod,
        person.Voter.RegistrationLog,
        person.UnitName
      };
    }

    public static object PersonForList(Person p, bool isSingleNameElection, List<Vote> votes)
    {
      return new
      {
        Id = p.C_RowId,
        //p.PersonGuid,
        Name = p.C_FullName,
        p.Area,
        p.Email,
        p.Phone,
        V = (p.Voter.CanReceiveVotes.AsBoolean() ? "1" : "0") + (p.Voter.CanVote.AsBoolean() ? "1" : "0"),
        IRG = p.Voter.IneligibleReasonGuid,
        NumVotes = isSingleNameElection
          ? votes.Where(v => v.PersonGuid == p.PersonGuid).Sum(v => v.SingleNameElectionCount ?? 1).AsInt()
          : votes.Count(v => v.PersonGuid == p.PersonGuid)
      };
    }


    public JsonResult SavePerson(Person personFromInput)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();

      var personInDatastore = PeopleInElection.SingleOrDefault(p => p.C_RowId == personFromInput.C_RowId);
      var changed = false;

      if (personInDatastore == null)
      {
        if (personFromInput.C_RowId != -1)
          return new
          {
            Message = "Unknown ID"
          }.AsJsonResult();

        // create new
        personInDatastore = new Person
        {
          PersonGuid = Guid.NewGuid(),
          ElectionGuid = PeopleElectionGuid
        };

        Db.Person.Add(personInDatastore);

        PeopleInElection.Add(personInDatastore);

        changed = true;
      }
      else
      {
        Db.Person.Attach(personInDatastore);
      }

      var beforeChanges = personInDatastore.GetAllProperties();

      if (personFromInput.Voter.IneligibleReasonGuid == Guid.Empty) personFromInput.Voter.IneligibleReasonGuid = null;

      if (personFromInput.Phone != null)
        //check via Twilio to ensure real number?
        if (!new Regex(@"\+[0-9]{4,15}").IsMatch(personFromInput.Phone))
          return new
          {
            Message = "Invalid phone number. Must start with + and only contain digits."
          }.AsJsonResult();


      if (personInDatastore.Voter.VotingMethod == VotingMethodEnum.Online.Value)
      {
        if (personInDatastore.Email != personFromInput.Email
            || personInDatastore.Phone != personFromInput.Phone)
        {
          // can't allow email or phone to be changed if they have already voted online
          return new
          {
            Message = "Cannot change email or phone number after voting online."
          }.AsJsonResult();
        }
      }

      var editableFields = new
      {
        // personFromInput.AgeGroup,
        personFromInput.BahaiId,
        personFromInput.FirstName,
        personFromInput.Voter.IneligibleReasonGuid,
        personFromInput.LastName,
        personFromInput.OtherInfo,
        personFromInput.OtherLastNames,
        personFromInput.OtherNames,
        personFromInput.Area,
        personFromInput.Email,
        personFromInput.Phone,
        personFromInput.UnitName
      }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray();

      changed = personFromInput.CopyPropertyValuesTo(personInDatastore, editableFields) || changed;

      changed = ApplyVoteReasonFlags(personInDatastore) || changed;

      if (changed)
      {
        SetCombinedInfos(personInDatastore);

        try
        {
          Db.SaveChanges();
        }
        catch (Exception e)
        {
          // revert person object back to what it was
          beforeChanges.CopyPropertyValuesTo(personInDatastore);

          if (e.GetAllMsgs(";").Contains("IX_PersonEmail"))
            return new
            {
              Message = "That email is registered with another person.",
              Person = PersonForEdit(personInDatastore)
            }.AsJsonResult();

          if (e.GetAllMsgs(";").Contains("IX_PersonPhone"))
            return new
            {
              Message = "That phone number is registered with another person.",
              Person = PersonForEdit(personInDatastore)
            }.AsJsonResult();

          return new
          {
            e.LastException().Message
          }.AsJsonResult();
        }

        new PersonCacher(Db).ReplaceEntireCache(PeopleInElection);

        UpdateFrontDeskListing(personInDatastore);
      }

      var persons = new PersonCacher(Db).AllForThisElection;
      return new
      {
        Status = "Saved",
        Person = PersonForEdit(personInDatastore),
        OnFile = persons.Count,
        Eligible = persons.Count(p => p.Voter.IneligibleReasonGuid == null) //TODO? split to: can vote, can receive votes
      }.AsJsonResult();
    }

    public JsonResult DeletePerson(int personId)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();

      var person = PeopleInElection.SingleOrDefault(p => p.C_RowId == personId);
      if (person == null)
        return new
        {
          Message = "Unknown person"
        }.AsJsonResult();

      if (person.Voter.VotingMethod != null)
        return new
        {
          Message = "Cannot delete a person who has already voted."
        }.AsJsonResult();

      var voteCacher = new VoteCacher(Db);
      var votedFor = voteCacher.AllForThisElection.Any(v => v.PersonGuid == person.PersonGuid);
      if (votedFor)
        return new
        {
          Message = "Cannot delete a person who has been voted for."
        }.AsJsonResult();

      var onlineVotingInfo = Db.OnlineVotingInfo.SingleOrDefault(o => o.PersonGuid == person.PersonGuid);
      if (onlineVotingInfo != null)
        return new
        {
          Message = "Cannot delete a person who has voted online."
        }.AsJsonResult();

      // TODO does this work?
      var voter = person.Voter;

      // all checks done...

      Db.Person.Attach(person);
      Db.Person.Remove(person);

      if (voter != null)
      {
        Db.Voter.Attach(voter);
        Db.Voter.Remove(voter);
      }

      Db.SaveChanges();

      new PersonCacher(Db).DropThisCache();  // force a reload

      return new
      {
        Success = true
      }.AsJsonResult();
    }


    /// <Summary>Everyone</Summary>
    public IEnumerable<object> FrontDeskPersonLines(FrontDeskSortEnum sortType = FrontDeskSortEnum.ByName)
    {
      return FrontDeskPersonLines(PeopleWhoCanVote());
    }


    private string LocationName(Guid? location)
    {
      if (!location.HasValue) return "";

      var matched = Locations.FirstOrDefault(l => l.LocationGuid == location.Value);
      if (matched == null) return "?";

      return matched.Name;
    }

    public IEnumerable<object> Deselected()
    {
      var hasMultipleLocations = ContextItems.LocationModel.HasMultipleLocations;

      var ballotSources = PeopleInElection // start with everyone
        .Where(
          p =>
            p.Voter.EnvNum.HasValue && string.IsNullOrEmpty(p.Voter.VotingMethod)
            || string.IsNullOrEmpty(p.Voter.VotingMethod) && (p.Voter.Teller1 != null || p.Voter.Teller2 != null))
        .ToList()
        .OrderBy(p => p.Voter.EnvNum)
        .Select(p => new
        {
          PersonId = p.C_RowId,
          C_FullName = p.FullName,
          VotedAt = hasMultipleLocations ? LocationName(p.Voter.VotingLocationGuid) : null,
          // When = p.RegistrationTime.FromSql().AsString("o"),
          RegistrationTime = p.Voter.RegistrationTime.AsUtc(),
          Log = p.Voter.RegistrationLog,
          p.Voter.EnvNum,
          Tellers = ShowTellers(p)
        })
        .ToList();

      return ballotSources;
    }

    public IEnumerable<object> BallotSources(int forLocationId = WantAllLocations)
    {
      if (forLocationId == NotSet) return new List<string>();

      var forLocationGuid = forLocationId == WantAllLocations
        ? Guid.Empty
        : Locations.Where(l => l.C_RowId == forLocationId)
          .Select(l => l.LocationGuid)
          .Single();

      var ballotSources = PeopleInElection // start with everyone
        .Where(p => !string.IsNullOrEmpty(p.Voter.VotingMethod))
        .Where(p => !(p.Voter.VotingMethod == VotingMethodEnum.Online || p.Voter.VotingMethod == VotingMethodEnum.Kiosk || p.Voter.VotingMethod == VotingMethodEnum.Imported))
        .Where(p => forLocationId == WantAllLocations || p.Voter.VotingLocationGuid == forLocationGuid)
        .ToList()
        .OrderBy(p => p.Voter.VotingMethod)
        .ThenByDescending(p => p.Voter.RegistrationTime)
        .Select(p => new
        {
          PersonId = p.C_RowId,
          C_FullName = p.FullName,
          VotedAt = LocationName(p.Voter.VotingLocationGuid),
          RegistrationTime = p.Voter.RegistrationTime.AsUtc(),
          Log = p.Voter.RegistrationLog,
          p.Voter.VotingMethod,
          EnvNum = ShowEnvNum(p),
          Tellers = ShowTellers(p)
        })
        .ToList();

      //var location = ContextItems.LocationModel.HasLocations && forLocationGuid.HasContent()
      //  ? new LocationCacher(Db).AllForThisElection.Single(l => l.LocationGuid == forLocationGuid)
      //  : new LocationCacher(Db).AllForThisElection.Single(l => l.LocationGuid == UserSession.CurrentLocationGuid);

      //      if (location.BallotsCollected.AsInt() == 0)
      //      {
      //        location.BallotsCollected = ballotSources.Count;
      //        Db.SaveChanges();
      //      }

      //      if (location.LocationGuid != UserSession.CurrentLocationGuid)
      //      {
      //        UserSession.CurrentLocationGuid = location.LocationGuid;
      //      }


      return ballotSources;
    }

    public HtmlString GetLocationOptions()
    {
      return Locations
        .OrderBy(l => l.SortOrder)
        .Select(l => "<option value={C_RowId}>{Name}</option>".FilledWith(l))
        .JoinedAsString()
        .AsRawHtml();
    }

    /// <Summary>Only those listed</Summary>
    public IEnumerable<object> FrontDeskPersonLines(List<Person> people,
      FrontDeskSortEnum sortType = FrontDeskSortEnum.ByName)
    {
      var peopleCount = people.Count;
      if (peopleCount == 0) return new List<object>();
      if (people.Any(p => p.Voter == null)) throw new ApplicationException("Voter is null");

      var hasMultipleLocations = ContextItems.LocationModel.HasMultipleLocations;
      var useOnline = UserSession.CurrentPeopleElection.OnlineEnabled;
      var firstPersonGuid = people[0].PersonGuid;

      var onlineProcessed = Db.OnlineVotingInfo
        .Where(ovi => ovi.ElectionGuid == UserSession.CurrentElectionGuid)
        .Where(ovi => ovi.Status == OnlineBallotStatusEnum.Processed.Value)
        .Where(ovi => peopleCount != 1 || firstPersonGuid == ovi.PersonGuid)
        .Select(ovi => ovi.PersonGuid)
        .ToList();

      return people
        .OrderBy(p => sortType == FrontDeskSortEnum.ByArea ? p.Area : "")
        .ThenBy(p => p.LastName)
        .ThenBy(p => p.FirstName)
        .ThenBy(p => p.C_RowId)
        .Select(p =>
        {
          var votingMethod = p.Voter.VotingMethod;
          return new
          {
            PersonId = p.C_RowId,
            p.FullName,
            NameLower = (p.FullName + p.BahaiId).WithoutDiacritics(true).ReplacePunctuation(' ')
              .Replace(" ", "").Replace("\"", "\\\""),
            p.Area,
            RegistrationTime = p.Voter.RegistrationTime.AsUtc(),
            VotedAt = new[]
            {
              ShowTellers(p),
              hasMultipleLocations ? LocationName(p.Voter.VotingLocationGuid) : ""
            }.JoinedAsString("; ", true),
            Log = p.Voter.RegistrationLog,
            VotingMethod = votingMethod,
            InPerson = votingMethod == VotingMethodEnum.InPerson,
            DroppedOff = votingMethod == VotingMethodEnum.DroppedOff,
            MailedIn = votingMethod == VotingMethodEnum.MailedIn,
            CalledIn = votingMethod == VotingMethodEnum.CalledIn,
            Custom1 = votingMethod == VotingMethodEnum.Custom1,
            Custom2 = votingMethod == VotingMethodEnum.Custom2,
            Custom3 = votingMethod == VotingMethodEnum.Custom3,
            Imported = votingMethod == VotingMethodEnum.Imported,
            Online = useOnline &&
                     (votingMethod == VotingMethodEnum.Online || votingMethod == VotingMethodEnum.Kiosk),
            HasOnline = useOnline && p.Voter.HasOnlineBallot.GetValueOrDefault(),
            CanBeOnline = useOnline &&
                          (votingMethod == VotingMethodEnum.Online
                           || votingMethod == VotingMethodEnum.Kiosk
                           || p.Voter.HasOnlineBallot.GetValueOrDefault()
                           || p.Email.HasContent()
                           || p.Voter.KioskCode.HasContent()
                           || p.Phone.HasContent()
                          ), // consider VotingMethod in case email/phone removed after
            OnlineProcessed = onlineProcessed.Contains(p.PersonGuid),
            // Registered = votingMethod == VotingMethodEnum.Registered,
            EnvNum = ShowEnvNum(p),
            CanVote = p.Voter.CanVote,
            CanReceiveVotes = p.Voter.CanReceiveVotes, // for ballot entry page
            p.Voter.IneligibleReasonGuid, // for ballot entry page
            p.BahaiId,
            p.UnitName,
            flags = p.Voter.Flags.SplitWithString("|")
          };
        });
    }

    public static string FormatRegistrationLog(Person p)
    {
      return p.Voter.RegistrationLog.Count > 1
        ? p.Voter.RegistrationLog
          .JoinedAsString("\n")
          .SurroundContentWith(" <span class=Log title=\"", "\"></span>")
        : "";
    }

    private static int? ShowEnvNum(Person p)
    {
      return p.Voter.EnvNum;

      //let client show/hide
      //return p.VotingMethod.HasNoContent() || p.VotingMethod == VotingMethodEnum.Registered
      //  ? null
      //  : p.EnvNum;
    }

    private static string ShowTellers(Person p)
    {
      var names = new List<string>
      {
        p.Voter.Teller1,
        p.Voter.Teller2
      };
      return names.JoinedAsString(", ", true);
    }

    public JsonResult RegisterVotingMethod(int personId, string voteType, bool forceDeselect, int locationId)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();

      var locationModel = new LocationModel();

      var hasMultiplePhysicalLocations = locationModel.HasMultiplePhysicalLocations;

      if (hasMultiplePhysicalLocations && UserSession.CurrentLocation == null)
        return new { Message = "Must select your location first!" }.AsJsonResult();

      if (UserSession.CurrentLocation.C_RowId != locationId)
        new ComputerModel().MoveCurrentComputerIntoLocation(locationId);

      if (UserSession.GetCurrentTeller(1).HasNoContent())
        return new { Message = "Must select \"Teller at Keyboard\" first!" }.AsJsonResult();

      if (!VotingMethodEnum.Exists(voteType)) return new { Message = "Invalid type" }.AsJsonResult();

      var personCacher = new PersonCacher(Db);
      var person = personCacher.AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);
      if (person == null) return new { Message = "Unknown person" }.AsJsonResult();

      var voter = person.Voter;
      if (voter == null) return new { Message = "Unknown person voter" }.AsJsonResult();

      if (voter.VotingMethod == VotingMethodEnum.Online || voter.VotingMethod == VotingMethodEnum.Kiosk)
      {
        var onlineVoter = Db.OnlineVotingInfo.SingleOrDefault(ovi =>
          ovi.PersonGuid == person.PersonGuid && ovi.ElectionGuid == person.ElectionGuid);
        if (onlineVoter != null)
          if (onlineVoter.Status == OnlineBallotStatusEnum.Processed)
            return new { Message = "This online ballot has been processed. Registration cannot be changed." }
              .AsJsonResult();
      }

      Db.Person.Attach(person);
      Db.Voter.Attach(voter);

      voter.Teller1 = UserSession.GetCurrentTeller(1);
      voter.Teller2 = UserSession.GetCurrentTeller(2);

      var votingMethodRemoved = false;
      Guid? oldVoteLocationGuid;
      Guid? newVoteLocationGuid = null;
      var utcNow = DateTime.UtcNow;

      if (voter.VotingMethod == voteType || forceDeselect || !voter.CanVote.AsBoolean())
      {
        oldVoteLocationGuid = voter.VotingLocationGuid;

        // it is already set this way...turn if off
        voter.VotingMethod = null;
        voter.VotingLocationGuid = null;
        voter.RegistrationTime = utcNow;
        votingMethodRemoved = true;

        var log = voter.RegistrationLog;
        log.Add(new[]
        {
          voter.RegistrationTime.AsUtc().AsString("o"),
          "De-selected",
          ShowTellers(person),
          hasMultiplePhysicalLocations ? LocationName(UserSession.CurrentLocationGuid) : null
        }.JoinedAsString("; ", true));
        voter.RegistrationLog = log;
      }
      else
      {
        voter.VotingMethod = voteType;

        oldVoteLocationGuid = voter.VotingLocationGuid;

        voter.VotingLocationGuid = UserSession.CurrentLocationGuid;
        voter.RegistrationTime = utcNow;

        newVoteLocationGuid = voter.VotingLocationGuid;

        var log = voter.RegistrationLog;
        log.Add(new[]
        {
          voter.RegistrationTime.AsUtc().AsString("o"),
          VotingMethodEnum.TextFor(voter.VotingMethod),
          ShowTellers(person),
          hasMultiplePhysicalLocations ? LocationName(UserSession.CurrentLocationGuid) : null
        }.JoinedAsString("; ", true));
        voter.RegistrationLog = log;

        // make number for every method
        var needEnvNum = voter.EnvNum == null;

        if (needEnvNum) voter.EnvNum = new ElectionHelper().GetNextEnvelopeNumber();
      }

      personCacher.UpdateItemAndSaveCache(person);

      UpdateFrontDeskListing(person, votingMethodRemoved);

      UpdateLocationCounts(newVoteLocationGuid, oldVoteLocationGuid, personCacher);

      Db.SaveChanges();

      return true.AsJsonResult();
    }

    public JsonResult SetFlag(int personId, string flag, bool forceDeselect, int locationId)
    {
      var locationModel = new LocationModel();

      var hasMultiplePhysicalLocations = locationModel.HasMultiplePhysicalLocations;

      if (hasMultiplePhysicalLocations && UserSession.CurrentLocation == null)
        return new { Message = "Must select your location first!" }.AsJsonResult();

      if (UserSession.CurrentLocation.C_RowId != locationId)
        new ComputerModel().MoveCurrentComputerIntoLocation(locationId);

      if (UserSession.GetCurrentTeller(1).HasNoContent())
        return new { Message = "Must select \"Teller at Keyboard\" first!" }.AsJsonResult();

      var personCacher = new PersonCacher(Db);
      var person = personCacher.AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);
      if (person == null) return new { Message = "Unknown person" }.AsJsonResult();
      var voter = person.Voter;
      if (voter == null) return new { Message = "Unknown person" }.AsJsonResult();

      Db.Person.Attach(person);

      voter.Teller1 = UserSession.GetCurrentTeller(1);
      voter.Teller2 = UserSession.GetCurrentTeller(2);

      var utcNow = DateTime.UtcNow;

      var allowedFlags = UserSession.CurrentPeopleElection.FlagsList;
      var currentFlags = voter.Flags.DefaultTo("").Split('|').ToList();

      var incomingFlag = flag.Substring(5);

      if (currentFlags.Contains(incomingFlag) || forceDeselect)
      {
        // it is already set this way...turn if off
        voter.Flags = currentFlags.Where(f => f != incomingFlag).JoinedAsString("|");

        var log = voter.RegistrationLog;
        log.Add(new[]
        {
          utcNow.AsString("o"),
          "Removed " + incomingFlag,
          ShowTellers(person),
          hasMultiplePhysicalLocations ? LocationName(UserSession.CurrentLocationGuid) : null
        }.JoinedAsString("; ", true));
        voter.RegistrationLog = log;
      }
      else
      {
        currentFlags.Add(incomingFlag);
        voter.Flags = currentFlags.JoinedAsString("|");

        voter.VotingLocationGuid = UserSession.CurrentLocationGuid;

        var log = voter.RegistrationLog;
        log.Add(new[]
        {
          utcNow.AsString("o"),
          "Set " + incomingFlag,
          ShowTellers(person),
          hasMultiplePhysicalLocations ? LocationName(UserSession.CurrentLocationGuid) : null
        }.JoinedAsString("; ", true));
        voter.RegistrationLog = log;
      }
      personCacher.UpdateItemAndSaveCache(person);

      UpdateFrontDeskListing(person);

      Db.SaveChanges();

      return true.AsJsonResult();
    }

    private void UpdateLocationCounts(Guid? newVoteLocationGuid, Guid? oldVoteLocationGuid,
      PersonCacher personCacher)
    {
      // would be great to throw this into a remote queue to be processed later
      var locationCacher = new LocationCacher(Db);
      var saveNeeded = false;

      UpdateLocation(newVoteLocationGuid, personCacher, locationCacher, ref saveNeeded);

      if (oldVoteLocationGuid != null && oldVoteLocationGuid != newVoteLocationGuid)
        UpdateLocation(oldVoteLocationGuid, personCacher, locationCacher, ref saveNeeded);

      if (saveNeeded) Db.SaveChanges();
    }

    private void UpdateLocation(Guid? locationGuid, PersonCacher personCacher, LocationCacher locationCacher,
      ref bool saveNeeded)
    {
      if (locationGuid == null) return;

      var location = locationCacher.AllForThisElection.FirstOrDefault(l => l.LocationGuid == locationGuid);
      if (location == null) return;

      var oldCount = location.BallotsCollected;
      var newCount = personCacher.AllForThisElection.Count(
        p => p.Voter.VotingLocationGuid == location.LocationGuid && !string.IsNullOrEmpty(p.Voter.VotingMethod));
      if (oldCount == newCount) return;

      Db.Location.Attach(location);

      location.BallotsCollected = newCount;

      locationCacher.UpdateItemAndSaveCache(location);
      saveNeeded = true;
    }

    //    public void UpdateFrontDeskListing(long lastRowVersion, bool votingMethodRemoved)
    //    {
    //      UpdateFrontDeskListing(new PersonCacher(Db).AllForThisElection
    //          .Where(p => p.C_RowVersionInt > lastRowVersion)
    //          .ToList(), votingMethodRemoved);
    //    }
    /// <summary>
    ///   Update listing for everyone updated since this version
    /// </summary>
    /// <summary>
    ///   Update listing for just one person
    /// </summary>
    /// <param name="person"></param>
    public void UpdateFrontDeskListing(Person person)
    {
      UpdateFrontDeskListing(person, false);
    }

    /// <summary>
    ///   Update listing
    /// </summary>
    /// <param name="person">The people to update. Must have Voter embedded</param>
    /// <param name="votingMethodRemoved"></param>
    public void UpdateFrontDeskListing(Person person, bool votingMethodRemoved)
    {
      if (person == null) throw new ApplicationException("Person is null");
      var updateInfo = new
      {
        PersonLines = FrontDeskPersonLines(new List<Person> { person }),
        LastRowVersion = person.C_RowVersionInt
      };
      new FrontDeskHub().UpdatePeople(updateInfo);
      new VoterPersonalHub().Update(person);

      var oldestStamp = person.C_RowVersionInt.AsLong() - 5; // send last 5, to ensure none are missed
      var rollCallModel = new RollCallModel();
      var rollCallInfo = new
      {
        changed = rollCallModel.GetMorePeople(oldestStamp, out var newStamp),
        removedId = votingMethodRemoved ? person.C_RowId : 0,
        newStamp
      };

      if (rollCallInfo.newStamp != 0 || rollCallInfo.removedId != 0)
        new RollCallHub().UpdateAllConnectedClients(rollCallInfo);
    }

    public JsonResult DeleteAllPeople()
    {
      var hasOnline = Db.OnlineVotingInfo.Any(p => p.ElectionGuid == PeopleElectionGuid && p.ListPool != null);
      if (hasOnline)
        return new
        {
          Success = false,
          Results = "Nothing was deleted. Once online votes have been recorded, you cannot delete all the people. "
        }.AsJsonResult();

      var rows = 0;
      try
      {
        int newRows;
        do
        {
          newRows = Db.Person.Where(p => p.ElectionGuid == PeopleElectionGuid).Take(500).Delete();
          rows += newRows;
        } while (newRows > 0);

        do
        {
          newRows = Db.Voter.Where(p => p.ElectionGuid == PeopleElectionGuid).Take(500).Delete();
        } while (newRows > 0);

        int oviRows;
        do
        {
          oviRows = Db.OnlineVotingInfo.Where(p => p.ElectionGuid == PeopleElectionGuid).Take(500).Delete();
        } while (oviRows > 0);
      }
      catch (Exception)
      {
        return
          new
          {
            Success = false,
            Results =
              "Nothing was deleted. Once votes have been recorded, you cannot delete all the people. "
          }.AsJsonResult();
      }

      new PersonCacher(Db).DropThisCache();

      return new
      {
        Success = true,
        Results = $"{rows:N0} {rows.Plural("people", "person")} deleted",
        count = NumberOfPeople
      }.AsJsonResult();
    }
  }
}