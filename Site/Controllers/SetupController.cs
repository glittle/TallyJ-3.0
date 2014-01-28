using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Mvc;
//using Krystalware.SlickUpload;
//using Krystalware.SlickUpload.Storage.Streams;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  [AllowGuestsInActiveElection]
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
    public JsonResult SaveElection(Election election)
    {
      return new ElectionModel().SaveElection(election);
    }

    public JsonResult DetermineRules(string type, string mode)
    {
      return new ElectionModel().GetRules(type, mode).AsJsonResult();
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
    public ActionResult ImportV1(ImportV1Model importV1Model)
    {
      if (importV1Model == null)
      {
        importV1Model = new ImportV1Model();
      }

      return View(importV1Model);
    }


    [ForAuthenticatedTeller]
    public JsonResult Upload()
    {
      var model = new ImportCsvModel();
      int rowId;
      var messages = model.ProcessUpload(out rowId);

      return new { 
        success = messages.HasNoContent(),
        rowId, 
        messages
      }.AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public JsonResult UploadXml()
    {
      var model = new ImportV1Model();
      int rowId;
      var messages = model.ProcessUpload(out rowId);

      return new { 
        success = messages.HasNoContent(),
        rowId, 
        messages
      }.AsJsonResult();
    }


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

    [ForAuthenticatedTeller]
    public ActionResult GetUploadlistXml()
    {
      var importV1Model = new ImportV1Model();
      return importV1Model.GetUploadList();
    }

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

    [ForAuthenticatedTeller]
    public JsonResult ResetInvolvementFlags()
    {
      new PeopleModel().SetInvolvementFlagsToDefault();
      return true.AsJsonResult();
    }

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
  
    [ForAuthenticatedTeller]
    public JsonResult ImportXml(int id)
    {
      return new ImportV1Model().Import(id);
    }

    [ForAuthenticatedTeller]
    public JsonResult SaveMapping(int id, List<string> mapping)
    {
      return new ImportCsvModel().SaveMapping(id, mapping);
    }

    [ForAuthenticatedTeller]
    public JsonResult FileCodePage(int id, int cp)
    {
      return new ImportCsvModel().SaveCodePage(id, cp);
    }
    
    [ForAuthenticatedTeller]
    public JsonResult DeleteAllPeople()
    {
      new LogHelper().Add("Deleted all people");

      return new PeopleModel().DeleteAllPeople();
    }

    [ForAuthenticatedTeller]
    public JsonResult DeleteAllPeopleAndBallots()
    {
      new LogHelper().Add("Deleted all ballots and people");

      Election.EraseBallotsAndResults(UserSession.CurrentElectionGuid);

      return new PeopleModel().DeleteAllPeople();
    }
  }
}