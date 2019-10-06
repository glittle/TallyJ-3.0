using System;

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