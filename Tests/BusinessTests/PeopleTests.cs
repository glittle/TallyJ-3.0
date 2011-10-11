using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code;
using TallyJ.EF;
using TallyJ.Models;

namespace Tests.BusinessTests
{
  [TestClass]
  public class PeopleTests
  {
    PeopleSearchModel _model;
    IEnumerable<Person> _testGroup;

    [TestInitialize]
    public void Setup()
    {
      _testGroup = FillCombined(new List<Person>
                                  {
                                    new Person {FirstName = "Aa", LastName = "Zz"},
                                    new Person {FirstName = "Bb", LastName = "Yy"},
                                    new Person {FirstName = "Bb", LastName = "123"},
                                    new Person {FirstName = "Cc", LastName = "Xx"},
                                    new Person {FirstName = "Dd", LastName = "123"},
                                    new Person {FirstName = "Dd", LastName = "De"},
                                  });

      _model = new PeopleSearchModel(_testGroup.AsQueryable());
    }

    [TestMethod]
    public void SearchTest_FirstName()
    {
      var result = _model.InnerSearch("b", 5, false);
      result.Count.ShouldEqual(2);
      result[0].FirstName.ShouldEqual("Bb");
    }

    [TestMethod]
    public void SearchTest_FirstLast_NoMatch()
    {
      var result = _model.InnerSearch("b z", 5, false);
      result.Count.ShouldEqual(0);
    }

    [TestMethod]
    public void SearchTest_FirstLast_Match()
    {
      var result = _model.InnerSearch("b y", 5, false);
      result.Count.ShouldEqual(1);

      result = _model.InnerSearch("b    y", 5, false);
      result.Count.ShouldEqual(1);
    }

    [TestMethod]
    public void SearchTest_FirstLast_SameMatch()
    {
      // should only match if two different words both have same letter
      var result = _model.InnerSearch("d", 5, false);
      result.Count.ShouldEqual(2, "Find D in the names");

      result = _model.InnerSearch("d d", 5, false);
      result.Count.ShouldEqual(1, "Find two parts of the names with D");
    }

    static IEnumerable<Person> FillCombined(IEnumerable<Person> persons)
    {
      var i = 1;
      foreach (var person in persons)
      {
        person.C_RowId = i++;
        person.CombinedInfo = new[]
                                {
                                  person.FirstName,
                                  person.LastName,
                                  person.OtherNames,
                                  person.OtherLastNames,
                                  person.OtherInfo,
                                }.JoinedAsString(" ").ToLower();
        person.CombinedSoundEx = new[]
                                   {
                                     person.FirstName.ToSoundex(),
                                     person.LastName.ToSoundex(),
                                     person.OtherNames.ToSoundex(),
                                     person.OtherLastNames.ToSoundex(),
                                     person.OtherInfo.ToSoundex(),
                                   }.JoinedAsString(" ").ToLower();
        yield return person;
      }
    }
  }
}