using System;
using System.Collections.Generic;

namespace TallyJ.Code.Enumerations
{
  /*
   * 
   * Sample

  public class CacheKey : BaseEnumeration<CacheKey, string>
  {
    public static readonly CacheKey DalColumnMetaInfo = new CacheKey("DalColumnMetaInfo");
    public static readonly CacheKey DalLookup = new CacheKey("DalLookup");
    public static readonly CacheKey DalDataFilters = new CacheKey("DalDataFilters");

    static CacheKey()
    {
      Add(DalColumnMetaInfo);
      Add(DalLookup);
      Add(DalDataFilters);
    }

    public CacheKey(string key) : base(key, key)
    {
    }

    public static IList<CacheKey> Items { get { return BaseItems; } }
  }

   * 
   * 
   * 
   * 
   * 
   * 
   * 
   */

  public abstract class BaseEnumeration<TSelf, TValue> : IEnumeration<TSelf, TValue>
    where TSelf : BaseEnumeration<TSelf, TValue>
    where TValue : IEquatable<TValue>
  {
    private static TSelf _defaultItem;

    ///<summary>
    ///    A list of the items in this enum
    ///</summary>
    protected static readonly IList<TSelf> BaseItems = new List<TSelf>();

    protected BaseEnumeration()
    {
    }

    protected BaseEnumeration(TValue value, string text)
    {
      Value = value;
      Text = text;
    }

    public Type ReturnedType
    {
      get { return typeof (TSelf); }
    }

    public bool IsMutable
    {
      get { return false; }
    }

    public static TSelf Default
    {
      get { return _defaultItem; }
    }

    public string DisplayText
    {
      get { return Text; }
    }

    #region IEnumeration<TSelf,TValue> Members

    public TValue Value { get; private set; }

    public string Text { get; private set; }

    public abstract IList<TSelf> Items { get; }

    public override string ToString()
    {
      return Value.ToString();
    }

    #endregion

    public static implicit operator string(BaseEnumeration<TSelf, TValue> self)
    {
      return self.ToString();
    }

    public new bool Equals(object x, object y)
    {
      if (x == null || y == null)
      {
        return false;
      }
      return ((BaseEnumeration<TSelf, TValue>) x).Value.Equals(((BaseEnumeration<TSelf, TValue>) y).Value);
    }

    public int GetHashCode(object x)
    {
      return ((BaseEnumeration<TSelf, TValue>) x).Value.GetHashCode();
    }


    //    public object DeepCopy(object value)
    //    {
    //      if (value == null || value.GetType() != typeof (TValue))
    //      {
    //        return null;
    //      }
    //      return GetByValue((TValue) value);
    //    }

    public object Replace(object original, object target, object owner)
    {
      return original;
    }

    public object Assemble(object cached, object owner)
    {
      return cached;
    }

    public object Disassemble(object value)
    {
      return value;
    }

    protected static void Add(TSelf item)
    {
      BaseItems.Add(item);
    }

    protected static void AddAsDefault(TSelf item)
    {
      BaseItems.Add(item);
      _defaultItem = item;
    }


    protected virtual bool TestEquals(TValue value, TValue value1)
    {
      return value.Equals(value1);
    }
  }
}