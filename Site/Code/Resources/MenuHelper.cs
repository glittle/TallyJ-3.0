using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Code.Resources
{
  public class MenuHelper
  {
    private readonly UrlHelper _urlHelper;
    private XmlElement _currentNode;
    private XmlElement _root;
    private Election _currentElection;
    private bool _isGuestTeller;
    private bool _isKnownTeller;

    public MenuHelper(UrlHelper urlHelper)
    {
      _urlHelper = urlHelper;
      _currentElection = UserSession.CurrentElection;
      _isGuestTeller = UserSession.IsGuestTeller;
      _isKnownTeller = UserSession.IsKnownTeller;
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

    public string CurrentPageTitle
    {
      get
      {
        var currentNode = CurrentNode;

        var title = currentNode.GetAttribute("title");
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
        if (nodes == null) return MakeEmptyNode();

        var routeData = _urlHelper.RequestContext.RouteData;

        _currentNode = nodes
          .Cast<XmlNode>()
          .Where(n => n.NodeType == XmlNodeType.Element)
          .Cast<XmlElement>()
          .SingleOrDefault(
            item => item != null && routeData.Values["controller"].ToString() == item.GetAttribute("controller")
                    && routeData.Values["action"].ToString() == item.GetAttribute("action"));

        return _currentNode ?? MakeEmptyNode();
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

    private XmlElement MakeEmptyNode()
    {
      return MainRootXml().OwnerDocument.CreateElement("EmptyDummy");
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

      var hidePreBallotPages = _currentElection == null || _currentElection.HidePreBallotPages.AsBoolean();

      var result = topLevelItems
        .Cast<XmlNode>()
        .Where(n => n.NodeType == XmlNodeType.Element)
        .Cast<XmlElement>()
        .Where(node1 => Allowed(node1, hidePreBallotPages))
        .Select(topLevelNode =>
        {
          var children = GetChildren(topLevelNode, hidePreBallotPages);
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
      var hidePreBallotPages = _currentElection != null && _currentElection.HidePreBallotPages.AsBoolean();
      var nodes = root.SelectNodes("//*");
      foreach (var node in nodes
        .Cast<XmlNode>()
        .Where(n => n.NodeType == XmlNodeType.Element)
        .Cast<XmlElement>()
        .Where(node => { return !Allowed(node, hidePreBallotPages); }))
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

    private string GetChildren(XmlNode parent, bool hidePreBallotPages)
    {
      return parent.ChildNodes
        .Cast<XmlNode>()
        .Where(n => n.NodeType == XmlNodeType.Element)
        .Cast<XmlElement>()
        .Where(node => Allowed(node, hidePreBallotPages))
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

    private bool Allowed(XmlElement node, bool hidePreBallotPages)
    {
      var role = node.GetAttribute("role");
      var hasElection = _currentElection != null;

      // false tests
      if (!hasElection && node.GetAttribute("requireElection") == "true") return false;
      if (hasElection && hidePreBallotPages && node.GetAttribute("isPreBallot") == "true") return false;

      // true tests
      if (role == "*" || role.HasNoContent()) return true;
      if (role == "guest" && (_isGuestTeller || _isKnownTeller)) return true;
      if (role == "known" && _isKnownTeller) return true;
      if (role == "anon" && !(_isGuestTeller || _isKnownTeller)) return true;

      return false;
    }

    public string QuickLinks()
    {
      var nodes = TrimmedMenu().ChildNodes.Cast<XmlElement>().ToList();

      const string linkTemplate = "<a href='{Link}' title=\"{Tip}\" class='{Class} Role-{Role}'>{Title}</a>";

      // for full users, give all menu sets
      var statusItems = ElectionTallyStatusEnum.Items.Select(ts => ts.Value).ToList();
//      statusItems.Add("General");
      var list =
        statusItems
          .Select(tallyStatus =>
            nodes.SelectMany(item =>
              item.ChildNodes.Cast<XmlNode>()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .Cast<XmlElement>()
                .Where(c =>
                {
                  var when = c.GetAttribute("featureWhen");
                  return when == "*" || when.Contains(tallyStatus);
                })).Select(item => linkTemplate.FilledWithObject(new
                {
                  Link = _urlHelper.Action(item.GetAttribute("action"), item.GetAttribute("controller")),
                  Class = item.GetAttribute("class"),
                  Role = item.GetAttribute("role"),
                  Title = item.GetAttribute("title"),
                  Tip = item.GetAttribute("desc"),
                }))
              .JoinedAsString("")
              .SurroundContentWith(
                "<span id=menu{0}{1}>".FilledWith(tallyStatus,
                  UserSession.IsFeatured(tallyStatus, UserSession.CurrentElection) ? "" : " class=Hidden"), "</span>")).ToList();

      return list.JoinedAsString("");
    }

    public HtmlString StateSelectorItems()
    {
      return ElectionTallyStatusEnum.ForHtmlList(_currentElection);
      //      if (UserSession.IsKnownTeller)
      //      {
      //        return ElectionTallyStatusEnum.ForHtmlList(UserSession.CurrentElection);
      //      }
      //      return ElectionTallyStatusEnum.ForHtmlList(UserSession.CurrentElection, false);
    }

    public bool IsFeatured(string pageFeatureWhen)
    {
      return UserSession.IsFeatured(pageFeatureWhen, _currentElection);
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