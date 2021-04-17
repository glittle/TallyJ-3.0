using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.Owin.Security.DataHandler;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.Controllers;
using TallyJ.CoreModels.ExportImport;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ImportBallotsModel : DataConnectedModel
  {
    private const string FileTypeXml = "XML";
    private string _tempMsg = "";

    /*
     *
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
          UploadTime = vi.UploadTime.GetValueOrDefault().AddMilliseconds(0 - timeOffset),
          vi.ProcessingStatus,
          vi.OriginalFileName,
          vi.CodePage,
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
      if (inputStream.Length == 0)
      {
        return "No file received";
      }

      var name = HttpUtility.UrlDecode(httpRequest.Headers["X-File-Name"].DefaultTo("unknown name"));
      var fileSize = (int)inputStream.Length;

      var record = new ImportFile
      {
        ElectionGuid = UserSession.CurrentElectionGuid,
        Contents = new byte[fileSize],
        FileSize = fileSize,
        OriginalFileName = name,
        UploadTime = DateTime.Now,
        FileType = FileTypeXml,
        ProcessingStatus = "Uploaded"
      };

      var numWritten = inputStream.Read(record.Contents, 0, fileSize);
      if (numWritten != fileSize)
      {
        return "Read {0}. Should be {1}.".FilledWith(numWritten, fileSize);
      }

      record.CodePage = ImportHelper.DetectCodePage(record.Contents);

      ImportHelper.ExtraProcessingIfMultipartEncoded(record);

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
      {
        return new
        {
          failed = true,
          result = new[] { "File not found" }
        }.AsJsonResult();
      }



      return new
      {
      }.AsJsonResult();
    }

    public JsonResult GetPreviewInfo(int id)
    {
      var importFile =
        Db.ImportFile.SingleOrDefault(
          fi => fi.ElectionGuid == UserSession.CurrentElectionGuid && fi.C_RowId == id);
      if (importFile == null)
      {
        throw new ApplicationException("File not found");
      }

      var electionInfo = ParseCdnBallotsFile(importFile);

      return new
      {
        numBallots = electionInfo.Ballots.Count,
        voters = electionInfo.Voters,
        electionInfo.locality,
        electionInfo.ImportErrors,
        electionInfo.HasUnregistered
      }.AsJsonResult();
    }

    private CdnImportDto ParseCdnBallotsFile(ImportFile importFile)
    {
      var xml = new XmlDocument();
      var electionInfo = new CdnImportDto();
      string xmlString;

      try
      {
        var importFileCodePage = importFile.CodePage ?? ImportHelper.DetectCodePage(importFile.Contents);
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
          var ballot = new Ballot();
          incomingBallot.CopyAttributeValuesTo(ballot);
          electionInfo.Ballots.Add(ballot);


          foreach (XmlElement incomingVote in incomingBallot.SelectNodes("vote"))
          {
            var voteLine = new VoteLine();
            incomingVote.CopyAttributeValuesTo(voteLine);
            voteLine.Text = incomingVote.InnerText;
            ballot.Votes.Add(voteLine);
          }
        }
      }
      catch (Exception e)
      {
        electionInfo.ImportErrors.Add(e.GetBaseException().Message);
        return electionInfo;
      }

      AnalyzeImportFile(electionInfo);

      return electionInfo;
    }

    private void AnalyzeImportFile(CdnImportDto electionInfo)
    {

      // file is good; analyze more
      if (electionInfo.Voters.Count != electionInfo.Ballots.Count)
      {
        electionInfo.ImportErrors.Add($"The number of voters ({electionInfo.Voters.Count}) and the number of ballots ({electionInfo.Ballots.Count}) must match.");
      }

      // check if these people are valid

      var knownPeople = new PersonCacher(Db).AllForThisElection;

      foreach (var incomingVoter in electionInfo.Voters)
      {
        var matched = knownPeople
          // match on Bahai ID (valid for Canada)
          .Where(p => p.BahaiId == incomingVoter.bahaiid)
          .ToList();

        if (matched.Count == 1)
        {
          // found them!
          var person = matched[0];

          incomingVoter.PersonGuid = person.PersonGuid;
          incomingVoter.VotingMethod = VotingMethodEnum.TextFor(person.VotingMethod);

          if (!person.CanVote.GetValueOrDefault())
          {
            electionInfo.ImportErrors.Add($"{person.FullNameFL} is not able vote.");
          }
        }
        else
        {
          electionInfo.ImportErrors.Add($"The Bahá'í ID ({incomingVoter.bahaiid}) for {incomingVoter.firstname} {incomingVoter.lastname} is not registered in TallyJ.");
          electionInfo.HasUnregistered = true;
        }
      }

      var numAlreadyVoted = electionInfo.Voters.Count(v => v.VotingMethod.HasContent());
      if (numAlreadyVoted > 0)
      {
        electionInfo.ImportErrors.Add($"{numAlreadyVoted.Plural("Some people have", "A person has")} already voted. You must change their registration on the Front Desk, if appropriate.");
      }

      // check ballots
      var knownBallots = new BallotCacher(Db).AllForThisElection;
      foreach (var incomingBallot in electionInfo.Ballots)
      {
        var matched = knownBallots.FirstOrDefault(b => b.BallotGuid == incomingBallot.guid);
        if (matched != null)
        {
          incomingBallot.AlreadyLoaded = true;
        }
      }

      var numBallotsAlreadyIn = electionInfo.Ballots.Count(b => b.AlreadyLoaded);
      if (numBallotsAlreadyIn > 0)
      {
        if (numBallotsAlreadyIn == electionInfo.Ballots.Count)
        {
          electionInfo.ImportErrors.Add("All ballots already loaded");
        }
        else
        {
          electionInfo.ImportErrors.Add($"{numBallotsAlreadyIn} {numBallotsAlreadyIn.Plural()} already loaded.");
        }
      }
    }

    // custom for Canadian import.  If/when more are added, can make more flexible.

    [Serializable]
    public class CdnImportDto
    {
      public CdnImportDto()
      {
        ImportErrors = new List<string>();
        Voters = new List<Voter>();
        Ballots = new List<Ballot>();
      }

      public DateTime timestamp { get; set; }
      public string locality { get; set; }
      public List<Voter> Voters { get; set; }
      public List<Ballot> Ballots { get; set; }
      public List<string> ImportErrors { get; set; }
      public bool HasUnregistered { get; set; }
    }

    [Serializable]
    public class Voter
    {
      public string bahaiid { get; set; }
      public string firstname { get; set; }
      public string lastname { get; set; }
      public Guid PersonGuid { get; set; }
      public string VotingMethod { get; set; }
    }

    [Serializable]
    public class Ballot
    {
      public Ballot()
      {
        Votes = new List<VoteLine>();
      }

      public Guid guid { get; set; }
      public List<VoteLine> Votes { get; set; }
      public bool AlreadyLoaded { get; set; }
    }

    [Serializable]
    public class VoteLine
    {
      public string Text { get; set; }
      public int index { get; set; }
    }

    public JsonResult LoadFile(int id)
    {
      return new
      {
        result = new List<string>
        {
          "Not done"
        }
      }.AsJsonResult();
    }
  }
}