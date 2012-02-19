using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;
using TallyJ.EF;
using TallyJ.Models;
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
      var fakeDataContext = new FakeDataContext();

      xmlDoc.Load(@"..\..\Support\SampleElection.xml");

      var electionGuid = Guid.NewGuid();
      var election = new Election { ElectionGuid = electionGuid };
      var location = new Location {LocationGuid = Guid.NewGuid() };
      var ballots = new List<vBallotInfo>();
      var votes = new List<vVoteInfo>();

      var model = new ImportV1Election(fakeDataContext, fakeImportFile, xmlDoc, election, location,
        fakes.AddBallotToDb, fakes.AddVoteToDb, 
        fakes.People, fakes.AddPersonToDb, fakes.AddResultSummaryToDb, fakes.LogHelper);

      model.Process();

      election.Name.ShouldEqual("(Imported) Sample LSA Election");
      election.DateOfElection.ShouldEqual(new DateTime(2011, 4, 20));
      election.ElectionType.ShouldEqual(ElectionTypeEnum.Lsa);
      election.ElectionMode.ShouldEqual(ElectionModeEnum.Normal);
      election.IsSingleNameElection.ShouldEqual(null);
      election.NumberExtra.ShouldEqual(0);
      election.NumberToElect.ShouldEqual(9);
      election.ShowAsTest.ShouldEqual(true, "Imported elections are marked as Test");
      election.TallyStatus.ShouldEqual(ElectionTallyStatusEnum.Reviewing, "Imported elections set to Review mode");

      fakes.ResultSummaries.Count.ShouldEqual(1);
      var resultSummary = fakes.ResultSummaries[0];
      resultSummary.DroppedOffBallots.ShouldEqual(1);
      resultSummary.MailedInBallots.ShouldEqual(10);
      resultSummary.InPersonBallots.ShouldEqual(17);
      resultSummary.NumEligibleToVote.ShouldEqual(51);

      fakes.Ballots.Count.ShouldEqual(28);

    }

  }
}