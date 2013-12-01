using TallyJ.EF;
using TallyJ.EF;

namespace TallyJ.Code.Data
{
    public class DbContextFactory : IDbContextFactory
    {
      private TallyJ2dEntities _tallyJ2Entities;

        #region IDbContextFactory Members

        public TallyJ2dEntities DbContext
        {
            get
            {
                if (_tallyJ2Entities != null)
                {
                    return _tallyJ2Entities;
                }

                _tallyJ2Entities = new TallyJ2dEntities();

                _tallyJ2Entities.Configuration.ValidateOnSaveEnabled = true;

                return _tallyJ2Entities;


                //        var cnString = "MultipleActiveResultSets=True;" + ConfigurationManager.ConnectionStrings["MainConnection"].ConnectionString;
                //
                //        var connection = new SqlConnection(cnString);
                //        var workspace = new MetadataWorkspace(
                //          new[] { "res://*/" },
                //          new[] { typeof(SqlSearch_Result).Assembly }
                //          );
                //
                //        var entityConnection = new EntityConnection(workspace, connection);
                //
                //        return _tallyJ2Entities = new TallyJ2dEntities(entityConnection);
            }
        }

        #endregion
    }
}