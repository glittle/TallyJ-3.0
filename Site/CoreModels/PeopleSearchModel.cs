using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Providers.Entities;
using System.Web.UI.WebControls.WebParts;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class PeopleSearchModel : DataConnectedModel
  {
    private bool _savePeopleNeeded;
    // private readonly IQueryable<Person> _people;

    //private IQueryable<Person> People
    //{
    //  get { return _people; }
    //}
    //
    //    public JsonResult Search(string nameToFind, bool includeMatches, bool forBallot)
    //    {
    //      const int max = 45;
    //
    //      var parts = nameToFind.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
    //
    //      var term1 = parts[0];
    //      var metaphone1 = term1.GenerateDoubleMetaphone().DefaultTo("_");
    //
    //      string term2 = null;
    //      string metaphone2 = null;
    //      if (parts.Length > 1)
    //      {
    //        term2 = parts[1];
    //        metaphone2 = term2.GenerateDoubleMetaphone().DefaultTo("_");
    //      }
    //
    //      bool moreFound;
    //      var results = Db.SqlSearch(UserSession.CurrentElectionGuid, term1, term2, metaphone1, metaphone2, max,
    //        out moreFound);
    //
    //      var voteHelper = new VoteHelper(forBallot);
    //
    //      return new
    //      {
    //        People = results
    //          .Select(r => new
    //          {
    //            Id = r.PersonId,
    //            Name = r.FullName,
    //            Ineligible = voteHelper.IneligibleToReceiveVotes(r.Ineligible, r.CanReceiveVotes),
    //            r.BestMatch,
    //            r.MatchType
    //          }),
    //        MoreFound = moreFound ? "More than {0} exact matches".FilledWith(max) : ""
    //      }
    //        .AsJsonResult();
    //    }

    public JsonResult Search2(string nameToFind, bool includeMatches, bool forBallot)
    {
      const int max = 30;

      var personList = new PersonCacher().AllForThisElection;
      var voteList = new VoteCacher().AllForThisElection;
      var voteHelper = new VoteHelper(forBallot);

      List<SearchResult> results;
      var moreFound = false;

      switch (nameToFind)
      {
        case "~~All~~":
          results = personList.OrderBy(p => p.FullName).AsSearchResults(0, voteHelper, forBallot).ToList();
          break;
        case "~~Voters~~":
          results = personList.Where(p => p.CanVote.AsBoolean(true)).OrderBy(p => p.FullName).AsSearchResults(0, voteHelper, forBallot).ToList();
          break;
        case "~~Tied~~":
          results = personList.Where(p => p.CanReceiveVotes.AsBoolean(true)).OrderBy(p=>p.FullName).AsSearchResults(0, voteHelper, forBallot).ToList();
          break;
        default:
          results = GetRankedResults(personList, voteList, nameToFind, max, voteHelper, forBallot, out moreFound);
          break;
      }

      if (_savePeopleNeeded)
      {
        Db.SaveChanges();
        new PersonCacher().ReplaceEntireCache(personList);
        new VoteCacher().ReplaceEntireCache(voteList);
      }

      return new
      {
        People = results
          .Select(r => new
          {
            r.Id,
            r.Name,
            r.CanReceiveVotes,
            r.CanVote,
            r.Ineligible,
            r.BestMatch,
            r.MatchType,
            r.Extra
          }),
        MoreFound = moreFound ? "More than {0} matches".FilledWith(max) : "",
        LastRowVersion = results.Count == 0 ? 0 : results.Max(p => p.RowVersion)
      }
        .AsJsonResult();
    }

    private List<SearchResult> GetRankedResults(IEnumerable<Person> people, List<Vote> voteList, string nameToFind, int max, VoteHelper voteHelper, bool forBallot, out bool moreFound)
    {
      moreFound = false; // need to set

      var matched = new List<SearchResult>();

      var terms = MakeTerms(nameToFind);

      if (terms.Length == 0 || (terms.Length==1 && terms[0].HasNoContent()))
      {
        return matched;
      }
      
      
      var metas = MakeMetas(terms);


      foreach (var person in people)
      {
//        if (forBallot && person.CanReceiveVotes.HasValue && !person.CanReceiveVotes.Value)
//        {
//          // ignore if can't receive votes
//          continue;
//        }
        var matchType = DetermineMatch(person, voteList, terms, metas);
        if (matchType > 0)
        {
          matched.Add(person.AsSearchResult(matchType, voteHelper, forBallot));
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

      var isSingleNameElection = UserSession.CurrentElection.IsSingleNameElection;

      foreach (var result in matched)
      {
        result.BestMatch = isSingleNameElection
          ? voteList.Where(v => v.PersonGuid == result.PersonGuid).Sum(v => v.SingleNameElectionCount).AsInt()
          : voteList.Count(v => v.PersonGuid == result.PersonGuid);
      }

      return matched.OrderBy(m => m.MatchType).ThenBy(m => m.Name).ToList();
    }

    /// <summary>
    ///   Test this person for the terms and metas.
    /// </summary>
    /// <param name="person"></param>
    /// <param name="voteList"></param>
    /// <param name="terms">Each term in the list must start with a space</param>
    /// <param name="metas">Each meta in the list must start with a space</param>
    /// <returns></returns>
    public int DetermineMatch(Person person, List<Vote> voteList, string[] terms, string[] metas)
    {
      AssertAtRuntime.That(terms[0][0] == ' ', "invalid term");
      if (person.CombinedInfo.HasNoContent()
          || person.CombinedSoundCodes.HasNoContent()
          || person.CombinedInfo.Contains("^")
          || person.CombinedSoundCodes.Contains("^"))
      {
        AutoFix(person, voteList);
      }

      if (AllTermsFound(terms, person.CombinedInfo))
      {
        return 1;
      }

      if (AllTermsFound(metas, person.CombinedSoundCodes, true))
      {
        return 2;
      }

      return 0;
    }

    public bool AllTermsFound(string[] terms, string targets, bool soundMatching = false)
    {
      var matches = 0;
      Dictionary<string, int> matchedPositions = null;
      var adjusted = " " + targets + (soundMatching ? " " : "");

      foreach (var t in terms)
      {
        var start = 0;
        if (matchedPositions != null && matchedPositions.ContainsKey(t))
        {
          start = matchedPositions[t] + 1;
        }

        var where = adjusted.IndexOf(t, start,
          soundMatching ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        if (where == -1) continue;

        // found a match
        matches++;

        if (matchedPositions == null)
        {
          matchedPositions = new Dictionary<string, int>();
        }

        matchedPositions[t] = where;
      }
      return matches == terms.Length;
    }

    public void AutoFix(Person person, List<Vote> voteList)
    {
      var oldCombined = person.CombinedInfo;
      var oldSounds = person.CombinedSoundCodes;

      Db.Person.Attach(person);
      new PeopleModel().SetCombinedInfos(person);

      if (person.CombinedInfo == oldCombined && person.CombinedSoundCodes == oldSounds)
      {
        return;
      }

      _savePeopleNeeded = true;
      
      foreach (var vote in voteList.Where(v=>v.PersonGuid==person.PersonGuid))
      {
        if (vote.PersonCombinedInfo == oldCombined)
        {
          Db.Vote.Attach(vote);
          vote.PersonCombinedInfo = person.CombinedInfo;
        }
      }
    }

    public string[] MakeTerms(string nameToFind)
    {
      return
        nameToFind.WithoutDiacritics(true)
          .ReplacePunctuation(' ')
          .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
          .Select(s => " " + s)
          .ToArray();
    }

    public string[] MakeMetas(string[] terms)
    {
      return terms.GenerateDoubleMetaphoneArray().Select(s => " " + s + " ").ToArray();
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