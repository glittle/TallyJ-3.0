using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.Models
{
  public class ResultsModel : DataConnectedModel
  {
    private readonly IElectionAnalyzer analyzer;

    public ResultsModel()
    {
      analyzer = UserSession.CurrentElection.IsSingleNameElection.AsBool()
                   ? (IElectionAnalyzer)new SingleNameElectionAnalyzer()
                   : new ElectionAnalyzer();
    }

    public JsonResult CurrentResults
    {
      get
      {
        if (analyzer.TotalInputsNeedingReview != 0)
        {
          // don't show any details if review is needed
          var needReview = analyzer.VoteInfos.Where(ElectionAnalyzerCore.NeedReview)
            .Join(Db.Locations, vi=>vi.LocationId, l=>l.C_RowId, (vi, location) => new { vi, location })
            .Select(x => new
                            {
                              x.vi.LocationId,
                              x.vi.BallotId,
                              Ballot = x.location.Name + " - " + x.vi.C_BallotCode,
                              x.vi.PositionOnBallot
                            }).OrderBy(x => x.Ballot).ThenBy(x => x.PositionOnBallot);

          return new
                   {
                     NeedReview = needReview,
                     analyzer.TotalVotes,
                     analyzer.TotalInvalidVotes,
                     analyzer.TotalInvalidBallots
                   }.AsJsonResult();
        }

        var vResultInfos =
          Db.vResultInfoes.Where(ri => ri.ElectionGuid == UserSession.CurrentElectionGuid).OrderBy(ri => ri.Rank);
        return new
                 {
                   Votes = vResultInfos,
                   analyzer.TotalVotes,
                   analyzer.TotalInvalidVotes,
                   analyzer.TotalInvalidBallots
                 }.AsJsonResult();
      }
    }

    public void GenerateResults()
    {
      analyzer.GenerateResults();
    }
  }
}