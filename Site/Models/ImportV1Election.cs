using System;
using System.Collections.Generic;
using System.Xml;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ImportV1Election : ImportV1Base
  {
    public ImportV1Election(IDbContext db, ImportFile file, XmlDocument xml, List<Person> people, Action<Person> registerPerson)
      : base(db, file, xml, people, registerPerson)
    {
    }

    public override void Process()
    {
      
    }
  }
}