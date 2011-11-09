using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ElectionsListViewModel : BaseViewModel
  {
    public IEnumerable<object> MyElectionsInfo
    {
      get
      {
        return
          MyElections()
            .OrderByDescending(e => e.DateOfElection)
            .Select(e => new
                           {
                             e.Name,
                             e.ElectionGuid,
                             e.DateOfElection
                           })
            .ToList()
            .Select(x => new
                           {
                             x.Name,
                             x.ElectionGuid,
                             DateOfElection = x.DateOfElection.AsHtmlString(),
                             IsCurrent = x.ElectionGuid == UserSession.CurrentElectionGuid
                           });
      }
    }

    IQueryable<Election> MyElections()
    {
      var userGuid = UserSession.UserGuid;
      return Db
        .Elections
        .SelectMany(e => Db.JoinElectionUsers.Where(j => j.UserId == userGuid),
                    (e, j) => new {e, j})
        .Where(joined => joined.j.ElectionGuid.Equals(joined.e.ElectionGuid))
        .Select(joined => joined.e);
    }
  }
}