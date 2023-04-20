using System;
using System.Linq;
using System.Transactions;
using System.Web.Mvc;
using EntityFramework.Extensions;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.ExportImport
{
  public class ElectionDeleter : DataConnectedModel
  {
    private readonly Guid _electionGuid;

    public ElectionDeleter(Guid electionGuid)
    {
      _electionGuid = electionGuid;
    }

    public ActionResult Delete()
    {
      var target = Db.Election.SingleOrDefault(e => e.ElectionGuid == _electionGuid);
      if (target == null)
      {
        return new
        {
          Deleted = false,
          Error = "Cannot find specified election."
        }.AsJsonResult();
      }
      var electionName = target.Name;

      var user = Db.Users.SingleOrDefault(u => u.UserName == UserSession.LoginId);
      if (user == null || !Db.JoinElectionUser.Any(j => j.ElectionGuid == _electionGuid && j.UserId == user.UserId && j.Role == null))
      {
        // Role == null indicates main owner of the election
        return new
        {
          Deleted = false,
          Error = "Specified election not accessible to you."
        }.AsJsonResult();
      }

      // delete everything...
      // don't rely on stored procedures
      using var transaction = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromMinutes(10));

      try
      {
        Db.Result.Where(r => r.ElectionGuid == _electionGuid).Delete();
        Db.ResultTie.Where(r => r.ElectionGuid == _electionGuid).Delete();
        Db.ResultSummary.Where(r => r.ElectionGuid == _electionGuid).Delete();

        var locationGuids = Db.Location.Where(x => x.ElectionGuid == _electionGuid).Select(l => l.LocationGuid).ToList();

        // delete ballots in all locations... cascading will delete votes (NOT ANYMORE... must do manually)
        var ballotsQuery = Db.Ballot.Where(b => locationGuids.Contains(b.LocationGuid));

        Db.Vote.Where(v => ballotsQuery.Select(b => b.BallotGuid).Contains(v.BallotGuid)).Delete();

        // also delete orphan votes by person
        Db.Vote
          .Where(v => v.PersonGuid != null && Db.Person
            .Where(p => p.ElectionGuid == _electionGuid)
            .Select(p => p.PersonGuid)
            .Contains(v.PersonGuid.Value)
          ).Delete();

        ballotsQuery.Delete();

        Db.OnlineVotingInfo.Where(x => x.ElectionGuid == _electionGuid).Delete();
        Db.Location.Where(x => x.ElectionGuid == _electionGuid).Delete();
        Db.Person.Where(x => x.ElectionGuid == _electionGuid).Delete();
        Db.Teller.Where(x => x.ElectionGuid == _electionGuid).Delete();
        Db.JoinElectionUser.Where(x => x.ElectionGuid == _electionGuid).Delete();
        Db.ImportFile.Where(x => x.ElectionGuid == _electionGuid).Delete();
        Db.Message.Where(x => x.ElectionGuid == _electionGuid).Delete();
        Db.Election.Where(x => x.ElectionGuid == _electionGuid).Delete();

        new LogHelper(_electionGuid).Add("Deleted election '{0}'".FilledWith(electionName));

        transaction.Complete();

        if (_electionGuid == UserSession.CurrentElectionGuid)
        {
          new CacherHelper().DropAllCachesForThisElection();
        }

        return new
        {
          Deleted = true
        }.AsJsonResult();

      }
      catch (Exception ex)
      {
        return new
        {
          Deleted = false,
          Message = ex.GetAllMsgs("<br>")
        }.AsJsonResult();

      }
    }

    //private void DeleteFrom<T>(string electionGuidName)
    //{
    //  var target = typeof(T).Name;
    //  var sql = "delete from tj.{0} where {1}=@{1}".FilledWith(
    //    target,
    //    electionGuidName);
    //  Db.Database.ExecuteSqlCommand(sql, new SqlParameter("@" + electionGuidName, _electionGuid));
    //}

    //private void DeleteFrom<T, T1>(string electionGuidName, string join1Name)
    //{
    //  var target = typeof(T).Name;
    //  var join = typeof(T1).Name;
    //  var sql = "delete from {0} from tj.{0} join tj.{1} on {1}.{2}={0}.{2} where {1}.{3}=@{3}".FilledWith(
    //    target,
    //    join,
    //    join1Name,
    //    electionGuidName
    //    );
    //  Db.Database.ExecuteSqlCommand(sql, new SqlParameter("@" + electionGuidName, _electionGuid));
    //}

    //private void DeleteFrom<T, T1, T2>(string electionGuidName, string join1Name, string join2Name)
    //{
    //  var target = typeof(T).Name;
    //  var join1 = typeof(T1).Name;
    //  var join2 = typeof(T2).Name;
    //  var sql = ("delete from {0} from tj.{0} "
    //            + "join tj.{1} on {1}.{2}={0}.{2} "
    //            + "join tj.{3} on {3}.{4}={1}.{4} where {3}.{5}=@{5}").FilledWith(
    //              target,
    //              join1,
    //              join1Name,
    //              join2,
    //              join2Name,
    //              electionGuidName
    //                );
    //  Db.Database.ExecuteSqlCommand(sql, new SqlParameter("@" + electionGuidName, _electionGuid));
    //}
  }
}