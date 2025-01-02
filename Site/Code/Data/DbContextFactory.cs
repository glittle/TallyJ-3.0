using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using System.Web.Configuration;
using TallyJ.EF;

namespace TallyJ.Code.Data
{
  public class DbContextFactory : IDbContextFactory
  {
    private List<ITallyJDbContext> _tallyEntities;

    public DbContextFactory()
    {
      _tallyEntities = new List<ITallyJDbContext>();
    }

    public void CloseAll()
    {
      _tallyEntities.ForEach(d => d.Dispose());
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

        var settings = WebConfigurationManager.ConnectionStrings["MainConnection3"];

        if (settings == null)
        {
          throw new ConfigurationErrorsException("MainConnection3 not found");
        }

        var cnString = "MultipleActiveResultSets=True;" + settings.ConnectionString;

        var connection = new SqlConnection(cnString);
        var workspace = new MetadataWorkspace(
          new[] { "res://*/" },
          new[] { typeof(BallotCacher).Assembly }
          );

        var entityConnection = new EntityConnection(workspace, connection);

        var tallyJDbContext = new TallyJEntities(entityConnection);
        _tallyEntities.Add(tallyJDbContext);

        return tallyJDbContext;
      }
    }

  }
}