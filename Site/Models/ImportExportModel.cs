using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ImportExportModel : DataConnectedModel
  {
    //public HttpPostedFileBase File { get; set; }
    //public Exception Exception { get; set; }
    //public UploadSession UploadSession { get; set; }

    public string ProcessUpload()
    {
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

      //var buffer = new byte[fileSize];
      var numWritten = inputStream.Read(record.Contents, 0, fileSize);
      if (numWritten != fileSize)
      {
        return "Read {0}. Should be {1}.".FilledWith(numWritten, fileSize);
      }

      Db.ImportFiles.Add(record);
      Db.SaveChanges();

      return "";
    }

    public object PreviousUploads()
    {
      return Db.vImportFileInfoes
        .Where(vi => vi.ElectionGuid == UserSession.CurrentElectionGuid)
        .OrderByDescending(vi => vi.UploadTime)
        .Select(vi => new
                        {
                          vi.C_RowId,
                          vi.FileSize,
                          vi.UploadTime,
                          vi.FileType,
                          vi.ProcessingStatus,
                          vi.OriginalFileName,
                          vi.Messages
                        })
        .ToList();
    }

    public ActionResult DeleteFile(int id)
    {
      var targetFile = new ImportFile { C_RowId = id, ElectionGuid = UserSession.CurrentElectionGuid };
      Db.ImportFiles.Attach(targetFile);
      Db.ImportFiles.Remove(targetFile);
      Db.SaveChanges();

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
  }
}