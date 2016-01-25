using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TallyJ.Code.Session;

namespace Tests.Support
{
  public class FakeCurrentContext : ICurrentContext
  {
    Dictionary<string, object> _items = new Dictionary<string, object>();
    ISessionWrapper _fakeSessionWrapper = new FakeSessionWrapper();

    public IDictionary Items
    {
      get
      {
        return _items;
      }
    }

    public ISessionWrapper Session
      { get{
          return _fakeSessionWrapper;

        } }
  }
}
