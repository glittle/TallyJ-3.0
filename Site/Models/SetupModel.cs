using TallyJ.Code;
using System.Linq;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class SetupModel : DataAccessibleModel
  {
    public int NumberOfPeople
    {
      get { return Db.People.Count(); }
    }

      public Election CurrentElection
      {
          get { return Db.Elections.Single(e => e.ElectionGuid == UserSession.CurrentElectionGuid); }
      }

      public object RulesForCurrentElection
      {
          get
          {
              var em = new ElectionModel();
              var currentElection = CurrentElection;
              var rules = em.GetRules(currentElection.ElectionType, currentElection.ElectionMode);

              return new
                         {
                             type = currentElection.ElectionType,
                             mode = currentElection.ElectionMode,
                             rules = rules.SerializedAsJson()
                         };
          }
      }
  }
}