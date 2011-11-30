using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class PeopleSearchModel : DataConnectedModel
  {
    private readonly IQueryable<Person> _people;

    public PeopleSearchModel(IQueryable<Person> people)
    {
      _people = people;
    }

    private IQueryable<Person> People
    {
      get { return _people; }
    }

    public JsonResult Search(string nameToFind)
    {
      const int max = 9;

      var matched = InnerSearch(nameToFind, max, true);

      return new
               {
                 People = matched
                   .Take(max)
                   .Select(p => new
                                  object[]
                                  {
                                    p.C_RowId,
                                    p.C_FullName
                                    //"{0}{1}, {2}{3}{4}".FilledWith(
                                    //  p.LastName,
                                    //  p.OtherLastNames.SurroundContentWith(" [", "]"),
                                    //  p.FirstName,
                                    //  p.OtherNames.SurroundContentWith(" [", "]"),
                                    //  p.OtherInfo.SurroundContentWith(" (", ")")
                                    //)
                                    ,
                                  }),
                 MoreFound = matched.Count > max ? "More than {0} matches".FilledWith(max) : "",
                 DefaultTo = 0 // which of these matches is the most referenced right now? 0 based.
               }
        .AsJsonResult();
    }

    public List<Person> InnerSearch(string search, int max, bool callingSqlDb)
    {
      var parts = search.Split(new[] {' ', '-', '\''}, 2, StringSplitOptions.RemoveEmptyEntries);
      var numParts = parts.Length;

      var term1 = parts[0];
      var metaphone1 = term1.GenerateDoubleMetaphone().DefaultTo("_");

      // IQueryable<Person> query;
      //var whereClause = PredicateBuilder.False<Person>();

      //whereClause = callingSqlDb 
      //  ? whereClause.Or(p => p.CombinedInfo.Contains(SqlFunctions.SoundCode(part0))) 
      //  : whereClause.Or(p => p.CombinedInfo.Contains(SoundEx.ToSoundex(part0)));

      //var nameMatch = PredicateBuilder.True<Person>();
      //nameMatch = nameMatch.And(p => p.CombinedInfo.Contains(part0));

      //if (numParts > 1)
      //{
      //  var part1 = parts[1];
      //  nameMatch = nameMatch.And(p => p.CombinedInfo.Contains(part1));
      //  nameMatch = nameMatch.And(p => SqlFunctions.PatIndex(part0, p.CombinedInfo) != SqlFunctions.PatIndex(part1, p.CombinedInfo));
      //}

      //whereClause = whereClause.Or(nameMatch);

      //return People
      //  .Where(whereClause)
      //  .OrderBy(p => p.LastName)
      //  .ThenBy(p => p.FirstName)
      //  .Take(max + 1)
      //  .ToList();

      string term2 = null;
      string metaphone2 = null;
      if (parts.Length > 1)
      {
        term2 = parts[1];
        metaphone2 = term2.GenerateDoubleMetaphone().DefaultTo("_");

        if (term2 == term1)
        {
          term2 = null;
          metaphone2 = null;
        }
      }

      IQueryable<Person> query;
      if (callingSqlDb)
      {
        query = People.Where(p =>
                             (
                              SqlFunc.HasMatch(p.CombinedInfo, term1, term2) ||
                              SqlFunc.HasMatch(p.CombinedSoundCodes, metaphone1, metaphone2)));
        //if (term2.HasContent())
        //{
        //  query = query.Where(p =>
        //                       (p.CombinedInfo.Contains(term2) ||
        //                        p.CombinedSoundCodes.Contains(metaphone2)));
        //  //query = query.Where(p =>
        //  //                    (SqlFunctions.CharIndex(p.CombinedInfo, term2,
        //  //                                            SqlFunctions.CharIndex(p.CombinedInfo, term1)) != 0 ||
        //  //                     SqlFunctions.CharIndex(p.CombinedSoundCodes, metaphone2,
        //  //                                            SqlFunctions.CharIndex(p.CombinedSoundCodes, metaphone1)) != 0));
        //}
      }
      else
      {
        query = People.Where(p =>
                             (p.CombinedInfo.Contains(term1) ||
                              p.CombinedSoundCodes.Contains(metaphone1)));
        if (term2.HasContent())
        {
          query = query.Where(p =>
                              (p.CombinedInfo.IndexOf(term2, p.CombinedInfo.IndexOf(term1, StringComparison.Ordinal),
                                                      StringComparison.Ordinal) != -1 ||
                               p.CombinedSoundCodes.IndexOf(metaphone2,
                                                            p.CombinedSoundCodes.IndexOf(metaphone1,
                                                                                         StringComparison.Ordinal),
                                                            StringComparison.Ordinal) != -1));
        }
        //query = People.Where(p =>
        //                     p.CombinedSoundCodes.Contains(metaphone1)
        //                     &&
        //                     (metaphone2 == null ||
        //                      p.CombinedSoundCodes.IndexOf(metaphone2, p.CombinedSoundCodes.IndexOf(metaphone1)) != -1)
        //                     ||
        //                     p.CombinedInfo.Contains(term1.ToLower())
        //                     && (term2 == null
        //                         ||
        //                         p.CombinedInfo.IndexOf(term2.ToLower(),
        //                                                p.CombinedInfo.IndexOf(" ",
        //                                                                       p.CombinedInfo.IndexOf(term1.ToLower()))) !=
        //                         -1));
      }

      return query
        .OrderBy(p => p.LastName)
        .ThenBy(p => p.FirstName)
        .Take(max + 1)
        .ToList();

      //if (numParts > 1)
      //{
      //  var part1 = parts[1];
      //  nameMatch = nameMatch.And(p => p.CombinedInfo.Contains(part1));
      //  nameMatch = nameMatch.And(p => SqlFunctions.PatIndex(part0, p.CombinedInfo) != SqlFunctions.PatIndex(part1, p.CombinedInfo));
      //}

      //whereClause = whereClause.Or(nameMatch);
    }
  }
}