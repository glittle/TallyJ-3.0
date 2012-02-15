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
    protected int peopleAdded = 0;
    protected int nonAdults = 0;
    protected int alreadyLoaded = 0;

    protected readonly List<Person> _people;
    protected readonly Action<Person> _registerPerson;

    protected ImportV1Base(IDbContext db, ImportFile file, XmlDocument xmlDoc, List<Person> people, Action<Person> registerPerson)
    {
      _db = db;
      _file = file;
      _xmlDoc = xmlDoc;
      _people = people;
      _registerPerson = registerPerson;
    }

    public JsonResult Finalize()
    {
      _file.ProcessingStatus = "Imported";

      _db.SaveChanges();

      var result = "Imported {0} {1}.".FilledWith(peopleAdded, peopleAdded.Plural("people", "person"));
      if (alreadyLoaded > 0)
      {
        result += " Skipped {0} {1} matching existing.".FilledWith(alreadyLoaded, alreadyLoaded.Plural("people", "person"));
      }
      if (nonAdults > 0)
      {
        result += " Skipped {0} non-adult{1}.".FilledWith(nonAdults, nonAdults.Plural());
      }

      LogHelper.Add("Imported Xml v1 file #" + _file.C_RowId + ": " + result);

      return new
               {
                 result
               }.AsJsonResult();

    }

    public abstract void Process();
  }
}