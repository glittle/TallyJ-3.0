using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ImportV1Model : DataConnectedModel
  {
    private const string FileTypeV1Community = "V1_Comm";
    private const string FileTypeV1Election = "V1_Elect";
    private const int Utf16CodePage = 1200;

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
      var fileSize = (int) inputStream.Length;

      var importFile = new ImportFile
                         {
                           ElectionGuid = UserSession.CurrentElectionGuid,
                           Contents = new byte[fileSize],
                           FileSize = fileSize,
                           OriginalFileName = name,
                           UploadTime = DateTime.Now,
                           ProcessingStatus = "Uploaded"
                         };

      var numWritten = inputStream.Read(importFile.Contents, 0, fileSize);
      if (numWritten != fileSize)
      {
        return "Read {0}. Should be {1}.".FilledWith(numWritten, fileSize);
      }

      try
      {
        var xml = GetXmlDoc(importFile);

        if (xml == null || xml.DocumentElement == null)
        {
          return "Invalid Xml file";
        }

        if (xml.DocumentElement.Name == "Community")
        {
          importFile.FileType = FileTypeV1Community;
        }
        else if (xml.DocumentElement.Name == "Election")
        {
          importFile.FileType = FileTypeV1Election;
        }
        else
        {
          return "Unexpected Xml file";
        }
      }
      catch (Exception e)
      {
        return e.Message;
      }

      Db.ImportFiles.Add(importFile);
      Db.SaveChanges();

      rowId = importFile.C_RowId;

      new LogHelper().Add("Uploaded file #" + importFile.C_RowId);

      return "";
    }

    public object PreviousUploads()
    {
      return Db.vImportFileInfoes
        .Where(vi => vi.ElectionGuid == UserSession.CurrentElectionGuid)
        .Where(vi => vi.FileType == FileTypeV1Community || vi.FileType == FileTypeV1Election)
        .OrderByDescending(vi => vi.UploadTime)
        .Select(vi => new
                        {
                          vi.C_RowId,
                          vi.FileSize,
                          vi.UploadTime,
                          vi.FileType,
                          vi.ProcessingStatus,
                          vi.OriginalFileName,
                          vi.CodePage,
                          vi.Messages
                        })
        .ToList();
    }

    public ActionResult DeleteFile(int id)
    {
      var targetFile = new ImportFile {C_RowId = id, ElectionGuid = UserSession.CurrentElectionGuid};
      Db.ImportFiles.Attach(targetFile);
      Db.ImportFiles.Remove(targetFile);
      Db.SaveChanges();

      new LogHelper().Add("Deleted file #" + id);

      return GetUploadList();
    }

    public ActionResult GetUploadList()
    {
      return new
               {
                 serverTime = DateTime.Now,
                 previousFiles = PreviousUploads()
               }.AsJsonResult();
    }


    public JsonResult Import(int rowId)
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      var file =
        Db.ImportFiles.SingleOrDefault(
          fi => fi.ElectionGuid == currentElectionGuid && fi.C_RowId == rowId);

      if (file == null)
      {
        throw new ApplicationException("File not found");
      }

      var xml = GetXmlDoc(file);

      if (xml == null || xml.DocumentElement == null)
      {
        throw new ApplicationException("Invalid Xml file");
      }

      var logHelper = new LogHelper();

      ImportV1Base importer;
      var currentPeople = Db.People.Where(p => p.ElectionGuid == currentElectionGuid).ToList();
      var personModel = new PeopleModel();


      switch (xml.DocumentElement.Name)
      {
        case "Community":
          importer = new ImportV1Community(Db, file, xml
                                           , currentPeople
                                           , delegate(Person person)
                                               {
                                                 personModel.ResetAllInfo(person);
                                                 person.ElectionGuid = currentElectionGuid;
                                                 Db.People.Add(person);
                                               }
                                           , logHelper);
          break;
        case "Election":

          var currentElection = UserSession.CurrentElection;
          var currentLocation = UserSession.CurrentLocation;
          if (currentLocation == null)
          {
            currentLocation = Db.Locations.OrderBy(l=>l.SortOrder).FirstOrDefault(l => l.ElectionGuid == currentElection.ElectionGuid);
            if (currentLocation == null)
            {
              throw  new ApplicationException("An election must have a Location before importing.");
            }
          }

          EraseElectionContents(currentElection);

          importer = new ImportV1Election(Db, file, xml
                                          , currentElection
                                          , currentLocation
                                          , ballot => Db.Ballots.Add(ballot)
                                          , vote => Db.Votes.Add(vote)
                                          , currentPeople
                                          , person =>
                                              {
                                                personModel.ResetAllInfo(person);
                                                Db.People.Add(person);
                                              }
                                          , summary => Db.ResultSummaries.Add(summary)
                                          , logHelper
            );
          break;
        default:
          throw new ApplicationException("Unexpected Xml file");
      }

      importer.Process();

      return importer.SendSummary();
    }

    /// <Summary>Totally erase all tally information</Summary>
    private void EraseElectionContents(Election election)
    {
      Db.EraseElectionContents(election.ElectionGuid, true, UserSession.LoginId);
    }

    private static XmlDocument GetXmlDoc(ImportFile file)
    {
      var xml = new XmlDocument();
      xml.Load(new MemoryStream(file.Contents));
      return xml;
    }
  }
}