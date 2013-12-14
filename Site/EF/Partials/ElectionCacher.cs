using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ElectionCacher : CacherBase<Election>
  {
    protected override IQueryable<Election> MainQuery(TallyJ2dEntities db)
    {
      return db.Election.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid);
    }

    public Election CurrentElection
    {
      get
      {
        return AllForThisElection.First();
      }
    }


  }
}