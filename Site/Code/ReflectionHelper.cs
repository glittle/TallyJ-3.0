using System;
using System.Linq.Expressions;

namespace TallyJ.Code
{
  public static class ReflectionHelper
  {
    /// <Summary>Get the property name as a string.  e.g.  GetName(() => myObject.Property)</Summary>
    public static string GetName<T>(Expression<Func<T>> expression)
    {
      var memberExpression = expression.Body as MemberExpression;
      if (memberExpression == null)
      {
        throw new ApplicationException("Should be '()=>x.y'. Invalid syntax: " + expression.Body);
      }
      return memberExpression.Member.Name;
    }

    /// <Summary>Get the name of this class</Summary>
    public static string GetName<T>(T input)
    {
      return typeof (T).Name;
    }
  }
}