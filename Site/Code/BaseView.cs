using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using TallyJ.Code.Data;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
    public abstract class BaseView : BaseView<object>
    {
        // is there a better way to have two base classes?
    }

    public abstract class BaseView<TModel> : WebViewPage<TModel>
    {
        private ITallyJDbContext _db;
        private IViewResourcesHelper _viewResourcesHelper;

        /// <summary>
        ///     Access to the database
        /// </summary>
        public ITallyJDbContext DbContext
        {
            get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
        }

        public string ControllerActionNames
        {
            get { return ControllerName + " " + ActionName; }
        }

        public string ControllerActionNamesJoined
        {
            get { return ControllerName + ActionName; }
        }

        public string ControllerName
        {
            get { return Url.RequestContext.RouteData.Values["controller"].ToString(); }
        }

        public string ActionName
        {
            get { return Url.RequestContext.RouteData.Values["action"].ToString(); }
        }
        
        /// <Summary>Either Normal (large logo), Mini (work pages), or Full (presentation)</Summary>
        public string ScreenMode
        {
            get
            {
                var mode = "Mini";

                switch (ControllerName)
                {
                    case "Public":
                    case "Account":
                        mode = "Normal";
                        break;

                        //case "Dashboard":
                        //  switch (ActionName)
                        //  {
                        //    case "ChooseElection":
                        //      break;
                        //    default:
                        //      mode = "Normal";
                        //      break;
                        //  }
                        //  break;

                    case "RollCall":
                    case "Presenter":
                        mode = "Full";
                        break;
                }
                return mode;
            }
        }

        protected IHtmlString AddResourceFilesForViews(string extension, params string[] secondaryExtensions)
        {
            return _viewResourcesHelper.CreateTagsToReferenceContentFiles(extension, secondaryExtensions);
        }


        /// <summary>
        ///     Return a client usable URL to the supplied URL. If {0} is included, a modifier is inserted, either the production or debugging one.
        /// </summary>
        public string ClientFile(string url, string productionNameModifier = "", string debuggingNameModifier = "")
        {
            return url.AsClientFileWithVersion(productionNameModifier, debuggingNameModifier);
        }


        protected override void InitializePage()
        {
            base.InitializePage();

            _viewResourcesHelper = UnityInstance.Resolve<IViewResourcesHelper>();
            _viewResourcesHelper.Register(this);
        }

        public MvcHtmlString ActionLink2(string linkText, string actionName, string controllerName, bool show = true)
        {
            if (!show) return null;
            return Html
                .ActionLink(linkText, actionName, controllerName, null,
                            controllerName == ControllerName && actionName == ActionName ? new {Class = "Active"} : null)
                .ToString()
                .SurroundWith("<li>", "</li>")
                .AsRawMvcHtml();
        }
    }
}