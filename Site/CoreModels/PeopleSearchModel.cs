using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using NLog.Targets;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.Models;

namespace TallyJ.CoreModels
{
  public class PeopleSearchModel : DataConnectedModel
  {
    // private readonly IQueryable<Person> _people;

    //private IQueryable<Person> People
    //{
    //  get { return _people; }
    //}

    public JsonResult Search(string nameToFind, bool includeMatches, bool forBallot)
    {
      const int max = 45;

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

      bool moreFound;
      var results = Db.SqlSearch(UserSession.CurrentElectionGuid, term1, term2, metaphone1, metaphone2, max,
        out moreFound);

      var voteHelper = new VoteHelper(forBallot);

      return new
      {
        People = results
          .Select(r => new
          {
            Id = r.PersonId,
            Name = r.FullName,
            Ineligible = voteHelper.IneligibleToReceiveVotes(r.Ineligible, r.CanReceiveVotes),
            r.BestMatch,
            r.MatchType
          }),
        MoreFound = moreFound ? "More than {0} exact matches".FilledWith(max) : ""
      }
        .AsJsonResult();
    }

    public JsonResult Search2(string nameToFind, bool includeMatches, bool forBallot)
    {
      const int max = 45;

      var personList = Person.AllPeopleCached.ToList();

      IEnumerable<SearchResult> results;
      var moreFound = false;

      switch (nameToFind)
      {
        case "~~Voters~~":
          results = personList.Where(p => p.CanVote.AsBoolean()).AsSearchResults();
          break;
        case "~~Tied~~":
          results = personList.Where(p => p.CanReceiveVotes.AsBoolean()).AsSearchResults();
          break;
        default:

          results = GetRankedResults(personList, nameToFind, max, out moreFound);
          break;
      }
      var voteHelper = new VoteHelper(forBallot);

      return new
      {
        People = results
          .Select(r => new
          {
            Id = r.PersonId,
            Name = r.FullName,
            Ineligible = voteHelper.IneligibleToReceiveVotes(r.Ineligible, r.CanReceiveVotes),
            r.BestMatch,
            r.MatchType
          }),
        MoreFound = moreFound ? "More than {0} matches".FilledWith(max) : ""
      }
        .AsJsonResult();
    }

    private IEnumerable<SearchResult> GetRankedResults(List<Person> people, string nameToFind, int max, out bool moreFound)
    {
      moreFound = false;// need to set

      var terms = nameToFind.WithoutDiacritics(true).ReplacePunctuation(' ').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
      var metas = terms.GenerateDoubleMetaphoneArray().ToArray();

      var numTerms = terms.Count();
      var matched = new List<SearchResult>();

      foreach (var person in people)
      {
        var matchType = DetermineMatch(person, terms, metas);
        if (matchType > 0)
        {
          matched.Add(person.AsSerachResult(matchType));
        }

        if (matched.Count > max)
        {
          moreFound = true;
          break;
        }
      }

      if (matched.Count == 0)
      {
        return matched;
      }

      var allVotesCached = Vote.AllVotesCached.ToList();
      foreach (var result in matched)
      {
        result.BestMatch = allVotesCached.Count(v => v.PersonGuid == result.PersonGuid);
      }

      return matched.OrderBy(m => m.MatchType).ThenBy(m => m.FullName);
    }

    private int DetermineMatch(Person person, string[] terms, string[] metas)
    {
      var targets = " " + person.CombinedInfo;
      var numTerms = terms.Length;
      var matches = 0;
      var matched = new List<int>();

      for (var i = 0; i < numTerms; i++)
      {
        var where = targets.IndexOf(" " + terms[i], StringComparison.Ordinal);
        if (!matched.Contains(where))
        {
          matches++;
          matched.Add(where);
        }
      }

      if (matches == numTerms)
      {
        return 1;
      }

      var numMetas = metas.Length;
      matched.Clear();
      matches = 0;
      var sounds = " " + person.CombinedSoundCodes;

      for (var i = 0; i < numMetas; i++)
      {
        var where = sounds.IndexOf(" " + metas[i], StringComparison.Ordinal);
        if (!matched.Contains(where))
        {
          matches++;
          matched.Add(where);
        }
      }

      if (matches == numMetas)
      {
        return 2;
      }

      return 0;
    }


    //public JsonResult Search_Old(string nameToFind, bool includeMatches)
    //{
    //  const int max = 19;

    //  var matched = InnerSearch(nameToFind, max, true).Take(max + 1).ToList();
    //  var toShow = matched.Select(x => x).Take(max).ToList();

    //  Guid topVote;

    //  if (includeMatches)
    //  {
    //    var guids = toShow.Select(p => p.PersonGuid).ToList();

    //    topVote = Db
    //      .vVoteInfoes
    //      .Where(
    //        v =>
    //        v.ElectionGuid == UserSession.CurrentElectionGuid && v.PersonGuid.HasValue &&
    //        guids.Contains(v.PersonGuid.Value))
    //      .GroupBy(vi => vi.PersonGuid)
    //      .Select(g => new {personGuid = g.Key, count = g.Sum(v => v.SingleNameElectionCount)})
    //      .Where(x => x.count > 0)
    //      .OrderByDescending(x => x.count)
    //      .Select(x => x.personGuid.Value)
    //      .FirstOrDefault();
    //  }
    //  else
    //  {
    //    topVote = Guid.Empty;
    //  }

    //  return new
    //           {
    //             People = toShow
    //               .Select(p => new
    //                              {
    //                                Id = p.C_RowId,
    //                                Name = p.C_FullName,
    //                                Ineligible = p.IneligibleReasonGuid,
    //                                BestMatch = p.PersonGuid == topVote
    //                              }),
    //             MoreFound = matched.Count > max ? "More than {0} matches".FilledWith(max) : ""
    //           }
    //    .AsJsonResult();
    //}

    //public List<Person> InnerSearch(string search, int max, bool callingSqlDb)
    //{
    //  var parts = search.Split(new[] {' ', '-', '\''}, 2, StringSplitOptions.RemoveEmptyEntries);
    //  var numParts = parts.Length;

    //  var term1 = parts[0];
    //  var metaphone1 = term1.GenerateDoubleMetaphone().DefaultTo("_");

    //  string term2 = null;
    //  string metaphone2 = null;
    //  if (parts.Length > 1)
    //  {
    //    term2 = parts[1];
    //    metaphone2 = term2.GenerateDoubleMetaphone().DefaultTo("_");

    //    if (term2 == term1)
    //    {
    //      term2 = null;
    //      metaphone2 = null;
    //    }
    //  }

    //  IQueryable<Person> query1;
    //  IQueryable<Person> query2;
    //  if (callingSqlDb)
    //  {
    //    query1 = People.Where(p => SqlFunc.HasMatch(p.CombinedInfo, term1, term2, false));
    //    query2 = People.Where(p => SqlFunc.HasMatch(p.CombinedSoundCodes, metaphone1, metaphone2, true));
    //  }
    //  else
    //  {
    //    query1 = People.Where(p => p.CombinedInfo.Contains(term1));
    //    query2 = People.Where(p => p.CombinedSoundCodes.Contains(metaphone1));

    //    if (term2.HasContent())
    //    {
    //      query1 = query1.Where(p =>
    //                            (p.CombinedInfo.IndexOf(term2, p.CombinedInfo.IndexOf(term1, StringComparison.Ordinal),
    //                                                    StringComparison.Ordinal) != -1 ||
    //                             p.CombinedSoundCodes.IndexOf(metaphone2,
    //                                                          p.CombinedSoundCodes.IndexOf(metaphone1,
    //                                                                                       StringComparison.Ordinal),
    //                                                          StringComparison.Ordinal) != -1));
    //    }
    //  }

    //  var list1 = query1
    //    .OrderBy(p => p.LastName)
    //    .ThenBy(p => p.FirstName)
    //    .Take(max + 1)
    //    .ToList();

    //  if (list1.Count < max)
    //  {
    //    var idsInList1 = list1.Select(x => x.C_RowId).ToList();

    //    var list2 = query2
    //      .Where(p => !idsInList1.Contains(p.C_RowId))
    //      .OrderBy(p => p.LastName)
    //      .ThenBy(p => p.FirstName)
    //      .Take(max + 1 - list1.Count)
    //      .ToList();

    //    list1.AddRange(list2);
    //  }

    //  return list1;
    //}

  }


}