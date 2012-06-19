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
    private TallyJ2dContext _tallyJ2Entities;

    #region IDbContextFactory Members

    public TallyJ2dContext DbContext
    {
      get
      {
        if (_tallyJ2Entities != null)
        {
          return _tallyJ2Entities;
        }

        //var cnString = "MultipleActiveResultSets=True;" + ConfigurationManager.ConnectionStrings["MainConnection"].ConnectionString;

        //var connection = new SqlConnection(cnString);
        //var workspace = new MetadataWorkspace(
        //  new[] { "res://*/" },
        //  new[] { typeof(SqlSearch_Result).Assembly }
        //  );

        
        //var entityConnection = new EntityConnection(workspace, connection);

        return _tallyJ2Entities = new TallyJ2dContext();
        //return _tallyJ2Entities = new TallyJ2dContext(entityConnection);
      }
    }

    #endregion
  }
}