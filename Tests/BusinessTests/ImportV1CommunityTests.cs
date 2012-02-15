using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.EF;
using TallyJ.Models;
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
      var people = new List<Person>();
      var xmlDoc = new XmlDocument();
      var fakeDataContext = new FakeDataContext();

      xmlDoc.LoadXml("<?xml version='1.0' encoding='UTF-16'?><Community><Person LName='Accorti' FName='Pónt' AKAName='Paul'></Person></Community>");
      var model = new ImportV1Community(fakeDataContext, fakeImportFile, xmlDoc, people, Fakes.ResetPerson);

      model.Process();

      people.Count.ShouldEqual(1);
      people[0].LastName.ShouldEqual("Accorti");
      people[0].FirstName.ShouldEqual("Pónt");
      people[0].OtherNames.ShouldEqual("Paul");
    }

    [TestMethod]
    public void SkipDuplicates()
    {
      var fakeImportFile = new ImportFile();
      var people = new List<Person>();
      var xmlDoc = new XmlDocument();
      var fakeDataContext = new FakeDataContext();

      xmlDoc.LoadXml("<Community><Person LName='Accorti' FName='Pónt' AKAName='Paul'></Person><Person LName='Accorti' FName='Pónt' AKAName='Paul'></Person></Community>");
      var model = new ImportV1Community(fakeDataContext, fakeImportFile, xmlDoc, people, Fakes.ResetPerson);

      model.Process();

      people.Count.ShouldEqual(1);
      people[0].LastName.ShouldEqual("Accorti");
      people[0].FirstName.ShouldEqual("Pónt");
      people[0].OtherNames.ShouldEqual("Paul");
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

  public static class Fakes
  {
    public static void ResetPerson(Person toReset)
    {
      // pretent to remove
    }
  }
}