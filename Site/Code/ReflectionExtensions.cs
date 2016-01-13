using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;

namespace TallyJ.Code
{
  public static class ReflectionExtensions
  {
    public static IDictionary<string, object> GetAllProperties(this object data)
    {
      return GetAllProperties(data, null);
    }

    /// <summary>
    ///     Get a dictionary of all the named properties. The results are ordered based on the underlying class, not the order of names provided
    /// </summary>
    public static IDictionary<string, object> GetAllProperties(this object data, IEnumerable<string> wantedNames)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;
      return data.GetType()
        .GetProperties(attr)
        .Where(property => property.CanRead && (wantedNames == null || wantedNames.Contains(property.Name)))
        .ToDictionary(property => property.Name, property => property.GetValue(data, null));
    }

    public static IDictionary<string, object> GetPropertiesExcept(this object data, IEnumerable<string> wantedNames,
                                                                  IEnumerable<string> namesNotWanted)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;
      return data.GetType()
        .GetProperties(attr)
        .Where(
          property => property.CanRead && (wantedNames==null || wantedNames.Contains(property.Name)) && !namesNotWanted.Contains(property.Name))
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

    /// <summary>
    ///     Get the typed value of this property. Caller must know the type expected.
    /// </summary>
    public static T GetPropertyValue<T>(this object obj, string propertyName)
    {
      return GetPropertyValue(obj, propertyName, default(T));
    }

    /// <summary>
    ///     Get the typed value of this property. Caller must know the type expected.
    /// </summary>
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

    /// <summary>
    ///     Get the value of this property, as an object.
    /// </summary>
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

    /// <summary>
    ///     Set the value of this property, as an object.
    /// </summary>
    /// <remarks>
    ///     Callee should validate the value... if it cannot be coersed to fit the property, an exception will be thrown.
    /// </remarks>
    public static void SetPropertyValue<T>(this object obj, string propertyName, T objValue)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;

      var type = obj.GetType();

      var property = type.GetProperty(propertyName, attr);

      if (property == null) return;

      var propertyType = property.PropertyType;

      var isNullable = (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>));
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

    /// <summary>
    ///     Get names of all members of this type of object
    /// </summary>
    public static IEnumerable<string> GetAllMemberNamesExcept(this Type input, IEnumerable<string> namesToSkip)
    {
      const BindingFlags attr = BindingFlags.Public | BindingFlags.Static;

      return input.GetMembers(attr).Where(mi => !namesToSkip.Contains(mi.Name)).Select(mi => mi.Name).ToArray();
    }


    /// <summary>
    ///     Copies all matching properties (by name) to the target object
    /// </summary>
    /// <param name="incoming"> The entity to read. </param>
    /// <param name="target"> The entity to update. </param>
    /// <param name="propertyNames"> List of property names to copy </param>
    /// <returns> Returns true if any of the properties were changed. </returns>
    public static bool CopyPropertyValuesTo<T>(this T incoming, T target, IEnumerable<string> propertyNames)
    {
      var changed = 0;
      foreach (var propertyWithNewValue in
        incoming.GetAllProperties()
          .Where(valuePair => propertyNames.Contains(valuePair.Key) && target.GetPropertyType(valuePair.Key) != null)
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

    /// <summary>
    ///     Copies all matching attributes (by name) to the target object
    /// </summary>
    /// <param name="incoming"> The XmlElement to read. </param>
    /// <param name="target"> The entity to update. </param>
    /// <returns> Returns true if any of the properties were changed. </returns>
    public static bool CopyAttributeValuesTo<T>(this XmlElement incoming, T target)
    {
      var changed = 0;

      foreach (var attrib in
        incoming.Attributes
          .Cast<XmlAttribute>()
          .Where(attrib =>
            {
              var currentValue = target.GetPropertyValue(attrib.Name);
              if (attrib.Value.HasNoContent() && currentValue == null) return false;

              return attrib.Value.HasNoContent() || !attrib.Value.Equals(currentValue);
            }))
      {
        if (target.SetMatchedPropertyValueFromString(attrib.Name, attrib.Value))
        {
          changed++;
        }
      }
      return changed > 0;
    }

    public static void CopyAttributeValueTo<T,T2>(this XmlElement input, string attrName, T obj, Expression<Func<T, T2>> action) {
      var value = input.GetAttribute(attrName);
      if (value == "") {
        return;
      }
      var prop = ((MemberExpression)action.Body).Member as PropertyInfo;
      prop.SetValue(obj, value, null);
    }

    /// <summary>
    ///     Set the value of this property, as an object.
    /// </summary>
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
 
    /// <summary>
    ///     Set the value of this property, from the string value.  If the string is empty, the property is not changed.
    /// </summary>
    private static bool SetMatchedPropertyValueFromString(this object targetObject, string propertyName, string newValueString)
    {
      // if blank coming in, don't change it
      if (newValueString.HasNoContent())
      {
        return false;
      }

      const BindingFlags attr = BindingFlags.Public | BindingFlags.Instance;
      var targetObjectType = targetObject.GetType();

      var targetProperty = targetObjectType.GetProperty(propertyName, attr);
      if (targetProperty == null) return false;

      var targetPropertyType = targetProperty.PropertyType;

      if (targetPropertyType.IsGenericType && targetPropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
      {
        targetPropertyType = Nullable.GetUnderlyingType(targetPropertyType);
      }

      // need to change some types... 
      var typeName = targetPropertyType.Name;

      object realValue;
      if (typeName == typeof(DateTime).Name)
      {
        realValue = DateTime.Parse(newValueString);
      }
      else if(typeName == typeof(Boolean).Name)
      {
        realValue = newValueString.AsBoolean();
      }
      else if(typeName == typeof(Guid).Name)
      {
        realValue = newValueString.AsGuid();
      }
      else
      {
        realValue = Convert.ChangeType(newValueString, targetPropertyType);
      }

      if (realValue.Equals(targetProperty.GetValue(targetObject, null)))
      {
        return false;
      }

      targetProperty.SetValue(targetObject, realValue, null);
      return true;
    }

    /// <Summary>Get the property name as a string.  e.g.  myObject.GetName(x=>x.Property)</Summary>
    public static string GetPropertyName<T,TReturn>(this T input, Expression<Func<T,TReturn>> property) where T: class
    {

      var memberExpression = property.Body as MemberExpression;
      if (memberExpression == null)
      {
        throw new ApplicationException("Should be '()=>x.y'. Invalid syntax: " + property.Body);
      }
      return memberExpression.Member.Name;
    }
  }
}