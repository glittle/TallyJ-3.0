using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;
using System.Web.SessionState;

namespace TallyJ.Code.Session
{
  /// <summary>
  /// Access to HttpContext.
  /// </summary>
  /// <remarks>Can be extended to support Testing environment.</remarks>
  public class HttpCurrentContext : ICurrentContext
  {
    public IDictionary Items
    {
      get { return HttpContext.Current.Items; }
    }

    public ISessionWrapper Session
    {
      get { return new SessionWrapper(); }
    }
  }

  public interface ISessionWrapper : IDictionary<string, object> {
    bool IsAvailable { get; }
  }

  public class SessionWrapper : ISessionWrapper
  {
    HttpSessionState _session = HttpContext.Current.Session;

    public bool IsAvailable => _session != null;

    public object this[string key]
    {
      get
      {
        return _session[key];
      }

      set
      {
        _session[key] = value;
      }
    }

    public int Count
    {
      get
      {
        return _session.Count;
      }
    }

    public bool IsReadOnly
    {
      get
      {
        return _session.IsReadOnly;
      }
    }

    public ICollection<string> Keys
    {
      get
      {
      throw new NotImplementedException();
      }
    }

    public ICollection<object> Values
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public void Add(KeyValuePair<string, object> item)
    {
      _session.Add(item.Key, item.Value);
    }

    public void Add(string key, object value)
    {
      _session.Add(key, value);
    }

    public void Clear()
    {
      _session.Clear();
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
      throw new NotImplementedException();
    }

    public bool ContainsKey(string key)
    {
      throw new NotImplementedException();
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
      throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
      throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<string, object> item)
    {
      throw new NotImplementedException();
    }

    public bool Remove(string key)
    {
      _session.Remove(key);
      return true;
    }

    public bool TryGetValue(string key, out object value)
    {
      throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      throw new NotImplementedException();
    }
  }
}