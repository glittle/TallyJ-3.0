using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;
using tallyj2dModel.Store;

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

    public JsonResult Search(string nameToFind, bool includeMatches)
    {
      const int max = 19;

      var parts = nameToFind.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

      var term1 = parts[0];
      var metaphone1 = term1.GenerateDoubleMetaphone().DefaultTo("_");

      string term2 = null;
      string metaphone2 = null;
      if (parts.Length > 1)
      {
        term2 = parts[1];
        metaphone2 = term2.GenerateDoubleMetaphone().DefaultTo("_");
      }

      var moreExactMatchesFound = new ObjectParameter("MoreExactMatchesFound", typeof(bool));
      var results = Db.SqlSearch(UserSession.CurrentElectionGuid, term1, metaphone1, term2, metaphone2, max,
                                 moreExactMatchesFound, null);

      var moreFound = moreExactMatchesFound.Value != null && (bool)moreExactMatchesFound.Value;

      return new
      {
        People = results
          .Select(p => new
          {
            Id = p.PersonId,
            Name = p.FullName,
            Inelligible = p.Eligible == 0,
            BestMatch = p.BestMatch == 1,
            SoundMatch = p.SoundMatch == 1
          }),
        MoreFound = moreFound ? "More than {0} exact matches".FilledWith(max) : ""
      }
        .AsJsonResult();
   
    }

    public JsonResult Search_Old(string nameToFind, bool includeMatches)
    {
      const int max = 19;

      var matched = InnerSearch(nameToFind, max, true).Take(max + 1).ToList();
      var toShow = matched.Select(x => x).Take(max).ToList();

      Guid topVote;

      if (includeMatches)
      {
        var guids = toShow.Select(p => p.PersonGuid).ToList();

        topVote = Db
          .vVoteInfoes
          .Where(
            v =>
            v.ElectionGuid == UserSession.CurrentElectionGuid && v.PersonGuid.HasValue &&
            guids.Contains(v.PersonGuid.Value))
          .GroupBy(vi => vi.PersonGuid)
          .Select(g => new {personGuid = g.Key, count = g.Sum(v => v.SingleNameElectionCount)})
          .Where(x => x.count > 0)
          .OrderByDescending(x => x.count)
          .Select(x => x.personGuid.Value)
          .FirstOrDefault();
      }
      else
      {
        topVote = Guid.Empty;
      }

      return new
               {
                 People = toShow
                   .Select(p => new
                                  {
                                    Id = p.C_RowId,
                                    Name = p.C_FullName,
                                    Inelligible = p.IneligibleReasonGuid.HasValue || p.AgeGroup.DefaultTo("A") != "A",
                                    BestMatch = p.PersonGuid == topVote
                                  }),
                 MoreFound = matched.Count > max ? "More than {0} matches".FilledWith(max) : ""
               }
        .AsJsonResult();
    }

    public List<Person> InnerSearch(string search, int max, bool callingSqlDb)
    {
      var parts = search.Split(new[] {' ', '-', '\''}, 2, StringSplitOptions.RemoveEmptyEntries);
      var numParts = parts.Length;

      var term1 = parts[0];
      var metaphone1 = term1.GenerateDoubleMetaphone().DefaultTo("_");

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

      IQueryable<Person> query1;
      IQueryable<Person> query2;
      if (callingSqlDb)
      {
        query1 = People.Where(p => SqlFunc.HasMatch(p.CombinedInfo, term1, term2, false));
        query2 = People.Where(p => SqlFunc.HasMatch(p.CombinedSoundCodes, metaphone1, metaphone2, true));
      }
      else
      {
        query1 = People.Where(p => p.CombinedInfo.Contains(term1));
        query2 = People.Where(p => p.CombinedSoundCodes.Contains(metaphone1));

        if (term2.HasContent())
        {
          query1 = query1.Where(p =>
                                (p.CombinedInfo.IndexOf(term2, p.CombinedInfo.IndexOf(term1, StringComparison.Ordinal),
                                                        StringComparison.Ordinal) != -1 ||
                                 p.CombinedSoundCodes.IndexOf(metaphone2,
                                                              p.CombinedSoundCodes.IndexOf(metaphone1,
                                                                                           StringComparison.Ordinal),
                                                              StringComparison.Ordinal) != -1));
        }
      }

      var list1 = query1
        .OrderBy(p => p.LastName)
        .ThenBy(p => p.FirstName)
        .Take(max + 1)
        .ToList();

      if (list1.Count < max)
      {
        var idsInList1 = list1.Select(x => x.C_RowId).ToList();

        var list2 = query2
          .Where(p => !idsInList1.Contains(p.C_RowId))
          .OrderBy(p => p.LastName)
          .ThenBy(p => p.FirstName)
          .Take(max + 1 - list1.Count)
          .ToList();

        list1.AddRange(list2);
      }

      return list1;
    }
  }
}