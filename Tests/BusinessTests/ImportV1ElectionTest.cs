using System;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;
using TallyJ.CoreModels;
using TallyJ.EF;
using Tests.Properties;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class ImportV1ElectionTest
  {
    [TestMethod]
    public void Basic_Import_Test()
    {
      var fakeImportFile = new ImportFile();
      var fakes = new ImportFakes();

      var xmlDoc = new XmlDocument();
      var fakeDataContext = new TestDbContext();

      xmlDoc.LoadXml(Resources.SampleElection);

      var electionGuid = Guid.NewGuid();
      var election = new Election {ElectionGuid = electionGuid};
      var location = new Location {LocationGuid = Guid.NewGuid()};

      var model = new ImportV1Election(fakeDataContext, fakeImportFile, xmlDoc, election, location,
                                       fakes.AddBallotToDb, fakes.AddVoteToDb,
                                       fakes.People, fakes.AddPersonToDb, fakes.AddResultSummaryToDb, fakes.LogHelper);

      model.Process();

      election.Name.ShouldEqual("(Imported) Sample LSA Election");
      election.DateOfElection.ShouldEqual(new DateTime(2011, 4, 20));
      election.ElectionType.ShouldEqual(ElectionTypeEnum.Lsa);
      election.ElectionMode.ShouldEqual(ElectionModeEnum.Normal);
      election.IsSingleNameElection.ShouldEqual(false);
      election.NumberExtra.ShouldEqual(0);
      election.NumberToElect.ShouldEqual(9);
      election.ShowAsTest.ShouldEqual(true, "Imported elections are marked as Test");
      election.TallyStatus.ShouldEqual(ElectionTallyStatusEnum.Reviewing, "Imported elections set to Review mode");

      fakes.ResultSummaries.Count.ShouldEqual(1);
      var resultSummary = fakes.ResultSummaries[0];
      resultSummary.DroppedOffBallots.ShouldEqual(1);
      resultSummary.MailedInBallots.ShouldEqual(10);
      resultSummary.CalledInBallots.ShouldEqual(0);
      resultSummary.InPersonBallots.ShouldEqual(17);
      resultSummary.NumEligibleToVote.ShouldEqual(51);

      fakes.Ballots.Count.ShouldEqual(28);
      var ballot1 = fakes.Ballots[0];
      ballot1.StatusCode.ShouldEqual(BallotStatusEnum.Ok);

      var votes1 = fakes.Votes.Where(v => v.BallotGuid == ballot1.BallotGuid).ToList();
      votes1.Count.ShouldEqual(9);

      var vote1 = votes1[0];
      vote1.StatusCode.ShouldEqual(VoteHelper.VoteStatusCode.Ok);

      var matchingPerson = fakes.People.Where(p => p.PersonGuid == vote1.PersonGuid).ToList();
      matchingPerson.Count.ShouldEqual(1);
      vote1.PersonCombinedInfo.ShouldEqual(matchingPerson[0].CombinedInfo);


      var ballot4 = fakes.Ballots[3];
      ballot4.ComputerCode.ShouldEqual("A");
      ballot4.BallotNumAtComputer.ShouldEqual(4);

      var votes4 = fakes.Votes.Where(v => v.BallotGuid == ballot4.BallotGuid).ToList();
      votes4.Count.ShouldEqual(9);

      var vote4_9 = votes4[8];
      vote4_9.StatusCode.ShouldEqual(VoteHelper.VoteStatusCode.Ok);
      vote4_9.InvalidReasonGuid.ShouldEqual(IneligibleReasonEnum.Unreadable_Writing_illegible);


      var ballot11 = fakes.Ballots[10];
      ballot11.ComputerCode.ShouldEqual("A");
      ballot11.BallotNumAtComputer.ShouldEqual(11);
      ballot11.StatusCode.ShouldEqual(BallotStatusEnum.TooMany);
      var votes11 = fakes.Votes.Where(v => v.BallotGuid == ballot11.BallotGuid).ToList();
      votes11.Count.ShouldEqual(10);
    }
  }
}