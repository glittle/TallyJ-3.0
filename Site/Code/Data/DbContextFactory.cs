using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using TallyJ.EF;

namespace TallyJ.Code.Data
{
    public class DbContextFactory : IDbContextFactory
    {
      private ITallyJDbContext _tallyJ2Entities;

        #region IDbContextFactory Members

        public ITallyJDbContext DbContext
        {
            get
            {
                if (_tallyJ2Entities != null)
                {
                    return _tallyJ2Entities;
                }

                //var x = new ITallyJDbContext();
                //x.Configuration.ValidateOnSaveEnabled = true;

                var cnString = "MultipleActiveResultSets=True;" + ConfigurationManager.ConnectionStrings["MainConnection"].ConnectionString;

                var connection = new SqlConnection(cnString);
                var workspace = new MetadataWorkspace(
                  new[] { "res://*/" },
                  new[] { typeof(BallotCacher).Assembly }
                  );

                var entityConnection = new EntityConnection(workspace, connection);

                return _tallyJ2Entities = new TallyJ2dEntities(entityConnection);
            }
        }

        #endregion
    }
}