using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code;

using TallyJ.CoreModels;
using TallyJ.Code.Helpers;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class PeopleTests
  {
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

      //_model = new PeopleSearchModel(_testGroup.AsQueryable());
    }

    [TestMethod]
    public void Search_TestTerms()
    {
      var model = new PeopleSearchModel();
      model.AllTermsFound(new []{" glen"}, "glen").ShouldEqual(true);
      model.AllTermsFound(new []{" glen"}, "aglen").ShouldEqual(false);
      model.AllTermsFound(new []{" glen"}, "aglen glen").ShouldEqual(true);
      model.AllTermsFound(new []{" glen"}, "aglen glen glen").ShouldEqual(true); // 1 term only matches one

      model.AllTermsFound(new[] { " glen", " glen" }, "glen").ShouldEqual(false);
      model.AllTermsFound(new[] { " glen", " glen" }, "aglen").ShouldEqual(false);
      model.AllTermsFound(new[] { " glen", " glen" }, "aglen glen").ShouldEqual(false);
      model.AllTermsFound(new[] { " glen", " glen" }, "aglen glen glen").ShouldEqual(true); // 2 terms match two

    }

    [TestMethod]
    public void Search_TestPersonSimple()
    {
      var pm = new PeopleModel();
      var p = new Person {FirstName = "Aa", LastName = "Zz"};
      var votes = new List<Vote>();
      pm.SetCombinedInfos(p);

      var psm = new PeopleSearchModel();

      // okay
      var terms = psm.MakeTerms("a");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);

      terms = psm.MakeTerms("aa");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);

      terms = psm.MakeTerms("a z");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);

      terms = psm.MakeTerms("  a   z   "); // extra spaces have no impact
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);

      terms = psm.MakeTerms("z a");  // order doesn't matter
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);



      // miss
      terms = psm.MakeTerms("b");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(0);

    }

    [TestMethod]
    public void Search_TestPerson1()
    {
      var pm = new PeopleModel();
      var votes = new List<Vote>();
      var p = new Person { FirstName = "Glen", LastName = "Little", BahaiId = "23680", OtherLastNames = "Miller" };
      pm.SetCombinedInfos(p);

      var psm = new PeopleSearchModel();

      // okay
      var terms = psm.MakeTerms("gl li");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);

      terms = psm.MakeTerms("glen");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);

      terms = psm.MakeTerms("g m l");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);

      terms = psm.MakeTerms("236");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);

      terms = psm.MakeTerms("mil gle");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(1);



      // miss
      terms = psm.MakeTerms("gln");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(0);


      // sound match
      terms = psm.MakeTerms("glenn");
      psm.DetermineMatch(p, votes, terms, psm.MakeMetas(terms)).ShouldEqual(2);
    }


    //  TODO:
    // add soundex to each word in a field
    // indicate which word in matched? or, indicate if a soundex was used
    // if soundex works, but text does NOT, should not match the person

    //[TestMethod]
    //public void SearchTest_FirstLast_NoMatch()
    //{
    //  var result = _model.InnerSearch("b z", 5, false);
    //  result.Count.ShouldEqual(0);
    //}

    //[TestMethod]
    //public void SearchTest_FirstLast_Match()
    //{
    //  var result = _model.InnerSearch("b y", 5, false);
    //  result.Count.ShouldEqual(1);

    //  result = _model.InnerSearch("b    y", 5, false);
    //  result.Count.ShouldEqual(1);
    //}

    //[TestMethod]
    //public void SearchTest_FirstLast_SameMatch()
    //{
    //  // should only match if two different words both have same letter
    //  var result = _model.InnerSearch("d", 5, false);
    //  result.Count.ShouldEqual(2, "Find D in the names");

    //  result = _model.InnerSearch("d d", 5, false);
    //  result.Count.ShouldEqual(1, "Find two parts of the names with D");
    //}

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
                                }.JoinedAsString(" ").ToLower()
                                .ReplacePunctuation(' ');
        person.CombinedSoundCodes = new[]
                                   {
                                     person.FirstName.GenerateDoubleMetaphone(),
                                     person.LastName.GenerateDoubleMetaphone(),
                                     person.OtherNames.GenerateDoubleMetaphone(),
                                     person.OtherLastNames.GenerateDoubleMetaphone(),
                                     person.OtherInfo.GenerateDoubleMetaphone(),
                                   }.JoinedAsString(" ").ToLower();
        yield return person;
      }
    }
  }
}