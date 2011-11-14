using System.Linq;
using TallyJ.Code;

namespace TallyJ.Models
{
  public class ImportExportModel : DataAccessibleModel
  {
    public int NumberOfPeople
    {
      get { return Db.People.Count(); }
    }
  }
}