using System;
using System.Collections.Generic;

namespace TallyJ.Code.Enumerations
{
  public interface IEnumeration
  {
  }

  public interface IEnumeration<TSelf, out TValue> : IEnumeration
    where TSelf : BaseEnumeration<TSelf, TValue>
    where TValue : IEquatable<TValue>

  {
    IList<TSelf> Items { get; }
    TValue Value { get; }
    string Text { get; }
    string ToString();
  }
}