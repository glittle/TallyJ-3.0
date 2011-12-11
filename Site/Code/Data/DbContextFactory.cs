using System.Configuration;
using System.Data.EntityClient;
using System.Data.Metadata.Edm;
using System.Data.SqlClient;
using System.Reflection;
using TallyJ.EF;

namespace TallyJ.Code.Data
{
  public class DbContextFactory : IDbContextFactory
  {
    private TallyJ2Entities _db;

    #region IDbContextFactory Members

    public TallyJ2Entities DbContext
    {
      get
      {
        if (_db != null)
          return _db;

        var cnString = ConfigurationManager.ConnectionStrings["MainConnection"].ConnectionString +
                       ";MultipleActiveResultSets=True";

        //  metadata=res://*/EF.MainData.csdl|res://*/EF.MainData.ssdl|res://*/EF.MainData.msl;provider=System.Data.SqlClient;provider connection string="data source=.;initial catalog=tallyj2d;integrated security=True;multipleactiveresultsets=True;App=EntityFramework"
        //  metadata=res://*/


        var cn = new SqlConnection(cnString);
        var ws = new MetadataWorkspace(
          new[] { "res://*/" },
          new[] { Assembly.GetExecutingAssembly() }
          );
        var ec = new EntityConnection(ws, cn);

        return _db = new TallyJ2Entities(ec);
      }
    }

    #endregion
  }
}