using System;
using System.Web;
using TallyJ.Models;

namespace TallyJ.CoreModels
{
  public class ElectionStatusSharer
  {
    private HttpApplicationState _app;

    private HttpApplicationState App
    {
      get { return _app ?? (_app = ((MvcApplication)HttpContext.Current.ApplicationInstance).Application); }
    }

    public string GetStateFor(Guid electionGuid)
    {
      var state = App["E" + electionGuid];
      if (state != null)
      {
        return state.ToString();
      }
      return "";
    }
    public void SetStateFor(Guid electionGuid, string state)
    {
      App.Lock();
      App["E" + electionGuid] = state;
      App.UnLock();
    }

    public void SetStateFor(Election election)
    {
      SetStateFor(election.ElectionGuid, election.TallyStatus);
    }
  }
}