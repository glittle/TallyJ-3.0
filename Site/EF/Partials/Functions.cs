using System;
using System.Data.Objects.DataClasses;

namespace TallyJ.EF
{
  public class SqlFunc
  {
    [EdmFunction("tallyj2dModel.Store", "HasMatch")]
    public static bool HasMatch(string allTerms, string term1, string term2)
    {
      throw new NotSupportedException("Direct calls not supported");
    }
  }
 
}