using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;
using TallyJ.Models.Helper;

namespace TallyJ.Models
{
  public class PeopleModel : DataConnectedModel
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
    ///     Process each person record, preparing it BEFORE the election starts
    /// </summary>
    public void CleanAllPersonRecordsBeforeStarting()
    {
      foreach (var person in Db.People)
      {
        ResetCombinedInfos(person);
        ClearVotingInfo(person);
        ClearEligibilityRestrictions(person);
      }
      Db.SaveChanges();
    }

    /// <Summary>Only to be done before an election</Summary>
    public void ResetCombinedInfos(Person person)
    {
      person.CombinedInfoAtStart =
        person.CombinedInfo = person.MakeCombinedInfo();

      person.UpdateCombinedSoundCodes();
    }

    public void SetCombinedInfos(Person person)
    {
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
    ///     Use age group to determine eligibility.
    /// </summary>
    /// <param name="person"></param>
    public void ClearEligibilityRestrictions(Person person)
    {
      var canVote = person.AgeGroup.HasNoContent() || person.AgeGroup == AgeGroup.Adult;

      person.IneligibleReasonGuid = canVote ? null : InelligibleReason.NotAdult;

      var whoCanVote = UserSession.CurrentElection.CanVote;
      var whoCanReceiveVotes = UserSession.CurrentElection.CanReceive;

      person.CanVote = whoCanVote == "A";
      person.CanReceiveVotes = whoCanReceiveVotes == "A";
    }

    public JsonResult DetailsFor(int personId)
    {
      var person =
        Db.People.SingleOrDefault(p => p.C_RowId == personId && p.ElectionGuid == UserSession.CurrentElectionGuid);

      if (person == null)
      {
        return new
                 {
                   Error = "Unknown person"
                 }.AsJsonResult();
      }

      //var whoCanVote = UserSession.CurrentElection.CanVote;
      //var whoCanReceiveVotes = UserSession.CurrentElection.CanReceive;

      return new
               {
                 Person = PersonForEdit(person)
               }.AsJsonResult();
    }

    private static object PersonForEdit(Person person)
    {
      return new
               {
                 person.C_RowId,
                 person.AgeGroup,
                 person.BahaiId,
                 person.CanReceiveVotes,
                 person.CanVote,
                 person.FirstName,
                 person.IneligibleReasonGuid,
                 person.LastName,
                 person.OtherInfo,
                 person.OtherLastNames,
                 person.OtherNames,
               };
    }

    public JsonResult SavePerson(Person personFromInput)
    {
      var savedPerson =
        Db.People.SingleOrDefault(p => p.C_RowId == personFromInput.C_RowId && p.ElectionGuid == UserSession.CurrentElectionGuid);
      if (savedPerson == null)
      {
        return new
                 {
                   Status = "Unknown ID"
                 }.AsJsonResult();
      }

      var editableFields = new 
                             {
                               personFromInput.AgeGroup,
                               personFromInput.BahaiId,
                               personFromInput.CanReceiveVotes,
                               personFromInput.CanVote,
                               personFromInput.FirstName,
                               personFromInput.IneligibleReasonGuid,
                               personFromInput.LastName,
                               personFromInput.OtherInfo,
                               personFromInput.OtherLastNames,
                               personFromInput.OtherNames
                             }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray();

      if (personFromInput.IneligibleReasonGuid == Guid.Empty)
      {
        personFromInput.IneligibleReasonGuid = null;
      }

      var changed = personFromInput.CopyPropertyValuesTo(savedPerson, editableFields);


      if (changed)
      {
        SetCombinedInfos(savedPerson);

        Db.SaveChanges();
      }

      return new
               {
                 Status = "Saved",
                 Person = PersonForEdit(savedPerson)
               }.AsJsonResult();
    }
  }
}