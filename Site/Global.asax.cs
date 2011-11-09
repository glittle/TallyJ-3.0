using System.Configuration;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using TallyJ.Code;
using TallyJ.Code.UnityRelated;
using Unity.Mvc3;

namespace TallyJ
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : HttpApplication
	{
		protected void Application_Start()
		{
			ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(UnityInstance.Container));
			DependencyResolver.SetResolver(new UnityDependencyResolver(UnityInstance.Container));

		  FixUpConnectionString();
			Bootstrapper.Initialise();

			AreaRegistration.RegisterAllAreas();

			RegisterGlobalFilters(GlobalFilters.Filters);
			RegisterRoutes(RouteTable.Routes);
		}

	  void FixUpConnectionString()
	  {
      var cnString = ConfigurationManager.ConnectionStrings["MainConnection"];

      var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
      fi.SetValue(cnString, false);

      cnString.ConnectionString = cnString.ConnectionString + ";MultipleActiveResultSets=True";
	  }

	  public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
				"Default", // Route name
				"{controller}/{action}/{id}", // URL with parameters
				new
					{
						controller = "Public",
						action = "Index",
						id = UrlParameter.Optional
					} // Parameter defaults
				);
		}
	}
}