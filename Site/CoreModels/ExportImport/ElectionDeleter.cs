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
      if (user == null || !Db.JoinElectionUser.Any(j => j.ElectionGuid == _electionGuid && j.UserId == user.UserId))
      {
        return new
          {
            Deleted = false,
            Error = "Specified election not accessible to you."
          }.AsJsonResult();
      }

      // delete everything...
      // don't rely on stored procedures
      using (var transaction = new TransactionScope())
      {
        
        //var electionGuidName = ReflectionHelper.GetName(() => default(Election).ElectionGuid);

        try
        {
          //DeleteFrom<Vote, Ballot, Location>(electionGuidName,
          //                                   ReflectionHelper.GetName(() => default(Ballot).BallotGuid),
          //                                   ReflectionHelper.GetName(() => default(Location).LocationGuid)
          //  );
          //DeleteFrom<Ballot, Location>(electionGuidName,
          //                             ReflectionHelper.GetName(() => default(Location).LocationGuid));

          //DeleteFrom<Result>(electionGuidName);
          //DeleteFrom<Person>(electionGuidName);
          //DeleteFrom<ResultTie>(electionGuidName);

          //DeleteFrom<Computer>(electionGuidName);
          //DeleteFrom<Location>(electionGuidName);
          //DeleteFrom<Teller>(electionGuidName);
          //DeleteFrom<ResultSummary>(electionGuidName);
          //DeleteFrom<JoinElectionUser>(electionGuidName);
          //DeleteFrom<ImportFile>(electionGuidName);
          //DeleteFrom<Message>(electionGuidName);
          //DeleteFrom<Election>(electionGuidName);

          Election.EraseBallotsAndResults(_electionGuid);

//          Db.Computer.Where(x => x.ElectionGuid == _electionGuid);
          Db.Location.Where(x => x.ElectionGuid == _electionGuid).Delete();
          Db.Person.Where(x => x.ElectionGuid == _electionGuid).Delete();
          Db.Teller.Where(x => x.ElectionGuid == _electionGuid).Delete();
          Db.JoinElectionUser.Where(x => x.ElectionGuid == _electionGuid).Delete();
          Db.ImportFile.Where(x => x.ElectionGuid == _electionGuid).Delete();
          Db.Message.Where(x => x.ElectionGuid == _electionGuid).Delete();
          Db.Election.Where(x => x.ElectionGuid == _electionGuid).Delete();

          new LogHelper().Add("Deleted election '{0}' ({1})".FilledWith(electionName, _electionGuid));

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