using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.EF
{
  [MetadataType(typeof(PersonMetadata))]
  public partial class Person : IIndexedForCaching
  {
    //public long RowVersionInt {
    //  get
    //  {
    //    return BitConverter.ToInt64(C_RowVersion, 0);
    //  }
    //}

    public string FullName {
      get
      {
        // ((((([LastName]+coalesce((' ['+nullif([OtherLastNames],''))+']',''))+', ')+coalesce([FirstName],''))
        // +coalesce((' ['+nullif([OtherNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))
        return new[]
        {
          LastName,
          OtherLastNames.SurroundContentWith(" [", "]"),
          FirstName.SurroundContentWith(", ", ""),
          OtherNames.SurroundContentWith(" [", "]"),
          OtherInfo.SurroundContentWith(" (", ")")
        }.JoinedAsString("", true);
      }
    }
    
    public string FullNameFL {
      get
      {
        // ((((coalesce([FirstName]+' ','')+[LastName])+coalesce((' ['+nullif([OtherNames],''))+']',''))+
        //  coalesce((' ['+nullif([OtherLastNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))
        return new[]
        {
          FirstName.SurroundContentWith("", " "),
          LastName,
          OtherNames.SurroundContentWith(" [", "]"),
          OtherLastNames.SurroundContentWith(" [", "]"),
          OtherInfo.SurroundContentWith(" (", ")")
        }.JoinedAsString("", true);
      }
    }

    private class PersonMetadata
    {
      [DebuggerDisplay("Local = {RegistrationTime.ToLocalTime()}, UTC = {RegistrationTime}")]
      public object RegistrationTime { get; set; }
    }

    ///// <summary>
    ///// Get all people for this election
    ///// </summary>
    //public static IEnumerable<Person> AllPeopleCached
    //{
    //  get
    //  {
    //    var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

    //    if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

    //    var currentElectionGuid = UserSession.CurrentElectionGuid;

    //    return db.Person.Where(p => p.ElectionGuid == currentElectionGuid).FromCache(CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60)), new[] { "AllPeople" + currentElectionGuid });
    //  }
    //}

    ///// <summary>
    ///// Drop the cache of people for this election
    ///// </summary>
    //public static void DropCachedPeople()
    //{
    //  if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;

    //  CacheManager.Current.Expire("AllPeople" + UserSession.CurrentElectionGuid);
    //}
  }
}