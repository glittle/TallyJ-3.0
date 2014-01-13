using System;
using System.Web;
using System.Web.Caching;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ElectionStatusSharer
  {
    private const string CachePrefix = "ElectionState";
    //private HttpApplicationState _app;

    //private HttpApplicationState App
    //{
    //    get { return _app ?? (_app = (HttpContext.Current.ApplicationInstance).Application); }
    //}

    private Cache HttpCache
    {
      get { return HttpContext.Current.Cache; }
    }

    public string GetStateFor(Guid electionGuid)
    {
      var state = HttpCache[CachePrefix + electionGuid];
      if (state != null)
      {
        return state.ToString();
      }
      return "";
    }

    private void SetStateFor(Guid electionGuid, string state)
    {
      HttpCache[CachePrefix + electionGuid] = state;
    }

    public void SetStateFor(Election election)
    {
      SetStateFor(election.ElectionGuid, election.TallyStatus);

    }
  }
}