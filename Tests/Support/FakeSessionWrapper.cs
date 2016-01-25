using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TallyJ.Code.Session;

namespace Tests.Support
{
  public class FakeSessionWrapper : ISessionWrapper
  {
    Dictionary<string, object> _dict = new Dictionary<string, object>();

    public object this[string key]
    {
      get
      {
        return _dict[key];
      }

      set
      {
        _dict[key] = value;
      }
    }

    public int Count
    {
      get
      {
        return _dict.Count;
      }
    }

    public bool IsReadOnly
    {
      get
      {
        return false;
      }
    }

    public ICollection<string> Keys
    {
      get
      {
        return _dict.Keys;
      }
    }

    public ICollection<object> Values
    {
      get
      {
        return _dict.Values;
      }
    }

    public void Add(KeyValuePair<string, object> item)
    {
      _dict.Add(item.Key, item.Value);
    }

    public void Add(string key, object value)
    {
      _dict.Add(key, value);
    }

    public void Clear()
    {
      _dict.Clear();
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
      return _dict.Contains(item);
    }

    public bool ContainsKey(string key)
    {
      return _dict.ContainsKey(key);
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
      throw new NotImplementedException();
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
