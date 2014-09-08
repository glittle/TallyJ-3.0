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
          results = personList.Where(p => p.CanReceiveVotes.AsBoolean(true)).OrderBy(p => p.FullName).AsSearchResults(0, voteHelper, forBallot).ToList();
          break;
        default:
          results = GetRankedResults(personList, voteList, nameToFind, max, voteHelper, forBallot, out moreFound);
          break;
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

      if (terms.Length == 0 || (terms.Length == 1 && terms[0].HasNoContent()))
      {
        return matched;
      }

      var metas = MakeMetas(terms);

      foreach (var person in people)
      {
        if (!System.Web.HttpContext.Current.Response.IsClientConnected)
        {
          return new List<SearchResult>();
        }

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
      // include ^ for backward compatibility
      var adjusted = " " + targets.Replace('^', ' ') + (soundMatching ? " " : "");

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
  }
}