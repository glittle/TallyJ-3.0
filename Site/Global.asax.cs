using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Profile;
using System.Web.Routing;
using FluentSecurity;
//using Microsoft.Web.Redis;
using NLog;
using NLog.Targets;
//using RedisSessionProvider.Config;
//using StackExchange.Redis;
using TallyJ.Code;
using TallyJ.Code.Data;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.Controllers;
using TallyJ.EF;
using Unity.Mvc3;
//using Configuration = TallyJ.Migrations.Configuration;

namespace TallyJ
{
  // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
  // visit http://go.microsoft.com/?LinkId=9394801

  public class MvcApplication : HttpApplication
  {
    //public static ConfigurationOptions ConfigOpts { get; set; }

    protected void Application_Start()
    {
      //ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(UnityInstance.Container));

      DependencyResolver.SetResolver(new UnityDependencyResolver(UnityInstance.Container));

      ViewEngines.Engines.Clear();
      ViewEngines.Engines.Add(new RazorViewEngine());

      SetupEnvironment();

      Bootstrapper.Initialise();

      SecurityConfigurator.Configure(
          configuration =>
          {
            // http://www.fluentsecurity.net/getting-started

            // Let Fluent Security know how to get the authentication status of the current user
            configuration.GetAuthenticationStatusFrom(() => HttpContext.Current.User.Identity.IsAuthenticated);

            configuration.ResolveServicesUsing(type => UnityInstance.Container.ResolveAll(type));

            // This is where you set up the policies you want Fluent Security to enforce on your controllers and actions
            configuration.ForAllControllers().DenyAnonymousAccess();

            configuration.For<PublicController>().Ignore();
            configuration.For<AccountController>().Ignore();


            configuration.For<AfterController>().AddPolicy(new RequireElectionPolicy());

            configuration.For<BallotsController>().AddPolicy(new RequireElectionPolicy());
            //configuration.For<BallotsController>().AddPolicy(new RequireLocationPolicy());

            configuration.For<BeforeController>().AddPolicy(new RequireElectionPolicy());

            configuration.For<DashboardController>().DenyAnonymousAccess();

            configuration.For<ElectionsController>().DenyAnonymousAccess();

            configuration.For<PeopleController>().AddPolicy(new RequireElectionPolicy());

            configuration.For<SetupController>().AddPolicy(new RequireElectionPolicy());
            configuration.For<SetupController>(x => x.Upload()).AddPolicy(new RequireElectionPolicy());

            //configuration.For<AccountController>(x => x.LogOn()).DenyAuthenticatedAccess();
            //configuration.For<AccountController>(x => x.Register()).DenyAuthenticatedAccess();
            configuration.For<AccountController>(x => x.ChangePassword()).DenyAnonymousAccess();
          });

      RegisterGlobalFilters(GlobalFilters.Filters);
      RegisterGeneralRoutes(RouteTable.Routes);
    }

    private void SetupEnvironment()
    {
      var siteInfo = new SiteInfo();
      if (siteInfo.CurrentDataSource == DataSource.SharedSql)
      {
        FixUpConnectionString();
        // Database.SetInitializer(new MigrateDatabaseToLatestVersion<TallyJ2dEntities, Configuration>());
      }

      RegisterDefaultRoute(RouteTable.Routes,
          siteInfo.CurrentHostMode == HostMode.SelfHostCassini ? "Dashboard" : "Public");

      ConfigureNLog();
      //ConfigureRedis();
    }

//    private void ConfigureRedis()
//    {
//      if (new SiteInfo().CurrentEnvironment != "Azure")
//      {
//        return;
//      }
//
//      // https://github.com/welegan/RedisSessionProvider 
//
//      RedisConfigOpts = ConfigurationOptions.Parse("tallyj.redis.cache.windows.net:6379");
//      RedisConfigOpts.Password = ConfigurationManager.AppSettings["REDIS_KEY"];
//
//      RedisConnectionConfig.GetSERedisServerConfig =
//        context => new KeyValuePair<string, ConfigurationOptions>("UsingRedis", RedisConfigOpts);
//    }

    private void ConfigureNLog()
    {
      // see https://github.com/nlog/nlog/wiki/Configuration-API
      var config = LogManager.Configuration;
      var target = config.FindTargetByName("logentries") as LogentriesTarget;
      if (target != null)
      {
        var siteInfo = new SiteInfo();
        var newKey = "";
        if (siteInfo.CurrentEnvironment == "Dev")
        {
          try
          {
            newKey = File.ReadAllText(@"c:\AppHarborConfig.LogEntries.txt");
          }
          catch
          {
            // swallow this
          }
        }
        else
        {
          newKey = ConfigurationManager.AppSettings["LOGENTRIES_ACCOUNT_KEY"];
        }

        if (newKey.HasContent())
        {
          target.Key = newKey;
        }
      }
    }

    private void Application_Error(object sender, EventArgs e)
    {
      var mainException = Server.GetLastError().GetBaseException();

      var msgs = new List<string>();

      var logger = LogManager.GetCurrentClassLogger();
      var siteInfo = new SiteInfo();
      var mainMsg = mainException.GetAllMsgs("; ");

      msgs.Add(mainMsg);

      var ex = mainException;
      while (ex != null)
      {
        var dbEntityValidation = ex as DbEntityValidationException;
        if (dbEntityValidation != null)
        {
          var msg = dbEntityValidation.EntityValidationErrors
              .Select(eve => eve.ValidationErrors
                  .Select(ve => "{0}: {1}".FilledWith(ve.PropertyName, ve.ErrorMessage))
                  .JoinedAsString("; "))
              .JoinedAsString("; ");
          logger.Debug(msg);
          msgs.Add(msg);
        }

        ex = ex.InnerException;
      }

      logger.FatalException(
          "Env: {0}  Err: {1}".FilledWith(siteInfo.CurrentEnvironment, msgs.JoinedAsString("; ")), mainException);

      new LogHelper().Add(msgs.JoinedAsString("\n") + "\n" + mainException.StackTrace, true);

      var url = siteInfo.RootUrl;
      // add  /* */  because this is sometimes written onto the end of a Javascript file!!
      //      Response.Write(String.Format("/* Server Error: {0} */", msgs.JoinedAsString("\r\n")));
      Response.Write(String.Format("Exception: {0}<br>", msgs.JoinedAsString("<br>")));
      Response.Write(String.Format("{0}", mainException.StackTrace.Replace("\n", "<br>")));
      if (HttpContext.Current.Request.Url.AbsolutePath.EndsWith(url))
      {
        //Response.Write("Error on site");
      }
      else
      {
        //Response.Write(String.Format("<script>location.href='{0}'</script>", url));
        //Response.Write("Error on site");
      }
      Response.End();
    }


    private void FixUpConnectionString()
    {
      var cnString = ConfigurationManager.ConnectionStrings["MainConnection"];

      var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
      fi.SetValue(cnString, false);

      cnString.ConnectionString = cnString.ConnectionString + ";MultipleActiveResultSets=True";
    }

    public static void RegisterGlobalFilters(GlobalFilterCollection filters)
    {
      filters.Add(new HandleSecurityAttribute(), 0);
      filters.Add(new HandleErrorAttribute());
    }

    public static void RegisterGeneralRoutes(RouteCollection routes)
    {
      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
      routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });
    }

    public static void RegisterDefaultRoute(RouteCollection routes, string controllerName)
    {
      routes.MapRoute(
          "Default", // Route name
          "{controller}/{action}/{id}", // URL with parameters
          new
          {
            controller = controllerName,
            action = "Index",
            id = UrlParameter.Optional
          } // Parameter defaults
          );
    }

    public override void Init()
    {
      base.Init();
      BeginRequest += OnBeginRequest;
      EndRequest += OnEndRequest;
    }

    private void OnEndRequest(object sender, EventArgs eventArgs)
    {
      var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;
      db.Dispose();
    }

    private void OnBeginRequest(object sender, EventArgs e)
    {
      var urlAdjuster = new UrlAdjuster(Request.Url.AbsolutePath);

      var newUrl = urlAdjuster.AdjustedUrl;
      if (newUrl.HasContent())
      {
        Context.RewritePath(newUrl);
      }
    }
  }
}