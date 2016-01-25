using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TallyJ.CoreModels;
using TallyJ.EF;
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
      var fakeDataContext = new TestDbContext();

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
      var fakeDataContext = new TestDbContext();

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
}