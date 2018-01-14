using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using EntityFramework.Extensions;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
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
      var fileSize = (int)inputStream.Length;

      var record = new ImportFile
                         {
                           ElectionGuid = UserSession.CurrentElectionGuid,
                           Contents = new byte[fileSize],
                           FileSize = fileSize,
                           OriginalFileName = name,
                           UploadTime = DateTime.Now,
                           ProcessingStatus = "Uploaded"
                         };

      var numWritten = inputStream.Read(record.Contents, 0, fileSize);
      if (numWritten != fileSize)
      {
        return "Read {0}. Should be {1}.".FilledWith(numWritten, fileSize);
      }

      new ImportHelper().ExtraProcessingIfMultipartEncoded(record);

      try
      {
        var xml = GetXmlDoc(record);

        if (xml == null || xml.DocumentElement == null)
        {
          return "Invalid Xml file";
        }

        if (xml.DocumentElement.Name == "Community")
        {
          record.FileType = FileTypeV1Community;
        }
        else if (xml.DocumentElement.Name == "Election")
        {
          record.FileType = FileTypeV1Election;
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

      Db.ImportFile.Add(record);
      Db.SaveChanges();

      rowId = record.C_RowId;

      new LogHelper().Add("Uploaded file #" + record.C_RowId);

      return "";
    }

    public object PreviousUploads()
    {
      var timeOffset = UserSession.TimeOffsetServerAhead;

      return Db.ImportFile
        .Where(vi => vi.ElectionGuid == UserSession.CurrentElectionGuid)
        .Where(vi => vi.FileType == FileTypeV1Community || vi.FileType == FileTypeV1Election)
        .OrderByDescending(vi => vi.UploadTime)
        .ToList()
        .Select(vi => new
                        {
                          vi.C_RowId,
                          vi.FileSize,
                          UploadTime = vi.UploadTime.GetValueOrDefault().AddMilliseconds(0 - timeOffset),
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
      var targetFile = new ImportFile { C_RowId = id, ElectionGuid = UserSession.CurrentElectionGuid };
      Db.ImportFile.Attach(targetFile);
      Db.ImportFile.Remove(targetFile);
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
        Db.ImportFile.SingleOrDefault(
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
      var currentPeople = new PersonCacher(Db).AllForThisElection.ToList();
      var personModel = new PeopleModel();


      switch (xml.DocumentElement.Name)
      {
        case "Community":
          importer = new ImportV1Community(Db, file, xml
                                           , currentPeople
                                           , delegate(Person person)
                                               {
                                                 personModel.SetCombinedInfoAtStart(person);
                                                 person.ElectionGuid = currentElectionGuid;
                                                 Db.Person.Add(person);
                                               }
                                           , logHelper);
          break;
        case "Election":

          var currentElection = UserSession.CurrentElection;
          var currentLocation = UserSession.CurrentLocation;
          if (currentLocation == null)
          {
            currentLocation = new LocationCacher(Db).AllForThisElection.OrderBy(l => l.SortOrder).FirstOrDefault();
            if (currentLocation == null)
            {
              throw new ApplicationException("An election must have a Location before importing.");
            }
          }

          Election.EraseBallotsAndResults(currentElection.ElectionGuid);

          importer = new ImportV1Election(Db, file, xml
                                          , currentElection
                                          , currentLocation
                                          , ballot => Db.Ballot.Add(ballot)
                                          , vote => Db.Vote.Add(vote)
                                          , currentPeople
                                          , person =>
                                              {
                                                personModel.SetCombinedInfoAtStart(person);
                                                Db.Person.Add(person);
                                              }
                                          , summary => Db.ResultSummary.Add(summary)
                                          , logHelper
            );
          break;
        default:
          throw new ApplicationException("Unexpected Xml file");
      }

      importer.Process();

      var resultsModel = new ResultsModel();
      resultsModel.GenerateResults();

      return importer.SendSummary();
    }

    private static XmlDocument GetXmlDoc(ImportFile file)
    {
      var xml = new XmlDocument();
      xml.Load(new MemoryStream(file.Contents));
      return xml;
    }
  }
}