using System.Configuration;
using System.Data.Entity.Infrastructure;
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

    public TallyJ2Entities DbContext
    {
      get
      {
        if (_db != null)
          return _db;

        var cnString = ConfigurationManager.ConnectionStrings["MainConnection"].ConnectionString + ";MultipleActiveResultSets=True";

        var cn = new SqlConnection(cnString);
        var ws = new MetadataWorkspace(new [] { "res://*/" },
                                       new [] { Assembly.GetExecutingAssembly() });
        var ec = new EntityConnection(ws, cn);

        return _db = new TallyJ2Entities(ec);
      }
    }
  }
}
