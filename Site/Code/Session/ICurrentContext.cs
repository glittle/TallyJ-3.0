using System.Collections;
using System.Web.Caching;
using System.Web.SessionState;

namespace TallyJ.Code.Session
{
  public interface ICurrentContext
  {
    IDictionary Items { get; }

    ISessionWrapper Session { get; }
  }
}