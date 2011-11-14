using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;
using TallyJ.Models.Helper;

namespace TallyJ.Models
{
  public class PeopleModel : DataAccessibleModel
  {
    public IQueryable<Person> PeopleInCurrentElection(bool includeInelligible)
    {
      {
        return Db.People
          .Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid)
          .Where(p => includeInelligible || p.IneligibleReasonGuid == null);
      }
    }


    /// <summary>
    /// Process each person record, preparing it BEFORE the election starts
    /// </summary>
    public void CleanAllPersonRecordsBeforeStarting()
    {
      foreach (var person in Db.People)
      {
        SetCombinedInfos(person);
        ClearVotingInfo(person);
        ClearEligibilityRestrictions(person);
      }
      Db.SaveChanges();
    }

    public void SetCombinedInfos(Person person)
    {
      person.CombinedInfoAtStart =
        person.CombinedInfo = person.MakeCombinedInfo();

      person.UpdateCombinedSoundCodes();
    }

    public void ClearVotingInfo(Person person)
    {
      person.RegistrationTime = null;
      person.VotingLocationGuid = null;
      person.VotingMethod = null;
      person.EnvNum = null;
    }

    /// <summary>
    /// Use age group to determine eligibility.
    /// </summary>
    /// <param name="person"></param>
    public void ClearEligibilityRestrictions(Person person)
    {
      var canVote = person.AgeGroup.HasNoContent() || person.AgeGroup == AgeGroup.Adult;

      person.IneligibleReasonGuid = canVote ? null : InelligibleReason.NotAdult;

      person.CanVote = canVote;
      person.CanReceiveVotes = canVote;
    }
  }
}