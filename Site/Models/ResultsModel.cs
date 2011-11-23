using System.Collections.Generic;
using TallyJ.Code;
using TallyJ.Code.Session;
using System.Linq;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ResultsModel : DataConnectedModel
  {
    IElectionAnalyzer analyzer;

    public ResultsModel()
    {
      analyzer = UserSession.CurrentElection.IsSingleNameElection.AsBool()
             ? (IElectionAnalyzer)new SingleNameElectionAnalyzer()
             : new ElectionAnalyzer();
    }

    public void GenerateResults()
    {
      analyzer.GenerateResults();
    }

    public IQueryable<vResultInfo> CurrentResults
    {
      get
      {
        var vResultInfos = Db.vResultInfoes.Where(ri => ri.ElectionGuid == UserSession.CurrentElectionGuid).OrderBy(ri => ri.Rank);
        // allow debugging
        return vResultInfos;
      }
    }
  }
}