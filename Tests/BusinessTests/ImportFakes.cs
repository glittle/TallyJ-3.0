using System.Collections.Generic;
using TallyJ.Code;
using TallyJ.EF;


namespace Tests.BusinessTests
{
  public class ImportFakes
  {
    public ImportFakes()
    {
      People = new List<Person>();
      ResultSummaries=new List<ResultSummary>();
      Ballots = new List<Ballot>();
      Votes=new List<Vote>();
      LogMessage = "";
      LogHelper = new FakeLogHelper(LogMessage);
    }

    private class FakeLogHelper : ILogHelper
    {
      private string _logMessage;

      public FakeLogHelper(string logMessage)
      {
        _logMessage = logMessage;
      }

      public void Add(string message, bool x = false)
      {
        _logMessage = _logMessage + message;
      }
    }

    public string LogMessage { set; get; }

    public ILogHelper LogHelper { set; get; }

    public List<Person> People { set; get; }

    public List<ResultSummary> ResultSummaries { set; get; }

    public List<Ballot> Ballots { set; get; }

    public List<Vote> Votes { set; get; }

    public void AddPersonToDb(Person item)
    {
      // don't add here... code adds to list
    }
    public void AddResultSummaryToDb(ResultSummary item)
    {
      ResultSummaries.Add(item);
    }
    public void AddBallotToDb(Ballot item)
    {
      Ballots.Add(item);
    }
    public void AddVoteToDb(Vote item)
    {
      Votes.Add(item);
    }
  }
}