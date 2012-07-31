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
using TallyJ.Code;
using TallyJ.Code.Session;
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

    public JsonResult Import(HttpPostedFileBase file)
    {
      try
      {
        VerifyFileReceived(file);
        LoadIntoXmlDoc(file);
        ValidateXml();

        var success = LoadXmlToDatabase();

        return success
                 ? new Result(true, "", _electionGuid).AsJsonResult()
                 : new Result(false, "(Under construction!)").AsJsonResult();
      }
      catch (LoaderException ex)
      {
        return new Result(false, ex.GetAllMsgs("\n")).AsJsonResult();
      }
      catch (Exception ex)
      {
        // some unexpected exception...
        return new Result(false, ex.GetType().Name + ": " + ex.GetAllMsgs("\n")).AsJsonResult();
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

      _xmlDocument.Validate(delegate(object sender, ValidationEventArgs args)
        {
          if (args.Severity == XmlSeverityType.Error)
          {
            fatal = true;
          }
          issues.Add(args.Message);
        });

      if (fatal)
      {
        throw new LoaderException(issues.JoinedAsString("\n"));
      }

      _nsm = new XmlNamespaceManager(_xmlDocument.NameTable);
      _nsm.AddNamespace("t", Exporter.XmlNameSpace);

      // get the document element for later use
      _xmlRoot = _xmlDocument.DocumentElement;
    }

    private bool LoadXmlToDatabase()
    {
      _guidMap = new Dictionary<Guid, Guid>();

      using (var transaction = new TransactionScope())
      {
        try
        {
          // the xml document is validated and ready to load
          LoadElectionInfo();

          LoadPeople();

          LoadTellers();

          LoadLocationsEtc();
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

        transaction.Complete();
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
      var matching = Db.Elections.Where(e => e.Name == newName || e.Name.StartsWith(newName)).ToList();
      if (matching.Any())
      {
        if (matching.All(e => e.Name != newName))
        {
          // don't need to rename
        }
        else
        {
          var last = matching.OrderBy(e => e.Name).Last();
          var num = last.Name.Split(' ').Last().AsInt();
          _election.Name = string.Format("{0} - {1}", newName, num + 1);
        }
      }

      Db.Elections.Add(_election);
      Db.SaveChanges();

      // set current person as owner
      var join = new JoinElectionUser
        {
          ElectionGuid = _electionGuid,
          UserId = UserSession.UserGuid
        };
      Db.JoinElectionUsers.Add(join);
      Db.SaveChanges();
    }

    private void LoadPeople()
    {
      var peopleXml = _xmlRoot.SelectNodes("t:person", _nsm);

      if (peopleXml == null || peopleXml.Count == 0)
      {
        throw new LoaderException("No people in the file");
      }

      _peopleModel = new PeopleModel();

      foreach (XmlElement personXml in peopleXml)
      {
        LoadPerson(personXml);
      }

      Db.SaveChanges();
    }

    private void LoadPerson(XmlElement personXml)
    {
      // need to map Guid to new Guid
      var person = new Person();
      personXml.CopyAttributeValuesTo(person);

      // reset Guid to a new guid
      var oldGuid = person.PersonGuid;
      var newGuid = Guid.NewGuid();
      _guidMap.Add(oldGuid, newGuid);

      person.PersonGuid = newGuid;
      person.ElectionGuid = _electionGuid;

      // leave the "AtStart" alone, so we preserve change from when the election was originally set up
      _peopleModel.SetCombinedInfos(person);

      Db.People.Add(person);
    }

    private void LoadTellers()
    {
      var tellersXml = _xmlRoot.SelectNodes("t:teller", _nsm);
      if (tellersXml == null) return;

      foreach (XmlElement tellerXml in tellersXml)
      {
        LoadTeller(tellerXml);
      }

      Db.SaveChanges();
    }

    private void LoadTeller(XmlElement tellerXml)
    {
      // need to map Guid to new Guid
      var teller = new Teller();
      tellerXml.CopyAttributeValuesTo(teller);

      // reset Guid to a new guid
      var oldGuid = teller.TellerGuid;
      var newGuid = Guid.NewGuid();
      _guidMap.Add(oldGuid, newGuid);

      Debugger.Log(3, "Teller", "Teller {0}-->{1}\n".FilledWith(oldGuid, newGuid));

      teller.TellerGuid = newGuid;
      teller.ElectionGuid = _electionGuid;

      Debugger.Log(3, "Teller", "Teller={0}\n".FilledWith(teller.TellerGuid));

      Db.Tellers.Add(teller);
    }

    private void LoadLocationsEtc()
    {
      var locationsXml = _xmlRoot.SelectNodes("t:location", _nsm);

      if (locationsXml == null || locationsXml.Count == 0)
      {
        throw new LoaderException("No locations in the file");
      }

      foreach (XmlElement locationXml in locationsXml)
      {
        LoadLocationEtc(locationXml);
      }
    }

    private void LoadLocationEtc(XmlElement locationXml)
    {
      // need to map Guid to new Guid
      var location = new Location();
      locationXml.CopyAttributeValuesTo(location);

      // reset Guid to a new guid
      var oldGuid = location.LocationGuid;
      var newGuid = Guid.NewGuid();
      _guidMap.Add(oldGuid, newGuid);

      var locationGuid = location.LocationGuid = newGuid;
      location.ElectionGuid = _electionGuid;

      Db.Locations.Add(location);
      Db.SaveChanges();

      var computersXml = locationXml.SelectNodes("t:computer", _nsm);

      if (computersXml != null)
      {
        foreach (XmlElement computerXml in computersXml)
        {
          LoadComputer(computerXml, locationGuid);
        }
      }
      Db.SaveChanges();

      var ballotsXml = locationXml.SelectNodes("t:ballot", _nsm);
      if (ballotsXml != null)
      {
        foreach (XmlElement ballotXml in ballotsXml)
        {
          LoadBallot(ballotXml, locationGuid);
        }
      }
      Db.SaveChanges();

      var logsXml = locationXml.SelectNodes("t:log", _nsm);
      if (logsXml != null)
      {
        foreach (XmlElement logXml in logsXml)
        {
          LoadLog(logXml, locationGuid);
        }
      }
      Db.SaveChanges();

      var logger = new LogHelper(_electionGuid);
      logger.Add("Loaded election from file");
      Db.SaveChanges();
    }

    private void LoadComputer(XmlElement computerXml, Guid locationGuid)
    {
      // need to map Guid to new Guid
      var computer = new Computer();
      computerXml.CopyAttributeValuesTo(computer);

      // reset Guid to a new guid
      computer.ElectionGuid = _electionGuid;
      computer.LocationGuid = locationGuid;

      Db.Computers.Add(computer);
    }

    private void LoadBallot(XmlElement ballotXml, Guid locationGuid)
    {
      var ballot = new Ballot();
      ballotXml.CopyAttributeValuesTo(ballot);

      // reset Guid to a new guid
      ballot.BallotGuid = Guid.NewGuid();
      ballot.LocationGuid = locationGuid;

      UpdateGuidFromMapping(ballot, b => b.TellerAssisting);
      UpdateGuidFromMapping(ballot, b => b.TellerAtKeyboard);

      Db.Ballots.Add(ballot);
      Db.SaveChanges();

      var votesXml = ballotXml.SelectNodes("t:vote", _nsm);
      if (votesXml != null)
      {
        var sequence = 1;
        foreach (XmlElement voteXml in votesXml)
        {
          LoadVote(voteXml, sequence++, ballot.BallotGuid);
        }
      }
    }

    private void LoadVote(XmlElement voteXml, int sequence, Guid ballotGuid)
    {
      var vote = new Vote();
      voteXml.CopyAttributeValuesTo(vote);

      vote.PositionOnBallot = sequence;
      vote.BallotGuid = ballotGuid;

      UpdateGuidFromMapping(vote, v => v.PersonGuid);
      var person = Db.People.Single(p => p.PersonGuid == vote.PersonGuid);
      vote.PersonCombinedInfo = person.CombinedInfo;
    }

    private void LoadLog(XmlElement logXml, Guid locationGuid)
    {
      var log = new C_Log();
      logXml.CopyAttributeValuesTo(log);

      // reset Guid to a new guid
      log.ElectionGuid = _electionGuid;
      log.LocationGuid = locationGuid;

      Db.C_Log.Add(log);
    }

    private void UpdateGuidFromMapping<T, T2>(T obj, Expression<Func<T, T2>> action)
    {
      var prop = ((MemberExpression) action.Body).Member as PropertyInfo;
      var oldValue = prop.GetValue(obj, null) as Guid?;

      if (oldValue.HasValue && oldValue.Value != Guid.Empty)
      {
        var oldGuid = oldValue.Value;
        if (_guidMap.ContainsKey(oldGuid))
        {
          prop.SetValue(obj, _guidMap[oldGuid], null);
        }
        else
        {
          throw new LoaderException("Mapped Guid not found for {0}: {1}".FilledWith(obj.GetType().Name, oldGuid));
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

    private class Result
    {
      public Guid ElectionGuid;
      public string Message;
      public bool Success;

      public Result(bool success, string message = "", Guid electionGuid = default(Guid))
      {
        Success = success;
        Message = message;
      }
    }

    #endregion
  }
}