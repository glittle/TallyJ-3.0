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
    private TallyJ2Entities _tallyJ2Entities;

    #region IDbContextFactory Members

    public TallyJ2Entities DbContext
    {
      get
      {
        if (_tallyJ2Entities != null)
        {
          return _tallyJ2Entities;
        }

        var cnString = "MultipleActiveResultSets=True;" + ConfigurationManager.ConnectionStrings["MainConnection"].ConnectionString;

        //  metadata=res://*/EF.MainData.csdl|res://*/EF.MainData.ssdl|res://*/EF.MainData.msl;provider=System.Data.SqlClient;provider connection string="data source=.;initial catalog=tallyj2d;integrated security=True;multipleactiveresultsets=True;App=EntityFramework"
        //  metadata=res://*/


        var connection = new SqlConnection(cnString);
        var workspace = new MetadataWorkspace(
          new[] { "res://*/" },
          new[] { typeof(SqlSearch_Result).Assembly }
          );

        
        var entityConnection = new EntityConnection(workspace, connection);

        return _tallyJ2Entities = new TallyJ2Entities(entityConnection);
      }
    }

    #endregion
  }
}