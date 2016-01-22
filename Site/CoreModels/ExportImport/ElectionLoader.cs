using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Schema;
using EntityFramework.BulkInsert.Extensions;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels.ExportImport
{
  public class ElectionLoader : DataConnectedModel
  {
    private Election _election;
    private Guid _electionGuid;
    private Dictionary<Guid, Guid> _guidMap;
    private XmlNamespaceManager _nsm;
    private PeopleModel _peopleModel;
    private XmlDocument _xmlDocument;
    private XmlElement _xmlRoot;
    private ImportHub _hub;
    private List<Person> _people;
    private Dictionary<Location, XmlElement> _locationsToProcess;
    private Guid _defaultLocationGuid;

    public JsonResult Import(HttpPostedFileBase file)
    {
      try
      {
        VerifyFileReceived(file);
        LoadIntoXmlDoc(file);
        ValidateXml();

        var success = LoadXmlToDatabase();

        return success
                 ? new LoadResult(_electionGuid).AsJsonResult()
                 : new LoadResult("(Under construction!)").AsJsonResult();
      }
      catch (LoaderException ex)
      {
        return new LoadResult(ex.GetAllMsgs("\n")).AsJsonResult();
      }
      catch (Exception ex)
      {
        // some unexpected exception...
        return new LoadResult(ex.GetType().Name + ": " + ex.GetAllMsgs("\n")).AsJsonResult();
      }
    }

    private void VerifyFileReceived(HttpPostedFileBase file)
    {
      if (file.InputStream == null || file.InputStream.Length == 0)
      {
        throw new LoaderException("No file provided");
      }
    }

    private void LoadIntoXmlDoc(HttpPostedFileBase file)
    {
      _xmlDocument = new XmlDocument();
      try
      {
        _xmlDocument.Load(file.InputStream);
      }
      catch (XmlException ex)
      {
        throw new LoaderException(ex.GetAllMsgs("\n"));
      }
    }

    private void ValidateXml()
    {
      var path = HttpContext.Current.Server.MapPath("~/Xsd/TallyJv2-Export.xsd");
      var schemaDocument = XmlReader.Create(path);
      _xmlDocument.Schemas.Add(Exporter.XmlNameSpace, schemaDocument);

      var issues = new List<string>();
      var fatal = false;

      _xmlDocument.Validate(delegate (object sender, ValidationEventArgs args)
        {
          if (args.Severity == XmlSeverityType.Error)
          {
            fatal = true;
          }
          var message = args.Message;
          if (!issues.Contains(message))
          {
            issues.Add(message);
          }
        });

      if (fatal)
      {
        throw new LoaderException("The XML document is not valid: " + issues.JoinedAsString("\n"));
      }

      _nsm = new XmlNamespaceManager(_xmlDocument.NameTable);
      _nsm.AddNamespace("t", Exporter.XmlNameSpace);

      // get the document element for later use
      _xmlRoot = _xmlDocument.DocumentElement;
    }

    private bool LoadXmlToDatabase()
    {
      _guidMap = new Dictionary<Guid, Guid>();
      _locationsToProcess = new Dictionary<Location, XmlElement>();
      _hub = new ImportHub();
      var start = DateTime.Now;
      //      using (var transaction = new TransactionScope())
      {
        try
        {
          // the xml document is validated and ready to load

          // Elements to be imported...

          // election
          // teller
          // user - not imported. Current user becomes owner of imported election
          // person
          // location
          // ballot
          // vote
          // log 
          // resultSummary
          // result
          // resultTie
          // reason - not imported - embedded in the application. Included in export for reporting

          LoadElectionInfo();

          LoadTellers();

          LoadLocations();

          LoadPeople();

          LoadBallots();

          LoadResultInfo();

          _hub.StatusUpdate("Running analysis...");

          new ElectionModel().JoinIntoElection(_electionGuid);

          var analyzer = new ResultsModel(null, _hub);
          analyzer.GenerateResults();

          _hub.StatusUpdate("Load complete ({0:N1} seconds)".FilledWith((DateTime.Now - start).TotalSeconds));
        }
        catch (DbUpdateException ex)
        {
          throw new LoaderException("Cannot save", ex.LastException());
        }
        catch (DbEntityValidationException ex)
        {
          var msgs = new List<string>();
          foreach (var msg in ex.EntityValidationErrors.Where(v => !v.IsValid).Select(validationResult =>
            {
              var err = validationResult.ValidationErrors.First();
              return "{0}: {1}".FilledWith(err.PropertyName, err.ErrorMessage);
            }).Where(msg => !msgs.Contains(msg)))
          {
            msgs.Add(msg);
          }
          throw new LoaderException("Unable to save: " + msgs.JoinedAsString("; "));
        }

        //        transaction.Complete();
      }

      return true;
    }


    private void LoadElectionInfo()
    {
      var electionNode = _xmlRoot.SelectSingleNode("t:election", _nsm) as XmlElement;

      _election = new Election();
      electionNode.CopyAttributeValuesTo(_election);

      // reset Guid to a new guid
      _election.ElectionGuid
        = _electionGuid
          = Guid.NewGuid();

      var newName = _election.Name;
      var matchingNums = Db.Election
        .Where(e => e.Name == newName || e.Name.StartsWith(newName))
        .ToList()
        .Select(e => e.Name.Replace(newName, "").Split(' ').Last().AsInt());
      if (matchingNums.Any())
      {
        if (matchingNums.All(n => n != 0))
        {
          // don't need to rename
        }
        else
        {
          var last = matchingNums.Max(n => n);
          _election.Name = string.Format("{0} - {1}", newName, last + 1);
        }
      }

      Db.BulkInsert(new[] { _election });
      _hub.StatusUpdate("Loading <b>{0}</b>".FilledWith(_election.Name));

      // set current person as owner
      var join = new JoinElectionUser
      {
        ElectionGuid = _electionGuid,
        UserId = UserSession.UserGuid
      };
      //Db.JoinElectionUser.Add(join);
      Db.BulkInsert(new[] { join });
    }

    private void LoadTellers()
    {
      var tellersXml = _xmlRoot.SelectNodes("t:teller", _nsm);

      // there may not be any tellers listed
      if (tellersXml == null) return;

      var toLoad = new List<Teller>();
      foreach (XmlElement tellerXml in tellersXml)
      {
        LoadTeller(tellerXml, toLoad);
      }
      Db.BulkInsert(toLoad);

      _hub.StatusUpdate("Loaded {0} teller{1}".FilledWith(tellersXml.Count, tellersXml.Count.Plural()));
    }

    private void LoadTeller(XmlElement tellerXml, List<Teller> tellers)
    {
      // need to map Guid to new Guid
      var teller = new Teller();
      tellers.Add(teller);

      tellerXml.CopyAttributeValuesTo(teller);

      // reset Guid to a new guid
      //      var oldGuid = teller.TellerGuid;
      //      var newGuid = Guid.NewGuid();
      //      _guidMap.Add(oldGuid, newGuid);

      //Debugger.Log(3, "Teller", "Teller {0}-->{1}\n".FilledWith(oldGuid, newGuid));

      //      teller.TellerGuid = newGuid;
      teller.ElectionGuid = _electionGuid;

      //Debugger.Log(3, "Teller", "Teller={0}\n".FilledWith(teller.TellerGuid));

      //Db.Teller.Add(teller);
    }

    private void LoadLocations()
    {
      var locationsXml = _xmlRoot.SelectNodes("t:location", _nsm);

      if (locationsXml == null || locationsXml.Count == 0)
      {
        throw new LoaderException("No locations in the file");
      }

      var toLoad = new List<Location>();
      foreach (XmlElement locationXml in locationsXml)
      {
        LoadLocation(locationXml, toLoad);
      }
      Db.BulkInsert(toLoad);
    }

    private void LoadLocation(XmlElement locationXml, List<Location> locations)
    {
      var location = new Location();
      locations.Add(location);

      locationXml.CopyAttributeValuesTo(location);

      // reset Guid to a new guid
      // reset Guid to a new guid
      var oldGuid = location.LocationGuid;
      var newGuid = Guid.NewGuid();
      location.LocationGuid = newGuid;
      if (oldGuid != Guid.Empty)
      {
        _guidMap.Add(oldGuid, newGuid);
      }

      location.ElectionGuid = _electionGuid;

      if (_defaultLocationGuid == Guid.Empty)
      {
        _defaultLocationGuid = location.LocationGuid;
      }

      _locationsToProcess.Add(location, locationXml);
    }


    private void LoadPeople()
    {
      var peopleXml = _xmlRoot.SelectNodes("t:person", _nsm);
      var numToLoad = peopleXml.Count;

      if (peopleXml == null || numToLoad == 0)
      {
        throw new LoaderException("No people in the file");
      }

      _peopleModel = new PeopleModel(_election);

      _people = new List<Person>();
      var rowsProcessed = 0;

      foreach (XmlElement personXml in peopleXml)
      {
        LoadPerson(personXml, _people);

        rowsProcessed++;
        if (rowsProcessed % 100 == 0)
        {
          _hub.StatusUpdate("Processing {0:n0} people".FilledWith(rowsProcessed, numToLoad), true);
        }
      }

      Db.BulkInsert(_people);

      _hub.StatusUpdate("Loaded {0:n0} people.".FilledWith(rowsProcessed));
    }

    private void LoadPerson(XmlElement personXml, List<Person> people)
    {
      // need to map Guid to new Guid
      var person = new Person();
      people.Add(person);
      personXml.CopyAttributeValuesTo(person);

      personXml.CopyAttributeValueTo("TellerAtKeyboard", person, p => person.Teller1);
      personXml.CopyAttributeValueTo("TellerAssisting", person, p => person.Teller2);

      // reset Guid to a new guid
      var oldGuid = person.PersonGuid;
      var newGuid = Guid.NewGuid();
      person.PersonGuid = newGuid;
      _guidMap.Add(oldGuid, newGuid);

      // pre version 2.3.6, the export did not include the Location's Guid, so we cannot map back to it
      // if missing, map to first location
      UpdateGuidFromMapping(person, o => o.VotingLocationGuid, _defaultLocationGuid);

      person.ElectionGuid = _electionGuid;

      // leave the "AtStart" alone, so we preserve change from when the election was originally set up
      _peopleModel.SetCombinedInfos(person);
    }
    private void LoadBallots()
    {
      foreach (var kvp in _locationsToProcess)
      {
        LoadBallots(kvp.Value, kvp.Key);
      }
    }

    private void LoadBallots(XmlElement locationXml, Location location)
    {
      _hub.StatusUpdate("Loading for '" + location.Name + "'...");

      var locationGuid = location.LocationGuid;

      var ballotsXml = locationXml.SelectNodes("t:ballot", _nsm);
      if (ballotsXml != null)
      {
        var ballots = new List<Ballot>();
        var votes = new List<Vote>();
        var numLoaded = 0;
        var numToLoad = ballotsXml.Count;

        foreach (XmlElement ballotXml in ballotsXml)
        {
          LoadBallotAndVotes(ballotXml, locationGuid, ballots, votes);

          numLoaded++;
          if (numLoaded == 1 || numLoaded % 10 == 0)
          {
            _hub.StatusUpdate("Processing {0:n0} of {1:n0} ballots ({2:n0} votes)".FilledWith(ballots.Count, numToLoad, votes.Count), true);
          }
        }

        Db.BulkInsert(ballots);
        Db.BulkInsert(votes);
        _hub.StatusUpdate("Loaded {0:n0} ballots ({1:n0} votes)".FilledWith(ballots.Count, votes.Count));
      }

      var logsXml = locationXml.SelectNodes("t:log", _nsm);
      if (logsXml != null)
      {
        var logs = new List<C_Log>();

        foreach (XmlElement logXml in logsXml)
        {
          LoadLog(logXml, locationGuid, logs);
        }
        //Db.SaveChanges();
        Db.BulkInsert(logs);
        _hub.StatusUpdate("Loaded {0} log entr{1}".FilledWith(logs.Count, logs.Count.Plural("ies", "y")));
      }

      var logger = new LogHelper(_electionGuid);
      logger.Add("Loaded election from file", true);
      //Db.SaveChanges();
    }

    //    private void LoadComputer(XmlElement computerXml, Guid locationGuid)
    //    {
    //      // need to map Guid to new Guid
    //      var computer = new Computer();
    //      computerXml.CopyAttributeValuesTo(computer);
    //
    //      // reset Guid to a new guid
    //      computer.ElectionGuid = _electionGuid;
    //      computer.LocationGuid = locationGuid;
    //
    //      Db.Computer.Add(computer);
    //    }

    private void LoadBallotAndVotes(XmlElement ballotXml, Guid locationGuid, List<Ballot> ballots, List<Vote> votes)
    {
      var ballot = new Ballot();
      ballots.Add(ballot);

      ballotXml.CopyAttributeValuesTo(ballot);

      ballot.BallotGuid = Guid.NewGuid();
      ballot.LocationGuid = locationGuid;

      var votesXml = ballotXml.SelectNodes("t:vote", _nsm);
      if (votesXml != null)
      {
        var positionOnBallot = 1;
        var ballotGuid = ballot.BallotGuid;
        foreach (XmlElement voteXml in votesXml)
        {
          LoadVote(voteXml, positionOnBallot++, ballotGuid, votes);
        }
        //Db.SaveChanges();
      }
    }

    private void LoadVote(XmlElement voteXml, int positionOnBallot, Guid ballotGuid, List<Vote> votes)
    {
      var vote = new Vote();
      votes.Add(vote);
      voteXml.CopyAttributeValuesTo(vote);

      vote.PositionOnBallot = positionOnBallot;
      vote.BallotGuid = ballotGuid;
      UpdateGuidFromMapping(vote, v => v.PersonGuid, Guid.Empty);

      if (vote.PersonGuid.AsGuid().HasContent())
      {
        var person = _people.SingleOrDefault(p => p.PersonGuid == vote.PersonGuid);
        if (person != null)
        {
          vote.PersonCombinedInfo = person.CombinedInfo;
        }
      }

      //Db.Vote.Add(vote);
    }

    private void LoadResultInfo()
    {
      XmlNodeList nodes;
      try
      {
        nodes = _xmlRoot.SelectNodes("t:resultSummary", _nsm);
        if (nodes != null)
        {
          var toLoad = new List<ResultSummary>();
          foreach (XmlElement element in nodes)
          {
            var resultSummary = new ResultSummary();
            element.CopyAttributeValuesTo(resultSummary);
            resultSummary.ElectionGuid = _electionGuid;
            toLoad.Add(resultSummary);
          }
          Db.BulkInsert(toLoad);
          _hub.StatusUpdate("Loaded {0} summar{1}".FilledWith(toLoad.Count, toLoad.Count.Plural("ies", "y")));
        }
      }
      catch (Exception ex)
      {
        _hub.StatusUpdate("Failed to load result summaries. Manual overrides on Analyze page may be missing. (Error: {0})".FilledWith(ex.LastException().Message));
      }

      try
      {
        nodes = _xmlRoot.SelectNodes("t:resultTie", _nsm);
        if (nodes != null)
        {
          var toLoad = new List<ResultTie>();
          foreach (XmlElement element in nodes)
          {
            var resultTie = new ResultTie();
            element.CopyAttributeValuesTo(resultTie);
            resultTie.ElectionGuid = _electionGuid;
            toLoad.Add(resultTie);
          }
          Db.BulkInsert(toLoad);
          _hub.StatusUpdate("Loaded {0} tie{1}".FilledWith(toLoad.Count, toLoad.Count.Plural("s")));
        }
      }
      catch (Exception ex)
      {
        _hub.StatusUpdate("Failed to load tie information. Tie-break counts may be missing. (Error: {0})".FilledWith(ex.LastException().Message));
      }

      try
      {
        nodes = _xmlRoot.SelectNodes("t:result", _nsm);
        if (nodes != null)
        {
          var toLoad = new List<EF.Result>();
          foreach (XmlElement element in nodes)
          {
            var result = new EF.Result();
            element.CopyAttributeValuesTo(result);
            result.ElectionGuid = _electionGuid;
            UpdateGuidFromMapping(result, v => v.PersonGuid, Guid.Empty);
            toLoad.Add(result);
          }
          Db.BulkInsert(toLoad);
          _hub.StatusUpdate("Loaded {0} result{1}".FilledWith(toLoad.Count, toLoad.Count.Plural("s")));
        }


      }
      catch (Exception ex)
      {
        _hub.StatusUpdate("Failed to load some individual results and tie-break counts. Overall results will be recreated but tie-break informaion may be missing. (Error: {0})".FilledWith(ex.LastException().Message));
      }
    }


    private void LoadLog(XmlElement logXml, Guid locationGuid, List<C_Log> logs)
    {
      var log = new C_Log();
      logs.Add(log);
      logXml.CopyAttributeValuesTo(log);

      log.ElectionGuid = _electionGuid;
      log.LocationGuid = locationGuid;

      //Db.C_Log.Add(log);
    }

    private void UpdateGuidFromMapping<T, T2>(T obj, Expression<Func<T, T2>> action, Guid fallBack)
    {
      var prop = ((MemberExpression)action.Body).Member as PropertyInfo;
      var oldValue = prop.GetValue(obj, null) as Guid?;

      if (oldValue.HasValue && oldValue.Value != Guid.Empty)
      {
        var oldGuid = oldValue.Value;
        if (_guidMap.ContainsKey(oldGuid))
        {
          var value = _guidMap[oldGuid];
          prop.SetValue(obj, value, null);
        }
        else if (fallBack != Guid.Empty)
        {
          // incoming XML had a value, but we can't map it
          // pre 2.3.6, Locations did not export their Guid, so the voting location was lost
          prop.SetValue(obj, fallBack, null);
        }
        else
        {
          throw new LoaderException("Mapped Guid not found for {0}.{1}: {2}".FilledWith(obj.GetType().Name, prop.Name, oldGuid));
        }
      }
    }

    #region Nested type: LoaderException

    public class LoaderException : Exception
    {
      public LoaderException(string message, Exception innerException = null)
        : base(message, innerException)
      {
      }
    }

    #endregion

    #region Nested type: Result

    private class LoadResult
    {
      public Guid ElectionGuid;
      public string Message;
      public bool Success;
      public IEnumerable<object> Elections;

      /// <Summary>Failed result</Summary>
      public LoadResult(string message = "")
      {
        Success = false;
        Message = message;
      }

      /// <Summary>Success!</Summary>
      public LoadResult(Guid electionGuid)
      {
        Elections = new ElectionsListViewModel().MyElectionsInfo;
        ElectionGuid = electionGuid;
        Success = true;
      }
    }

    #endregion
  }
}