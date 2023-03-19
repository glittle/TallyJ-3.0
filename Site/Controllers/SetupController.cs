using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  [AllowTellersInActiveElection]
  public class SetupController : BaseController
  {

    [ForAuthenticatedTeller]
    public ActionResult Index()
    {
      return View("Setup", new SetupModel());
    }

    public ActionResult People()
    {
      return View(new SetupModel());
    }

    [ForAuthenticatedTeller]
    public ActionResult Notify()
    {
      return View();
    }

    [ForAuthenticatedTeller]
    public JsonResult SaveElection(Election election)
    {
      return new ElectionHelper().SaveElection(election);
    }

    [ForAuthenticatedTeller]
    public JsonResult SaveNotification(string emailSubject, string emailText, string smsText)
    {
      return new ElectionHelper().SaveNotification(emailSubject, emailText, smsText);
    }

    public JsonResult DetermineRules(string type, string mode)
    {
      return ElectionHelper.GetRules(type, mode).AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public ActionResult ImportCsv(ImportCsvModel importCsvModel)
    {
      if (importCsvModel == null)
      {
        importCsvModel = new ImportCsvModel();
      }

      return View(importCsvModel);
    }


    [ForAuthenticatedTeller]
    public void JoinBallotImportHub(string connId)
    {
      new BallotImportHub().Join(connId);
    }

    [ForAuthenticatedTeller]
    public ActionResult ImportBallots(ImportBallotsModel importBallotsModel)
    {
      if (importBallotsModel == null)
      {
        importBallotsModel = new ImportBallotsModel();
      }

      return View(importBallotsModel);
    }


    [ForAuthenticatedTeller]
    public ActionResult GetBallotUploadlist()
    {
      var importBallotsModel = new ImportBallotsModel();
      return importBallotsModel.GetUploadList();
    }


    [ForAuthenticatedTeller]
    public JsonResult UploadBallots()
    {
      var model = new ImportBallotsModel();
      var messages = model.ProcessUpload(out var rowId);

      return new
      {
        success = messages.HasNoContent(),
        rowId,
        messages
      }.AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public JsonResult GetBallotsPreviewInfo(int id, bool forceRefreshCache = false)
    {
      return new ImportBallotsModel().GetPreviewInfo(id, forceRefreshCache);
    }

    [ForAuthenticatedTeller]
    public JsonResult LoadBallotsFile(int id)
    {
      return new ImportBallotsModel().LoadFile(id);
    }

    [ForAuthenticatedTeller]
    public JsonResult RemoveImportedInfo()
    {
      return new ImportBallotsModel().RemoveImportedInfo();
    }


    [ForAuthenticatedTeller]
    public ActionResult DeleteBallotsFile(int id)
    {
      var importBallotsModel = new ImportBallotsModel();

      return importBallotsModel.DeleteFile(id);
    }

    //[ForAuthenticatedTeller]
    //public ActionResult ImportV1(ImportV1Model importV1Model)
    //{
    //  if (importV1Model == null)
    //  {
    //    importV1Model = new ImportV1Model();
    //  }

    //  return View(importV1Model);
    //}


    [ForAuthenticatedTeller]
    public JsonResult Upload()
    {
      var model = new ImportCsvModel();
      var messages = model.ProcessUpload(out var rowId);

      return new
      {
        success = messages.HasNoContent(),
        rowId,
        messages
      }.AsJsonResult();
    }

    //[ForAuthenticatedTeller]
    //public JsonResult UploadXml()
    //{
    //  var model = new ImportV1Model();
    //  int rowId;
    //  var messages = model.ProcessUpload(out rowId);

    //  return new { 
    //    success = messages.HasNoContent(),
    //    rowId, 
    //    messages
    //  }.AsJsonResult();
    //}


    [ForAuthenticatedTeller]
    public ActionResult Download(int id)
    {
      var fullFile = Db.ImportFile.SingleOrDefault(f => f.ElectionGuid == UserSession.CurrentElectionGuid && f.C_RowId == id);

      if (fullFile != null)
      {
        return File(fullFile.Contents, "application/octet-stream", fullFile.OriginalFileName);
      }
      return null;
    }

    [ForAuthenticatedTeller]
    public ActionResult DeleteFile(int id)
    {
      var importCsvModel = new ImportCsvModel();

      return importCsvModel.DeleteFile(id);
    }


    [ForAuthenticatedTeller]
    public ActionResult GetUploadlist()
    {
      var importCsvModel = new ImportCsvModel();
      return importCsvModel.GetUploadList();
    }

    //[ForAuthenticatedTeller]
    //public ActionResult GetUploadlistXml()
    //{
    //  var importV1Model = new ImportV1Model();
    //  return importV1Model.GetUploadList();
    //}

    [ForAuthenticatedTeller]
    public JsonResult SavePerson(Person person)
    {
      return new PeopleModel().SavePerson(person);
    }

    [ForAuthenticatedTeller]
    public JsonResult EditLocation(int id, string text)
    {
      return ContextItems.LocationModel.EditLocation(id, text);
    }

    [ForAuthenticatedTeller]
    public JsonResult SortLocations(List<int> ids)
    {
      return ContextItems.LocationModel.SortLocations(ids);
    }

    // [ForAuthenticatedTeller]
    // public JsonResult ResetInvolvementFlags()
    // {
    //   new PeopleModel().SetInvolvementFlagsToDefault();
    //   return true.AsJsonResult();
    // }

    [ForAuthenticatedTeller]
    public JsonResult ReadFields(int id)
    {
      return new ImportCsvModel().ReadFields(id);
    }

    [ForAuthenticatedTeller]
    public JsonResult CopyMap(int from, int to)
    {
      return new ImportCsvModel().CopyMap(from, to);
    }

    [ForAuthenticatedTeller]
    public JsonResult Import(int id)
    {
      return new ImportCsvModel().Import(id);
    }

    //[ForAuthenticatedTeller]
    //public JsonResult ImportXml(int id)
    //{
    //  return new ImportV1Model().Import(id);
    //}

    [ForAuthenticatedTeller]
    public JsonResult SendEmail(string list)
    {
      return new EmailHelper().SendHeadTellerEmail(list);
    }

    [ForAuthenticatedTeller]
    public JsonResult SendSms(string list)
    {
      return new TwilioHelper().SendHeadTellerMessage(list);
    }

    [ForAuthenticatedTeller]
    public JsonResult GetContacts()
    {
      return new EmailHelper().GetContacts();
    }
    [ForAuthenticatedTeller]
    public JsonResult GetContactLog(int lastLogId = 0)
    {
      return new EmailHelper().GetContactLog(lastLogId);
    }

    [ForAuthenticatedTeller]
    public FileResult DownloadContactLog()
    {
      return new EmailHelper().DownloadContactLog();
    }

    [ForAuthenticatedTeller]
    public JsonResult SaveMapping(int id, List<string> mapping)
    {
      return new ImportCsvModel().SaveMapping(id, mapping);
    }

    [ForAuthenticatedTeller]
    public JsonResult FileCodePage(int id, int cp)
    {
      // generic for csv or ballot files
      return new ImportCsvModel().SaveCodePage(id, cp);
    }

    [ForAuthenticatedTeller]
    public JsonResult FileDataRow(int id, int firstDataRow)
    {
      return new ImportCsvModel().SaveDataRow(id, firstDataRow);
    }

    [ForAuthenticatedTeller]
    public JsonResult DeleteAllPeople()
    {
      new LogHelper().Add("Deleted all people");

      return new PeopleModel().DeleteAllPeople();
    }

    // [ForAuthenticatedTeller]
    // public JsonResult DeleteAllPeopleAndBallots()
    // {
    //   new LogHelper().Add("Deleted all ballots and people");
    //
    //   Election.EraseBallotsAndResults(UserSession.CurrentElectionGuid);
    //
    //   return new PeopleModel().DeleteAllPeople();
    // }
  }
}