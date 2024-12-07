using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Schema;
using Newtonsoft.Json;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  // custom for Canadian import.  If/when more are added, can make more flexible.

  public class ImportBallotsModel : DataConnectedModel
  {
    private const string FileTypeXml = "XML";
    private string _tempMsg = "";

    /*
     *
     * TODO: If Lsa2U - import ballots only
     *
     *
     */

    public object PreviousUploads()
    {
      var timeOffset = UserSession.TimeOffsetServerAhead;

      return Db.ImportFile
        .Where(vi => vi.ElectionGuid == UserSession.CurrentElectionGuid)
        .Where(vi => vi.FileType == FileTypeXml)
        .OrderByDescending(vi => vi.UploadTime)
        .ToList()
        .Select(vi => new
        {
          vi.C_RowId,
          vi.FileSize,
          UploadTime = vi.UploadTime.HasValue ? vi.UploadTime.AsUtc() : null,
          vi.ProcessingStatus,
          vi.OriginalFileName,
          CodePageName = vi.CodePage == null ? null : Encoding.GetEncoding(vi.CodePage.Value).EncodingName,
          vi.Messages
        })
        .ToList();
    }

    public ActionResult DeleteFile(int id)
    {
      var targetFile = Db.ImportFile.FirstOrDefault(f => f.C_RowId == id && f.FileType == FileTypeXml);
      if (targetFile != null)
      {
        Db.ImportFile.Remove(targetFile);
        Db.SaveChanges();

        new LogHelper().Add("Deleted file #" + id);
        _tempMsg = "File Deleted";
      }
      else
      {
        _tempMsg = "File not found";
      }

      return GetUploadList();
    }

    public ActionResult GetUploadList()
    {
      return new
      {
        serverTime = DateTime.Now,
        message = _tempMsg,
        previousFiles = PreviousUploads()
      }.AsJsonResult();
    }


    public string ProcessUpload(out int rowId)
    {
      rowId = 0;

      var httpRequest = HttpContext.Current.Request;
      var inputStream = httpRequest.InputStream;
      if (inputStream.Length == 0) return "No file received";

      var name = HttpUtility.UrlDecode(httpRequest.Headers["X-File-Name"].DefaultTo("unknown name"));
      var fileSize = (int)inputStream.Length;

      var record = new ImportFile
      {
        ElectionGuid = UserSession.CurrentElectionGuid,
        Contents = new byte[fileSize],
        FileSize = fileSize,
        OriginalFileName = name,
        UploadTime = DateTime.UtcNow,
        FileType = FileTypeXml,
        ProcessingStatus = "Uploaded"
      };

      var numWritten = inputStream.Read(record.Contents, 0, fileSize);
      if (numWritten != fileSize) return $"Read {numWritten} bytes. Should be {fileSize}.";

      record.CodePage = ImportHelper.DetectCodePage(record.Contents).CodePage;

      Db.ImportFile.Add(record);
      Db.SaveChanges();

      rowId = record.C_RowId;

      new LogHelper().Add("Uploaded file #" + record.C_RowId);

      return "";
    }


    public JsonResult Import(int rowId)
    {
      var file =
        Db.ImportFile.SingleOrDefault(
          fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == rowId);
      if (file == null)
        return new
        {
          failed = true,
          result = new[] { "File not found" }
        }.AsJsonResult();

      return new
      {
      }.AsJsonResult();
    }

    public JsonResult GetPreviewInfo(int id, bool forceRefreshCache)
    {
      if (forceRefreshCache)
      {
        new ElectionCacher().DropThisCache();
        new LocationCacher().DropThisCache();
        new BallotCacher().DropThisCache();
        new VoteCacher().DropThisCache();
        new PersonCacher().DropThisCache();
      }

      var electionInfo = ProcessFile(id);

      return ElectionPreviewInfo(electionInfo);
    }

    public JsonResult LoadFile(int id)
    {
      var electionInfo = ProcessFile(id);

      if (electionInfo.ImportErrors.Any()) return ElectionPreviewInfo(electionInfo);

      return ImportPeopleAndBallots(electionInfo);
    }

    private static JsonResult ElectionPreviewInfo(CdnImportDto electionInfo)
    {
      return new
      {
        numBallots = electionInfo.Ballots.Count,
        voters = electionInfo.Voters.OrderBy(v => v.lastname).ThenBy(v => v.firstname),
        electionInfo.locality,
        electionInfo.localunit,
        electionInfo.ImportErrors,
        electionInfo.AlreadyLoaded,
        electionInfo.HasUnregistered
      }.AsJsonResult();
    }

    private CdnImportDto ProcessFile(int id)
    {
      var importFile =
        Db.ImportFile.SingleOrDefault(
          fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == id);
      if (importFile == null) throw new ApplicationException("File not found");

      var electionInfo = ParseCdnBallotsFile(importFile);
      return electionInfo;
    }

    private CdnImportDto ParseCdnBallotsFile(ImportFile importFile)
    {
      var xml = new XmlDocument();
      var electionInfo = new CdnImportDto();
      string xmlString;

      try
      {
        var importFileCodePage = importFile.CodePage ?? ImportHelper.DetectCodePage(importFile.Contents)?.CodePage;
        xmlString = importFile.Contents.AsString(importFileCodePage);
        xml.LoadXml(xmlString);
      }
      catch (Exception e)
      {
        electionInfo.ImportErrors.Add(e.GetBaseException().Message);
        return electionInfo;
      }

      var path = HttpContext.Current.Server.MapPath("~/Xsd/CdnBallotImport.xsd");
      var schemaDocument = XmlReader.Create(path);
      xml.Schemas.Add("", schemaDocument);

      var issues = new List<string>();
      var fatal = false;

      xml.Validate(delegate (object sender, ValidationEventArgs args)
      {
        if (args.Severity == XmlSeverityType.Error) fatal = true;
        var message = args.Message;
        if (!issues.Contains(message)) issues.Add(message);
      });

      if (fatal || issues.Any())
      {
        electionInfo.ImportErrors.Add("The import file is not in the expected format:\n" + issues.JoinedAsString("\n"));
        return electionInfo;
      }

      try
      {
        var electionNode = xml.DocumentElement;
        if (electionNode == null)
        {
          electionInfo.ImportErrors.Add("No content?");
          return electionInfo;
        }

        electionNode.CopyAttributeValuesTo(electionInfo);

        foreach (XmlElement incomingVoter in electionNode.SelectNodes("descendant::voter"))
        {
          var voted = new Voter();
          incomingVoter.CopyAttributeValuesTo(voted);
          electionInfo.Voters.Add(voted);
        }

        foreach (XmlElement incomingBallot in electionNode.SelectNodes("descendant::ballot"))
        {
          var ballot = new IncomingBallot();
          incomingBallot.CopyAttributeValuesTo(ballot);
          electionInfo.Ballots.Add(ballot);


          foreach (XmlElement incomingVote in incomingBallot.SelectNodes("vote"))
          {
            var voteLine = new OnlineRawVote(incomingVote.InnerText);
            ballot.Votes.Add(voteLine);
          }
        }
      }
      catch (Exception e)
      {
        electionInfo.ImportErrors.Add(e.GetBaseException().Message);
        return electionInfo;
      }

      AnalyzeImportData(electionInfo);

      return electionInfo;
    }

    private void AnalyzeImportData(CdnImportDto electionInfo)
    {
      // file is good; analyze more
      if (electionInfo.Voters.Count != electionInfo.Ballots.Count)
        electionInfo.ImportErrors.Add(
          $"The number of voters ({electionInfo.Voters.Count}) and the number of ballots ({electionInfo.Ballots.Count}) must match.");

      // check if these people are valid

      var knownPeople = new PersonCacher(Db).AllForThisElection;

      foreach (var incomingVoter in electionInfo.Voters)
      {
        var matched = knownPeople
          // match on Baha'i ID (valid for Canada)
          .Where(p => p.BahaiId == incomingVoter.bahaiid)
          .ToList();

        if (matched.Count == 1)
        {
          // found them!
          var person = matched[0];

          incomingVoter.PersonGuid = person.PersonGuid;

          // only get the method if not Registered
          if (person.VotingMethod != null)
          {
            incomingVoter.ImportBlocked = true;
          }
          incomingVoter.VotingMethod = VotingMethodEnum.TextFor(person.VotingMethod);

          if (!person.CanVoteInElection.GetValueOrDefault())
            electionInfo.ImportErrors.Add($"{person.FullNameFL} is not permitted to vote.");
        }
        else
        {
          electionInfo.ImportErrors.Add(
            $"The Bahá'í ID ({incomingVoter.bahaiid}) for {incomingVoter.firstname} {incomingVoter.lastname} is not registered in TallyJ.");
          electionInfo.HasUnregistered = true;
        }
      }

      var numAlreadyVoted = electionInfo.Voters.Count(v => v.ImportBlocked);
      if (numAlreadyVoted > 0)
        electionInfo.ImportErrors.Add(
          $"{numAlreadyVoted.Plural("Some people have", "A person has")} already voted. You must change their registration on the Front Desk, if appropriate.");

      // check ballots
      var importedLocation = new LocationModel(Db).GetImportedLocation();
      var importedBallots = new BallotCacher(Db).AllForThisElection.Where(b => b.LocationGuid == importedLocation?.LocationGuid).ToList();
      var numToElect = UserSession.CurrentElection.NumberToElect.AsInt(0);
      var invalidNumVotes = false;

      foreach (var incomingBallot in electionInfo.Ballots)
      {
        var matched = importedBallots.FirstOrDefault(b => b.BallotNumAtComputer == incomingBallot.index);
        if (matched != null) incomingBallot.AlreadyLoaded = true;

        if (incomingBallot.Votes.Count != numToElect) invalidNumVotes = true;
      }

      if (invalidNumVotes)
        electionInfo.ImportErrors.Add(
          $"At least one ballot has the incorrect number of votes. Expecting {numToElect}.");

      var numBallotsAlreadyIn = electionInfo.Ballots.Count(b => b.AlreadyLoaded);
      if (numBallotsAlreadyIn > 0)
      {
        if (numBallotsAlreadyIn == electionInfo.Ballots.Count)
        {
          electionInfo.ImportErrors.Add("All ballots already loaded");
          electionInfo.AlreadyLoaded = true;
        }
        else
        {
          electionInfo.ImportErrors.Add($"{numBallotsAlreadyIn} {numBallotsAlreadyIn.Plural()} already loaded.");
        }
      }
    }

    private JsonResult ImportPeopleAndBallots(CdnImportDto importedElectionInfo)
    {
      // electionInfo has been cleaned, but make sure!
      if (importedElectionInfo.ImportErrors.Any() || !importedElectionInfo.Ballots.Any() ||
          !importedElectionInfo.Voters.Any()) return ElectionPreviewInfo(importedElectionInfo);

      // update election configuration
      var utcNow = DateTime.UtcNow;
      var election = UserSession.CurrentElection;

      //TODO: are these needed?
      // var peopleElection = UserSession.CurrentPeopleElection;
      // var parentElectionGuid = UserSession.CurrentParentElectionGuid;

      if (election.TallyStatus == ElectionTallyStatusEnum.Finalized.Value)
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();

      // ready to start
      var peopleModel = new PeopleModel();
      var peopleToUpdate = new List<Person>();

      var hub = new BallotImportHub();
      var importOkay = true;

      // recoded to not use Cacher objects
      using (var transaction = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromMinutes(10)))
      {
        try
        {
          var dbContext = Db;

          // upgrade this election to support Imported!
          var locationModel = new LocationModel(dbContext);


          var importLocation = locationModel.GetImportedLocation();
          if (importLocation == null)
          {
            importLocation = new Location
            {
              Name = LocationModel.ImportedLocationName,
              LocationGuid = Guid.NewGuid(),
              ElectionGuid = election.ElectionGuid,
              SortOrder = 91
            };
            dbContext.Location.Add(importLocation);
            dbContext.SaveChanges();
          }

          if (!election.VotingMethodsContains(VotingMethodEnum.Imported))
          {
            dbContext.Election.Attach(election);
            election.VotingMethodsAdjusted += VotingMethodEnum.Imported.Value;
            dbContext.SaveChanges();
          }

          var personCacher = new PersonCacher(dbContext);

          // marked who voted via imported ballot
          foreach (var voter in importedElectionInfo.Voters)
          {
            var person = personCacher.AllForThisElection.Single(p => p.PersonGuid == voter.PersonGuid);
            dbContext.Person.Attach(person);
            person.VotingMethod = VotingMethodEnum.Imported.Value;
            person.VotingLocationGuid = importLocation.LocationGuid;
            person.RegistrationTime = utcNow;
            person.EnvNum = null;

            var log = person.RegistrationLog;
            log.Add(new[]
            {
              person.RegistrationTime.AsUtc().AsString("o"),
              VotingMethodEnum.TextFor(person.VotingMethod)
            }.JoinedAsString("; ", true));
            person.RegistrationLog = log;

            peopleToUpdate.Add(person);

            if (peopleToUpdate.Count % 10 == 0) hub.StatusUpdate("Voters registered: " + peopleToUpdate.Count, true);
          }

          hub.StatusUpdate("Voters registered: " + peopleToUpdate.Count);


          // load ballots
          // var electionGuid = election.ElectionGuid;
          // var problems = new List<string>();
          // var numBallotsCreated = 0;
          // var ballotNum = 0;

          var ballotModel = BallotModelFactory.GetForCurrentElection();
          var ballotCacher = new BallotCacher(dbContext);
          var voteCacher = new VoteCacher(dbContext);
          var allPeople = new PersonCacher(dbContext).AllForThisElection;
          var ballotNum = 0;

          foreach (var importedBallot in importedElectionInfo.Ballots)
          {
            ballotNum = importedBallot.index;

            try
            {
              var ballotCreated = CreateBallotForImportedBallot(importedBallot,
                importLocation.LocationGuid, out var message, allPeople, dbContext);
              if (ballotCreated)
              {
                // numBallotsCreated++;
              }
              else
              {
                // problems.Add(message + " - ballot #" + ballotNum);
                hub.StatusUpdate(message);
              }

              if (ballotNum % 10 == 0) hub.StatusUpdate("Ballots read: " + ballotNum, true);
            }
            catch (Exception e)
            {
              var msg = $"Error: {e.LastException().Message} - #{ballotNum}";
              // problems.Add(msg);
              hub.StatusUpdate(msg);
            }
          }

          hub.StatusUpdate("Ballots read: " + ballotNum);
          hub.StatusUpdate("Saving ballots... (please wait)", true);

          // all done
          dbContext.SaveChanges();

          transaction.Complete();

          hub.StatusUpdate("Ballots Saved");

          // all ballots done - resort all (include any already processed)
          // var onlineBallots = Db.Ballot
          //   .Join(Db.Location.Where(l => l.ElectionGuid == electionGuid && l.LocationGuid == importLocation.LocationGuid), b => b.LocationGuid, l => l.LocationGuid, (b, l) => b)
          //   .ToList();
          //
          // // sort of random, by guid (won't keep changing) but new ones will be inserted randomly
          // var sorted = onlineBallots.OrderBy(b => b.BallotGuid).ToList();
          //
          // ballotNum = 1;
          // sorted.ForEach(b =>
          // {
          //   b.BallotNumAtComputer = ballotNum++;
          // });
          // Db.SaveChanges();

          // foreach (var problem in problems)
          // {
          //   result.Add("~E " + problem);
          // }

          // result.Add($"Created {numBallotsCreated} ballot{numBallotsCreated.Plural()}.");
          // result.Add("Import Complete");
        }
        catch (Exception e)
        {
          hub.StatusUpdate("Error - " + e.GetAllMsgs("; "));
          // result.Add("~E " + e.Message);
          importOkay = false;
        }
      }

      // foreach (var person in peopleToUpdate) peopleModel.UpdateFrontDeskListing(person);

      if (importOkay)
      {
        // hub.StatusUpdate("Front desk updated");
        hub.StatusUpdate("Updating ballot statuses... (please wait)", true);

        new ElectionCacher().DropThisCache();
        new LocationCacher().DropThisCache();
        new BallotCacher().DropThisCache();
        new VoteCacher().DropThisCache();
        new PersonCacher().DropThisCache();

        var currentElection = UserSession.CurrentElection;
        var analyzer = currentElection.IsSingleNameElection
          ? new ElectionAnalyzerSingleName(currentElection) as IElectionAnalyzer
          : new ElectionAnalyzerNormal(currentElection);
        analyzer.RefreshBallotStatuses();

        hub.StatusUpdate("Ballot statuses updated");
        hub.StatusUpdate("Updating front desk...", true);

        new FrontDeskHub().ReloadPage();
        hub.StatusUpdate("Front desk updated");
        hub.StatusUpdate("---");
        hub.StatusUpdate("Import complete");
      }

      return new
      {
        Success = importOkay
      }.AsJsonResult();
    }


    /// <summary>
    /// Used only in import.  Don't save - let caller do it
    /// </summary>
    /// <param name="incomingBallot"></param>
    /// <param name="importLocationGuid"></param>
    /// <param name="errorMessage"></param>
    /// <param name="allPeople"></param>
    /// <param name="db"></param>
    /// <returns></returns>
    private bool CreateBallotForImportedBallot(IncomingBallot incomingBallot, Guid importLocationGuid,
      out string errorMessage, List<Person> allPeople, ITallyJDbContext db)
    {
      // create ballot
      var ballot = new Ballot
      {
        BallotGuid = Guid.NewGuid(),
        LocationGuid = importLocationGuid,
        ComputerCode = ComputerModel.ComputerCodeForImported,
        BallotNumAtComputer = incomingBallot.index,
        StatusCode = BallotStatusEnum.Empty,
      };
      db.Ballot.Add(ballot);

      // add Votes
      var nextVoteNum = 0;

      foreach (var rawVote in incomingBallot.Votes)
      {
        nextVoteNum++;
        var vote = new Vote
        {
          BallotGuid = ballot.BallotGuid,
          PositionOnBallot = nextVoteNum,
          StatusCode = VoteStatusCode.OnlineRaw,
          SingleNameElectionCount = 1,
          OnlineVoteRaw = JsonConvert.SerializeObject(rawVote),
        };

        // attempt to match if it is exact...
        var matched = allPeople
          // match on first and last name only
          .Where(p => p.FirstName.ToLower() == rawVote.First.ToLower() &&
                      p.LastName.ToLower() == rawVote.Last.ToLower())
          // don't match if our list has "otherInfo" for this person - there might be some special considerations
          .Where(p => p.OtherInfo.HasNoContent())
          .ToList();

        if (matched.Count == 1)
        {
          // found one exact match
          var person = matched[0];
          vote.StatusCode = VoteStatusCode.Ok;
          vote.PersonGuid = person.PersonGuid;
          vote.PersonCombinedInfo = person.CombinedInfo;
          vote.InvalidReasonGuid = person.CanReceiveVotesInElection.AsBoolean(true) ? null : person.IneligibleReasonGuid; // status code will be updated later if this is set
        }

        db.Vote.Add(vote);
      }

      errorMessage = "";
      return true;
    }

    public JsonResult RemoveImportedInfo()
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized.Value)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }

      new ElectionCacher().DropThisCache();
      new LocationCacher().DropThisCache();
      new BallotCacher().DropThisCache();
      new VoteCacher().DropThisCache();
      new PersonCacher().DropThisCache();

      var importedLocation = new LocationModel(Db).GetImportedLocation();
      if (importedLocation == null)
      {
        return new { Message = "No imported data found" }.AsJsonResult();
      }


      // un-mark everyone who is marked as Imported
      var personCacher = new PersonCacher(Db);
      var peopleModel = new PeopleModel();
      var people = personCacher.AllForThisElection
        .Where(p => p.VotingMethod == VotingMethodEnum.Imported.Value);
      foreach (var person in people)
      {
        Db.Person.Attach(person);
        person.VotingMethod = null;
        person.VotingLocationGuid = null;
        person.RegistrationTime = DateTime.UtcNow;

        var log = person.RegistrationLog;
        log.Add(new[]
        {
          person.RegistrationTime.AsUtc().AsString("o"),
          "Imports Removed",
        }.JoinedAsString("; ", true));
        person.RegistrationLog = log;
      }

      // delete the Imported location. This also deletes all ballots in that location
      Db.Location.Attach(importedLocation);
      Db.Location.Remove(importedLocation);

      // remove the "Imported" voting method
      var election = UserSession.CurrentElection;
      Db.Election.Attach(election);
      election.VotingMethodsAdjusted = election.VotingMethodsAdjusted.Replace("I", "");

      try
      {
        Db.SaveChanges();
      }
      catch (Exception e)
      {
        return new
        {
          Message = e.GetAllMsgs("; ")
        }.AsJsonResult();
      }

      new ElectionCacher().DropThisCache();
      new LocationCacher().DropThisCache();
      new BallotCacher().DropThisCache();
      new VoteCacher().DropThisCache();
      new PersonCacher().DropThisCache();

      new FrontDeskHub().ReloadPage();

      return new
      {
        Success = true,
        Message = "Ballots removed"
      }.AsJsonResult();
    }


    [Serializable]
    public class CdnImportDto
    {
      public CdnImportDto()
      {
        ImportErrors = new List<string>();
        Voters = new List<Voter>();
        Ballots = new List<IncomingBallot>();
      }

      public DateTime timestamp { get; set; }
      public string locality { get; set; }
      public string localunit { get; set; }
      public List<Voter> Voters { get; set; }
      public List<IncomingBallot> Ballots { get; set; }
      public List<string> ImportErrors { get; set; }
      public bool HasUnregistered { get; set; }
      public bool AlreadyLoaded { get; set; }
    }

    [Serializable]
    public class Voter
    {
      public string bahaiid { get; set; }
      public string firstname { get; set; }
      public string lastname { get; set; }
      // additonal attributes not in the XML
      public Guid PersonGuid { get; set; }
      public string VotingMethod { get; set; }
      public bool ImportBlocked { get; set; }
    }

    [Serializable]
    public class IncomingBallot
    {
      public IncomingBallot()
      {
        Votes = new List<OnlineRawVote>();
      }

      public Guid guid { get; set; }
      public int index { get; set; }
      public List<OnlineRawVote> Votes { get; set; }
      public bool AlreadyLoaded { get; set; }
    }
    //
    // [Serializable]
    // public class VoteLine
    // {
    //   public string Text { get; set; }
    //   public int index { get; set; }
    // }
  }
}