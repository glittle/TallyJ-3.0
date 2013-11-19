using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;

namespace TallyJ.CoreModels.ExportImport
{
  public static class LocalExtensions
  {
    public static IList FilterNulls<T>(this IEnumerable<T> list)
    {
      return list.Select(FilterNullProperties).ToList();
    }

    public static object NullIfEquals(this bool? input, bool defaultValue)
    {
      if (input.HasValue)
      {
        return input.Value.NullIfEquals(defaultValue);
      }
      return null;
    }

    public static object NullIfEquals(this bool input, bool defaultValue)
    {
      if (input == defaultValue)
      {
        return null;
      }
      return input;
    }

    private static object FilterNullProperties<T>(T o)
    {
      var target = Activator.CreateInstance<T>();
      foreach (var keyValuePair in o.GetAllProperties().Where(keyValuePair => keyValuePair.Value != null))
      {
        target.SetPropertyValue(keyValuePair.Key, keyValuePair.Value);
      }
      return target;
    }
  }
}