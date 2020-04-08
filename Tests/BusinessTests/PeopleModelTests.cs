using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;
using TallyJ.CoreModels;
using TallyJ.EF;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class PeopleModelTests
  {

    [TestMethod]
    public void CorrectNumberOfVotes_Forced_Test()
    {
      var model = new PeopleModel();

      var person = new Person();

      model.ApplyVoteReasonFlags(person, IneligibleReasonEnum.IneligiblePartial1_Not_in_TieBreak, true);
      person.CanVote.ShouldEqual(true);
      person.CanReceiveVotes.ShouldEqual(false);

      model.ApplyVoteReasonFlags(person, IneligibleReasonEnum.IneligiblePartial2_Not_a_Delegate, true);
      person.CanVote.ShouldEqual(false);
      person.CanReceiveVotes.ShouldEqual(true);

      model.ApplyVoteReasonFlags(person, IneligibleReasonEnum.Ineligible_Moved_elsewhere_recently, true);
      person.CanVote.ShouldEqual(false);
      person.CanReceiveVotes.ShouldEqual(false);

      model.ApplyVoteReasonFlags(person, null, true);
      person.CanVote.ShouldEqual(true);
      person.CanReceiveVotes.ShouldEqual(true);

    }

    [TestMethod]
    public void CorrectNumberOfVotes_NotForced_Test()
    {
      var model = new PeopleModel();

      var person = new Person
      {
        IneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Deceased
      };

      // ensure correct flags
      model.ApplyVoteReasonFlags(person);
      person.CanVote.ShouldEqual(false);
      person.CanReceiveVotes.ShouldEqual(false);

      // don't wipe 
      model.ApplyVoteReasonFlags(person, null);
      person.CanVote.ShouldEqual(false);
      person.CanReceiveVotes.ShouldEqual(false);

      // don't change to a different one
      model.ApplyVoteReasonFlags(person, IneligibleReasonEnum.IneligiblePartial2_Not_a_Delegate);
      person.CanVote.ShouldEqual(false);
      person.CanReceiveVotes.ShouldEqual(false);


      // ensure correct flags if person has null
      person.IneligibleReasonGuid = null;
      model.ApplyVoteReasonFlags(person);
      person.CanVote.ShouldEqual(true);
      person.CanReceiveVotes.ShouldEqual(true);

      // ensure correct flags if person has a status
      person.IneligibleReasonGuid = IneligibleReasonEnum.Unidentifiable_Unknown_person;
      model.ApplyVoteReasonFlags(person);
      person.CanVote.ShouldEqual(false);
      person.CanReceiveVotes.ShouldEqual(false);

    }
  }
}