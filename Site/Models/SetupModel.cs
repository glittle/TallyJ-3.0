using TallyJ.Code;
using System.Linq;

namespace TallyJ.Models
{
  public class SetupModel : DataAccessibleModel
  {
    public int NumberOfPeople
    {
      get { return Db.People.Count(); }
    }
  }
}