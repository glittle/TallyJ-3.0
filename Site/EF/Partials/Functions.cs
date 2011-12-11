using System;
using System.Data.Objects.DataClasses;

namespace tallyj2dModel.Store
{
  public class SqlFunc
  {
    [EdmFunction("tallyj2dModel.Store", "HasMatch")]
    public static bool HasMatch(string allTerms, string term1, string term2, bool? exactMatch)
    {
      throw new NotSupportedException("Direct calls not supported");
    }
  }
 
}