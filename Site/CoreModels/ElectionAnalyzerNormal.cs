using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ElectionAnalyzerNormal : ElectionAnalyzerCore, IElectionAnalyzer
  {

    public ElectionAnalyzerNormal()
    {
    }

    public ElectionAnalyzerNormal(IAnalyzerFakes fakes, Election election,
                                  List<VoteInfo> voteinfos, List<Ballot> ballots,
                                  List<Person> people)
      : base(fakes, election, people, ballots, voteinfos)
    {
      //_locationInfos = locationInfos;
    }

    public ElectionAnalyzerNormal(Election election)
      : base(election)
    {
    }

    //private List<vLocationInfo> _locationInfos;
    ///// <Summary>Current location records</Summary>
    //public List<vLocationInfo> LocationInfos
    //{
    //  get
    //  {
    //    return _locationInfos ?? (_locationInfos = Db.vLocationInfoes
    //                                                 .Where(r => r.ElectionGuid == CurrentElection.ElectionGuid)
    //                                                 .ToList());
    //  }
    //}

    public override ResultSummary AnalyzeEverything()
    {
      if (!IsFaked)
      {
        Ballot.DropCachedBallots();
        Person.DropCachedPeople();
        Location.DropCachedLocations();
      }

      PrepareResultSummaryCalc();

      ResultSummaryCalc.BallotsNeedingReview = Ballots.Count(BallotAnalyzer.BallotNeedsReview);

      ResultSummaryCalc.BallotsReceived = Ballots.Count;
      ResultSummaryCalc.TotalVotes = ResultSummaryCalc.BallotsReceived * TargetElection.NumberToElect;

      var invalidBallotGuids = Ballots.Where(b => b.StatusCode != "Ok").Select(b => b.BallotGuid).ToList();

      ResultSummaryCalc.SpoiledBallots = invalidBallotGuids.Count();
      ResultSummaryCalc.SpoiledVotes =
        VoteInfos.Count(vi => !invalidBallotGuids.Contains(vi.BallotGuid) && VoteAnalyzer.IsNotValid(vi));

      var electionGuid = TargetElection.ElectionGuid;

      // collect only valid votes
      foreach (var ballot in Ballots.Where(bi => bi.StatusCode == BallotStatusEnum.Ok))
      {
        var ballotGuid = ballot.BallotGuid;
        foreach (var VoteInfo in VoteInfos.Where(vi => vi.BallotGuid == ballotGuid && VoteAnalyzer.VoteIsValid(vi)))
        {
          var voteInfo = VoteInfo;

          // get existing result record for this person, if available
          var result =
            Results.SingleOrDefault(r => r.ElectionGuid == electionGuid && r.PersonGuid == voteInfo.PersonGuid);
          if (result == null)
          {
            result = new Result
                       {
                         ElectionGuid = electionGuid,
                         PersonGuid = voteInfo.PersonGuid.AsGuid()
                       };
            ResetValues(result);
            Results.Add(result);
            AddResult(result);
          }

          var voteCount = result.VoteCount.AsInt() + 1;
          result.VoteCount = voteCount;
        }
      }

      DoAnalysisForTies();
      FinalizeSummaries();


      return ResultSummaryFinal;
    }
  }
}