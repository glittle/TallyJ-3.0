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
      var globalXml = new XmlHelper().GetCachedXmlFile(@"App_GlobalResources\Global.xml");

      var list = globalXml?.DocumentElement?.SelectSingleNode($"List[@key='{key}']");

      var item = list?.SelectSingleNode($"Item[@value='{value}']");

      return item?.InnerText;
    }
  }
}