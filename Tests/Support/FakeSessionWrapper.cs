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

    /// <summary>
    /// Adds a key-value pair to the dictionary.
    /// </summary>
    /// <param name="key">The key of the pair to be added.</param>
    /// <param name="value">The value of the pair to be added.</param>
    /// <remarks>
    /// This method adds a key-value pair to the dictionary. If the key already exists, an exception will be thrown.
    /// </remarks>
    public void Add(KeyValuePair<string, object> item)
    {
      _dict.Add(item.Key, item.Value);
    }

    public void Add(string key, object value)
    {
      _dict.Add(key, value);
    }

    /// <summary>
    /// Clears all the elements from the dictionary.
    /// </summary>
    public void Clear()
    {
      _dict.Clear();
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key-value pair.
    /// </summary>
    /// <param name="item">The key-value pair to locate in the dictionary.</param>
    /// <returns>True if the dictionary contains an element with the specified key and value; otherwise, false.</returns>
    public bool Contains(KeyValuePair<string, object> item)
    {
      return _dict.Contains(item);
    }

    /// <summary>
    /// Checks if the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the dictionary.</param>
    /// <returns>True if the dictionary contains an element with the specified key; otherwise, false.</returns>
    public bool ContainsKey(string key)
    {
      return _dict.ContainsKey(key);
    }

    /// <summary>
    /// Copies the elements of the ICollection to an Array, starting at a particular Array index.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements copied from ICollection. The Array must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    /// <exception cref="NotImplementedException">The method or operation is not implemented.</exception>
    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    /// <exception cref="NotImplementedException">Thrown when the method is not implemented.</exception>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Removes the specified key from the collection.
    /// </summary>
    /// <param name="key">The key to be removed.</param>
    /// <exception cref="NotImplementedException">The method is not implemented.</exception>
    public bool Remove(KeyValuePair<string, object> item)
    {
      throw new NotImplementedException();
    }

    public bool Remove(string key)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Tries to get the value associated with the specified key from the collection.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
    /// <returns>true if the object that implements IDictionary contains an element with the specified key; otherwise, false.</returns>
    /// <exception cref="System.NotImplementedException">The method or operation is not implemented.</exception>
    public bool TryGetValue(string key, out object value)
    {
      throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      throw new NotImplementedException();
    }

    public bool IsAvailable => true;
  }
}
