using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.Code.Resources
{
  /// <summary>
  /// A very simplistic, limited version of Sunwapta's XmlResources system
  /// </summary>
  public class LangResourceHelper 
  {
    public string GetFromList(string key, string value)
    {
      var globalXml = new XmlHelper().GetCachedXmlFile("App_GlobalResources/Global.xml");

      var list = globalXml?.DocumentElement?.SelectSingleNode($"List[@Key='{key}']");

      var item = list?.SelectSingleNode($"Item[@Value='{value}']");

      // hard code to english for now
      var text = item?.SelectSingleNode("en-US")?.InnerText;

      return text;
    }
  }
}