using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.EF;
using TallyJ.CoreModels;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class ImportV1CommunityTests
  {
    [TestMethod]
    public void ImportBasic()
    {
      var fakeImportFile = new ImportFile();
      var fakes = new ImportFakes();
      var xmlDoc = new XmlDocument();
      var fakeDataContext = new FakeDataContext();

      xmlDoc.LoadXml("<?xml version='1.0' encoding='UTF-16'?><Community><Person LName='Accorti' FName='Pónt' AKAName='Paul'></Person></Community>");
      var model = new ImportV1Community(fakeDataContext, fakeImportFile, xmlDoc, fakes.People, fakes.AddPersonToDb, fakes.LogHelper);

      model.Process();

      fakes.People.Count.ShouldEqual(1);
      var people = fakes.People[0];
      people.LastName.ShouldEqual("Accorti");
      people.FirstName.ShouldEqual("Pónt");
      people.OtherNames.ShouldEqual("Paul");
    }

    [TestMethod]
    public void SkipDuplicates()
    {
      var fakeImportFile = new ImportFile();
      var fakes = new ImportFakes();
      var xmlDoc = new XmlDocument();
      var fakeDataContext = new FakeDataContext();

      xmlDoc.LoadXml("<Community><Person LName='Accorti' FName='Pónt' AKAName='Paul'></Person><Person LName='Accorti' FName='Pónt' AKAName='Paul'></Person></Community>");
      var model = new ImportV1Community(fakeDataContext, fakeImportFile, xmlDoc, fakes.People, fakes.AddPersonToDb, fakes.LogHelper);

      model.Process();

      fakes.People.Count.ShouldEqual(1);
      var people = fakes.People[0];
      people.LastName.ShouldEqual("Accorti");
      people.FirstName.ShouldEqual("Pónt");
      people.OtherNames.ShouldEqual("Paul");
    }
  }

  public class FakeDataContext : IDbContext
  {
    public int SaveChanges()
    {
      // okay
      return 0;
    }
  }
}