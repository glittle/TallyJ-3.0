using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.EF
{
  public partial class Ballot : IIndexedForCaching
  {
    public long RowVersionInt
    {
      get
      {
        return BitConverter.ToInt64(C_RowVersion, 0);
      }
    }
  }
}