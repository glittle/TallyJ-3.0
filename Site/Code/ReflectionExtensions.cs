using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TallyJ.Code
{
  public static class ReflectionExtensions
  {
    public static IDictionary<string, object> GetAllProperties(this object data)
    {
      return GetAllProperties(data, null);
    }

    /// <summary>Get a dictionary of all the named properties. The results are ordered based on the underlying class, not the order of names provided</summary>
    public static IDictionary<string, object> GetAllProperties(this object data, IEnumerable<string> wantedNames)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;
      return data.GetType()
        .GetProperties(attr)
        .Where(property => property.CanRead && (wantedNames == null || wantedNames.Contains(property.Name)))
        .ToDictionary(property => property.Name, property => property.GetValue(data, null));
    }

    public static IDictionary<string, object> GetPropertiesExcept(this object data, IEnumerable<string> wantedNames, IEnumerable<string> namesNotWanted)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;
      return data.GetType()
        .GetProperties(attr)
        .Where(property => property.CanRead && wantedNames.Contains(property.Name) && !namesNotWanted.Contains(property.Name))
        .ToDictionary(property => property.Name, property => property.GetValue(data, null));
    }

    public static IEnumerable<PropertyInfo> GetAllPropertyInfos(this object data)
    {
      return GetAllPropertyInfos(data, null);
    }

    public static IEnumerable<PropertyInfo> GetAllPropertyInfos(this object data, IEnumerable<string> wantedNames)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;
      return data.GetType()
        .GetProperties(attr)
        .Where(property => property.CanRead && (wantedNames == null || wantedNames.Contains(property.Name)))
        .ToList();
    }

    /// <summary>Get the typed value of this property. Caller must know the type expected.</summary>
    public static T GetPropertyValue<T>(this object obj, string propertyName)
    {
      return GetPropertyValue(obj, propertyName, default(T));
    }

    /// <summary>Get the typed value of this property. Caller must know the type expected.</summary>
    public static T GetPropertyValue<T>(this object obj, string propertyName, T defaultValue)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;

      var type = obj.GetType();
      
      var property = type.GetProperty(propertyName, attr);
      if (property == null)
      {
        return defaultValue;
      }

      return (T) Convert.ChangeType(property.GetValue(obj, null), typeof (T));
    }

    /// <summary>Get the value of this property, as an object.</summary>
    public static object GetPropertyValue(this object obj, string propertyName)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;

      var type = obj.GetType();
      
      var property = type.GetProperty(propertyName, attr);
      
      return property == null ? null : property.GetValue(obj, null);
    }

    public static Type GetPropertyType(this object obj, string propertyName)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;

      var type = obj.GetType();
      
      var property = type.GetProperty(propertyName, attr);
      
      return property == null ? null : property.PropertyType;
    }

    /// <summary>Set the value of this property, as an object.</summary>
    /// <remarks>Callee should validate the value... if it cannot be coersed to fit the property, an exception will be thrown.</remarks>
    public static void SetPropertyValue<T>(this object obj, string propertyName, T objValue)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;

      var type = obj.GetType();
        
      var property = type.GetProperty(propertyName, attr);
      
      if(property == null)return;

      var propertyType = property.PropertyType;

      var isNullable = (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
      var underlyingType = Nullable.GetUnderlyingType(propertyType);

      if (propertyType.IsValueType && !isNullable && objValue == null)
      {
        // This works for most value types, but not custom ones
        objValue = default(T);
      }

      // need to change some types... bool may come in as a string...
      var realValue = objValue == null ? null : Convert.ChangeType(objValue, underlyingType ?? propertyType);

      property.SetValue(obj, realValue, null);
    }

    /// <summary>Get names of all members of this type of object</summary>
    public static IEnumerable<string> GetAllMemberNamesExcept(this Type input, IEnumerable<string> namesToSkip)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Static;

      return input.GetMembers(attr).Where(mi => !namesToSkip.Contains(mi.Name)).Select(mi => mi.Name).ToArray();
    }


    /// <summary>Copies all matching properties (by name) to the target object</summary>
    /// <param name="incoming">The entity to read. </param>
    /// <param name="target">The entity to update.</param>
    /// <param name="propertyNames">List of property names to copy</param>
    /// <returns>Returns true if any of the properties were changed.</returns>
    public static bool CopyPropertyValuesTo<T>(this T incoming, T target, IEnumerable<string> propertyNames)
    {
      var changed = 0;
      foreach (var propertyWithNewValue in
        incoming.GetAllProperties()
        .Where(valuePair => propertyNames.Contains(valuePair.Key) && target.GetPropertyType(valuePair.Key)!=null)
        .Where(newKeyValue =>
                 {
                   var currentValue = target.GetPropertyValue(newKeyValue.Key);
                   if (newKeyValue.Value == null && currentValue == null) return false;
                   
                   return newKeyValue.Value == null || !newKeyValue.Value.Equals(currentValue);
                 }))
      {
        target.SetMatchedPropertyValue(propertyWithNewValue.Key, propertyWithNewValue.Value);
        changed++;
      }
      return changed > 0;
    }

    /// <summary>Set the value of this property, as an object.</summary>
    private static void SetMatchedPropertyValue<T>(this object obj, string propertyName, T objValue)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;
      var type = obj.GetType();

      var property = type.GetProperty(propertyName, attr);
      if (property == null) return;

      //var isNullable = (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
      
      //var propertyType = property.PropertyType;

      //if (propertyType.IsValueType && !isNullable && objValue == null)
      //{
      //  // This works for most value types, but not custom ones
      //  objValue = default(T);
      //}

      //// need to change some types... bool may come in as a string...
      //var realValue = objValue != null ? Convert.ChangeType(objValue, propertyType) : objValue;

      property.SetValue(obj, objValue, null);
    }

  }
}