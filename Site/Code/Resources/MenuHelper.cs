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
    private readonly UrlHelper _urlHelper;
    private XmlElement _currentNode;
    private XmlElement _root;

    public MenuHelper(WebViewPage viewPage)
    {
      _urlHelper = viewPage.Url;
    }

    public MenuHelper(Controller controller)
    {
      _urlHelper = new UrlHelper(controller.ControllerContext.RequestContext);
    }

    /// <Summary>Title of current menu item, if there is one. Empty string if not.</Summary>
    public string CurrentMenuTitle
    {
      get
      {
        var currentNode = CurrentNode;

        var title = currentNode.GetAttribute("title");
        var parentGroup = ((XmlElement)currentNode.ParentNode);
        var showParentTitle = parentGroup != null &&
                              parentGroup.GetAttribute("showTitleInPage").DefaultTo("true") == "true";
        if (showParentTitle)
        {
          var parentTitle = parentGroup.GetAttribute("title");
          return "{0} - {1}".FilledWith(parentTitle, title);
        }

        return title;
      }
    }

    /// <Summary>Current menu node. If none, returns an empty element.</Summary>
    public XmlElement CurrentNode
    {
      get
      {
        if (_currentNode != null) return _currentNode;

        var nodes = MainRootXml().SelectNodes("//*");
        if (nodes == null) return MainRootXml().OwnerDocument.CreateElement("EmptyDummy");

        var routeData = _urlHelper.RequestContext.RouteData;

        _currentNode = nodes
          .Cast<XmlNode>()
          .Where(n => n.NodeType == XmlNodeType.Element)
          .Cast<XmlElement>()
          .Single(item => item != null && routeData.Values["controller"].ToString() == item.GetAttribute("controller")
                          && routeData.Values["action"].ToString() == item.GetAttribute("action"));

        return _currentNode;
      }
    }

    public bool ShowLocationSelection
    {
      get { return CurrentNode.GetAttribute("showLocationSelector").AsBoolean(); }
    }

    public bool ShowTellerSelector
    {
      get { return CurrentNode.GetAttribute("showTellerSelector").AsBoolean(); }
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
        .CheckForCurrentMenu(_urlHelper)
        .Select(i => i == null
                       ? ""
                       : "<li><a href='{0}' title='{2}'{3}>{1}</a></li>".FilledWith(
                         _urlHelper.Action(i.GetAttribute("action"), i.GetAttribute("controller"))
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

    public IEnumerable<string> QuickLinks()
    {
      return TrimmedMenu().ChildNodes
        .Cast<XmlElement>()
        .SelectMany(item => item.ChildNodes
                              .Cast<XmlNode>()
                              .Where(n => n.NodeType == XmlNodeType.Element)
                              .Cast<XmlElement>()
                              .Where(c => UserSession.IsFeatured(c.GetAttribute("featureWhen"))))
        .Select(item => "<li><a href='{Link}' class='{Class} Role-{Role}'>{Title}</a></li>".FilledWithObject(
          new
            {
              Link = _urlHelper.Action(item.GetAttribute("action"), item.GetAttribute("controller")),
              Class = item.GetAttribute("class"),
              Role = item.GetAttribute("role"),
              Title = item.GetAttribute("title"),
            }
          ));
    }
  }

  public static class ExtForMenu
  {
    public static IEnumerable<XmlElement> CheckForCurrentMenu(this IEnumerable<XmlElement> inputs,
                                                              UrlHelper urlHelper)
    {
      // want to have out param, so can't use iterator
      var list = new List<XmlElement>();


      var routeData = urlHelper.RequestContext.RouteData;
      foreach (var item in inputs.Where(item => item != null))
      {
        if (routeData.Values["controller"].ToString() == item.GetAttribute("controller")
            && routeData.Values["action"].ToString() == item.GetAttribute("action"))
        {
          item.SetAttribute("class", item.GetAttribute("class").SurroundContentWith("", " ") + "active");
        }

        list.Add(item);
      }

      return list;
    }
  }
}