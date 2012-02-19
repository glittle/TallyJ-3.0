using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Xml;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.Models
{
  public abstract class ImportV1Base
  {
    protected readonly IDbContext _db;
    protected readonly ImportFile _file;
    protected readonly XmlDocument _xmlDoc;

    protected readonly List<Person> _people;
    protected readonly Action<Person> AddPerson;
    protected readonly ILogHelper _logHelper;

    protected ImportV1Base(IDbContext db, ImportFile file, XmlDocument xmlDoc, List<Person> people, Action<Person> addPerson, ILogHelper logHelper)
    {
      _db = db;
      _file = file;
      _xmlDoc = xmlDoc;
      _people = people;
      AddPerson = addPerson;
      _logHelper = logHelper;
    }

    protected string ImportSummaryMessage;

    public JsonResult SendSummary()
    {
      return new
               {
                 importReport = ImportSummaryMessage
               }.AsJsonResult();

    }

    public abstract void Process();
  }
}