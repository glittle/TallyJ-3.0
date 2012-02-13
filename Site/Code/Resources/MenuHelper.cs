using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using TallyJ.Code.Session;

namespace TallyJ.Code.Resources
{
  public class MenuHelper
  {
    private readonly WebViewPage _webViewPage;
    private string _currentMenuTitle;
    private XmlElement _root;

    public MenuHelper(WebViewPage webViewPage)
    {
      _webViewPage = webViewPage;
    }

    /// <Summary>Available after the menu has been scanned.</Summary>
    public string CurrentMenuTitle
    {
      get
      {
        var nodes = MainRootXml().SelectNodes("//*");
        if (nodes == null) return "";

        var routeData = _webViewPage.Url.RequestContext.RouteData;
        var currentNode = nodes
          .Cast<XmlNode>()
          .Where(n => n.NodeType == XmlNodeType.Element)
          .Cast<XmlElement>()
          .Single(item => item != null && routeData.Values["controller"].ToString() == item.GetAttribute("controller")
                          && routeData.Values["action"].ToString() == item.GetAttribute("action"));

        var title = currentNode.GetAttribute("title");
        var parentGroup = ((XmlElement)currentNode.ParentNode);
        var showParentTitle = parentGroup.GetAttribute("showTitleInPage").DefaultTo("true") == "true";
        if (showParentTitle)
        {
          var parentTitle = parentGroup.GetAttribute("title");
          return "{0}... {1}".FilledWith(parentTitle, title);
        }

        return title;
      }
    }

    public XmlElement TrimmedMenu(string menuId = "main")
    {
      var root = MainRootXml().SelectSingleNode("*[@id='{0}']".FilledWith(menuId));
      return (XmlElement)root;
    }

    public MvcHtmlString InsertMenu(string menuId = "main")
    {
      var menu = MainRootXml();

      var node = menu.SelectSingleNode("*[@id='{0}']".FilledWith(menuId));

      var topLevelItems = node.ChildNodes;

      var result = topLevelItems
          .Cast<XmlNode>()
          .Where(n => n.NodeType == XmlNodeType.Element)
        .Cast<XmlElement>()
        .Where(Allowed)
        .Select(topLevelNode =>
                  {
                    var children = GetChildren(topLevelNode);
                    if (children.HasNoContent() && topLevelNode.ChildNodes.Count != 0)
                    {
                      return "";
                    }
                    return "<li><a class=fNiv href='#'>{0}</a>{1}<li>".FilledWith(topLevelNode.GetAttribute("title"),
                                                                                  children);
                  })
        .ToList();

      return result.JoinedAsString("", true).AsRawMvcHtml();
    }

    private XmlElement MainRootXml()
    {
      if (_root != null)
      {
        return _root;
      }

      var rawPath = HttpContext.Current.Server.MapPath("~/Views/Menu.xml");
      var doc = new XmlHelper().GetCachedXmlFile(rawPath);
      var root = (XmlElement)doc.DocumentElement.CloneNode(true);
      var nodes = root.SelectNodes("//*");
      foreach (var node in nodes
                  .Cast<XmlNode>()
                  .Where(n => n.NodeType == XmlNodeType.Element)
                  .Cast<XmlElement>()
                  .Where(node => !Allowed(node)))
      {
        node.ParentNode.RemoveChild(node);
      }

      // trim empty groups
      foreach (var node in nodes
                  .Cast<XmlNode>()
                  .Where(n => n.NodeType == XmlNodeType.Element)
                  .Cast<XmlElement>()
                  .Where(node => node.Name == "group" && node.ChildNodes.Count == 0))
      {
        node.ParentNode.RemoveChild(node);
      }

      _root = root;

      return root;
    }

    private string GetChildren(XmlNode parent)
    {
      return parent.ChildNodes
          .Cast<XmlNode>()
          .Where(n => n.NodeType == XmlNodeType.Element)
        .Cast<XmlElement>()
        .Where(Allowed)
        .DefaultIfEmpty()
        .CheckForCurrentMenu(_webViewPage, out _currentMenuTitle)
        .Select(i => i == null
                       ? ""
                       : "<li><a href='{0}' title='{2}'{3}>{1}</a></li>".FilledWith(
                         _webViewPage.Url.Action(i.GetAttribute("action"), i.GetAttribute("controller"))
                         , i.GetAttribute("title")
                         , i.GetAttribute("desc")
                         , i.GetAttribute("class").SurroundContentWith(" class='", "'")))
        .JoinedAsString()
        .SurroundContentWith("<ul class='submenu'>", "</ul>");
    }

    private bool Allowed(XmlElement node)
    {
      var role = node.GetAttribute("role");
      var hasElection = UserSession.CurrentElectionGuid != Guid.Empty;

      if (node.GetAttribute("requireElection") == "true" && !hasElection) return false;

      if (role == "*" || role.HasNoContent()) return true;

      if (role == "guest" && (UserSession.IsGuestTeller || UserSession.IsKnownTeller)) return true;

      if (role == "known" && UserSession.IsKnownTeller) return true;

      if (role == "anon" && !(UserSession.IsGuestTeller || UserSession.IsKnownTeller)) return true;

      return false;
    }
  }

  public static class ExtForMenu
  {
    public static IEnumerable<XmlElement> CheckForCurrentMenu(this IEnumerable<XmlElement> inputs,
                                                              WebViewPage webViewPage, out string currentMenuTitle)
    {
      // want to have out param, so can't use iterator
      var list = new List<XmlElement>();


      var routeData = webViewPage.Url.RequestContext.RouteData;
      currentMenuTitle = "";
      foreach (var item in inputs)
      {
        if (item == null) continue;

        if (routeData.Values["controller"].ToString() == item.GetAttribute("controller")
            && routeData.Values["action"].ToString() == item.GetAttribute("action"))
        {
          item.SetAttribute("class", item.GetAttribute("class").SurroundContentWith("", " ") + "active");
          currentMenuTitle = item.GetAttribute("title");
        }

        list.Add(item);
      }

      return list;
    }
  }
}