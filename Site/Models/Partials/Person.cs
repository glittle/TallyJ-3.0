using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.Models
{
  [MetadataType(typeof(PersonMetadata))]
  public partial class Person
  {

    private class PersonMetadata
    {
      [DebuggerDisplay("Local = {RegistrationTime.ToLocalTime()}, UTC = {RegistrationTime}")]
      public object RegistrationTime { get; set; }
    }

    /// <summary>
    /// Get all people for this election
    /// </summary>
    public static IEnumerable<Person> AllPeopleCached
    {
      get
      {
        var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

        if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

        var currentElectionGuid = UserSession.CurrentElectionGuid;

        return db.People.Where(p => p.ElectionGuid == currentElectionGuid).FromCache(null, new[] { "AllPeople" + currentElectionGuid });
      }
    }

    /// <summary>
    /// Drop the cache of people for this election
    /// </summary>
    public static void DropCachedPeople()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;

      CacheManager.Current.Expire("AllPeople" + UserSession.CurrentElectionGuid);
    }
  }
}