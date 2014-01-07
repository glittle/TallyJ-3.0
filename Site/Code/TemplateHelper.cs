using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TallyJ.Code
{
  public class TemplateHelper
  {
    readonly string _template;

    public TemplateHelper(string template)
    {
      _template = template;
    }


    public string FillByName(IDictionary<string, object> properties)
    {
      const string tokenPattern = "{([^{]+?)}";
      var result = Replace(properties, _template, tokenPattern);
      var lastResult = "";

      while (lastResult != result && Regex.IsMatch(result, tokenPattern))
      {
        lastResult = result;
        result = Replace(properties, result, tokenPattern);
      }

      return result;
    }

    string Replace(IDictionary<string, object> properties, string template, string tokenPattern)
    {
      return Regex.Replace(template, tokenPattern, match =>
      {
        var token = match.Value;
        var key = token.Substring(1, token.Length - 2);
        if (key.StartsWith("^"))
        {
          key = key.Substring(1); // ignore in c#
        }
        if (properties.ContainsKey(key))
        {
          var value = properties[key];
          return value == null ? "" : value.ToString();
        }
        return token;
      });
    }


    public string FillByArray<T>(IEnumerable<T> values)
    {
      var array = values.ToArray();
      var lastIndex = array.Length - 1;
      if (lastIndex < 0)
      {
        return _template;
      }

      return Regex.Replace(_template, "{([^{]+?)}", delegate(Match match)
                                                      {
                                                        var token = match.Value;
                                                        var arrayIndex = token.Substring(1, token.Length - 2).AsInt();
                                                        if (arrayIndex < 0 || arrayIndex > lastIndex)
                                                        {
                                                          return token;
                                                        }
                                                        var value = array[arrayIndex];
                                                        return value.ToString();
                                                      });
    }
  }
}