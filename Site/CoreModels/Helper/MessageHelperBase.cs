using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.CoreModels.Helper
{
  public class MessageHelperBase
  {
    private LogHelper _logHelper;

    protected LogHelper LogHelper => _logHelper ?? (_logHelper = new LogHelper());

    protected enum HtEmailCodes
    {
      Test,
      Intro,
      _unknown_
    }


    public JsonResult GetContacts()
    {
      var dbContext = UserSession.GetNewDbContext;
      var electionGuid = UserSession.CurrentElectionGuid;

      return new
      {
        Success = true,
        people = dbContext.Person
          .Where(p => p.ElectionGuid == electionGuid && p.CanVote.Value && (p.Email != null && p.Email.Trim().Length > 0 || p.Phone != null && p.Phone.Trim().Length > 0))
          .OrderBy(p => p.C_FullName)
          .Select(p => new
          {
            p.C_RowId,
            p.C_FullName,
            p.VotingMethod,
            p.Email,
            p.Phone
          }),
      }.AsJsonResult();
    }

    public JsonResult GetContactLog(int lastLogId = 0)
    {
      var dbContext = UserSession.GetNewDbContext;
      var electionGuid = UserSession.CurrentElectionGuid;

      return new
      {
        Success = true,
        Log = dbContext.C_Log
          .Where(l => l.ElectionGuid == electionGuid)
          .Where(l => l.Details.StartsWith("Email:") || l.Details.StartsWith("Sms:"))
          .Where(l => lastLogId == 0 || l.C_RowId < lastLogId)
          .OrderByDescending(l => l.AsOf)
          .Take(10)
          .Select(l => new
          {
            l.AsOf,
            l.Details,
            l.C_RowId
          })
      }.AsJsonResult();
    }

  }
}