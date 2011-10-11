using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class PeopleSearchModel
  {
    readonly IQueryable<Person> _people;

    public PeopleSearchModel(IQueryable<Person> people)
    {
      _people = people;
    }

    IQueryable<Person> People
    {
      get { return _people; }
    }

    public JsonResult Search(string nameToFind)
    {
      const int max = 5;

      var matched = InnerSearch(nameToFind, max, true);

      return new
               {
                 People = matched
                   .Take(max)
                   .Select(p => new
                                  object[]
                                  {
                                    p.C_RowId,
                                    "{0}, {1}".FilledWith(p.LastName, p.FirstName),
                                    "{0}".FilledWith(p.OtherInfo),
                                    new[] {p.OtherNames, p.OtherLastNames}.JoinedAsString(" ").Trim(),
                                  }),
                 MoreFound = matched.Count > max ? "More than {0} matches".FilledWith(max) : "",
                 DefaultTo = 2 // which of these matches is the most referenced right now? 0 based.
               }
        .AsJsonResult();
    }

    public List<Person> InnerSearch(string search, int max, bool callingSqlDb)
    {
      var parts = search.Split(new[] {' ', '-', '\''}, 2, StringSplitOptions.RemoveEmptyEntries);
      var numParts = parts.Length;

      var part0 = parts[0];
      var part0Sx = part0.ToSoundex();

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

      string part1 = null;
      string part1Sx = null;
      if (parts.Length > 1)
      {
        part1 = parts[1];
        part1Sx = part1.ToSoundex();
      }

      IQueryable<Person> query;
      if (callingSqlDb)
      {
        query = People.Where(p => p.CombinedSoundEx.Contains(SqlFunctions.SoundCode(part0))
                                  || p.CombinedInfo.Contains(part0)
                                  && (part1 == null
                                      ||
                                      SqlFunctions.CharIndex(part1, p.CombinedInfo,
                                                             SqlFunctions.CharIndex(" ", p.CombinedInfo,
                                                                                    SqlFunctions.CharIndex(part0,
                                                                                                           p.
                                                                                                             CombinedInfo))) !=
                                      -1));
      }
      else
      {
        query = People.Where(p =>
                             //p.CombinedSoundEx.Contains(part0Sx)
                             //                        &&
                             //                        (part1Sx == null ||
                             //                         p.CombinedSoundEx.IndexOf(part1Sx, 1 + p.CombinedSoundEx.IndexOf(part0Sx)) != -1)
                             //                        || 
                             p.CombinedInfo.Contains(part0.ToLower())
                             && (part1 == null
                                 ||
                                 p.CombinedInfo.IndexOf(part1.ToLower(),
                                                        p.CombinedInfo.IndexOf(" ",
                                                                               p.CombinedInfo.IndexOf(part0.ToLower()))) !=
                                 -1));
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