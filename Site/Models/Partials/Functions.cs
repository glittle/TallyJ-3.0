using System;
using System.Data.Entity;
using System.Data.Entity.Core.Objects.DataClasses;

namespace tallyj2dModel.Store // namespace must be:  tallyj2dModel.Store
{
  public class SqlFunc
  {
    [DbFunction("tallyj2dModel.Store", "HasMatch")]
    public static bool HasMatch(string allTerms, string term1, string term2, bool? exactMatch)
    {
      throw new NotSupportedException("Direct calls not supported");
    }
  }


  //  // debugging code
  //var defaultContainerName = ((IObjectContextAdapter)this).ObjectContext.DefaultContainerName;
  //var metadataWorkspace = ((IObjectContextAdapter)this).ObjectContext.MetadataWorkspace;
  //var findFunction = metadataWorkspace.GetItems(DataSpace.SSpace)
  //          .SelectMany(gi => gi.MetadataProperties)
  //          .Where(m=> Equals(m.Value, "SqlSearch"))
  //          .Select(m => "Found {0}".FilledWith(m.Value))
  //          .FirstOrDefault();

  //var logger = LogManager.GetCurrentClassLogger();
  //logger.Info(string.Format("Env: {0}\nContainer Name: {1}\nFound? {2}", new SiteInfo().CurrentEnvironment, defaultContainerName, findFunction));


}