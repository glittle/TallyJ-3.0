using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using TallyJ.EF;

namespace TallyJ.Code.Data
{
  public class DbContextFactory : IDbContextFactory
  {
    private List<ITallyJDbContext> _tallyJ2Entities;

    public DbContextFactory()
    {
      _tallyJ2Entities = new List<ITallyJDbContext>();
    }

    #region IDbContextFactory Members

    public void CloseAll()
    {
      _tallyJ2Entities.ForEach(d => d.Dispose());
    }

    public ITallyJDbContext GetNewDbContext
    {
      get
      {
        //                if (_tallyJ2Entities != null)
        //                {
        //                    return _tallyJ2Entities;
        //                }

        //var x = new ITallyJDbContext();
        //x.Configuration.ValidateOnSaveEnabled = true;

        var cnString = "MultipleActiveResultSets=True;" + ConfigurationManager.ConnectionStrings["MainConnection3"].ConnectionString;

        var connection = new SqlConnection(cnString);
        var workspace = new MetadataWorkspace(
          new[] { "res://*/" },
          new[] { typeof(BallotCacher).Assembly }
          );

        var entityConnection = new EntityConnection(workspace, connection);

        var tallyJDbContext = new TallyJ2dEntities(entityConnection);
        _tallyJ2Entities.Add(tallyJDbContext);

        return tallyJDbContext;
      }
    }

    #endregion
  }
}