using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using FluentSecurity;
using TallyJ.Code;
using TallyJ.Code.Data;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.Controllers;
using Unity.Mvc3;

//using Configuration = TallyJ.Migrations.Configuration;

namespace TallyJ;
// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
// visit http://go.microsoft.com/?LinkId=9394801

public class MvcApplication : HttpApplication
{
  //public static ConfigurationOptions ConfigOpts { get; set; }
  public static DateTime TestStartTime;

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
        configuration.GetAuthenticationStatusFrom(() => UserSession.IsAuthenticated);

        configuration.ResolveServicesUsing(type => UnityInstance.Container.ResolveAll(type));

        // This is where you set up the policies you want Fluent Security to enforce on your controllers and actions
        configuration.ForAllControllers().DenyAnonymousAccess();

        configuration.For<PublicController>().Ignore();
        configuration.For<AccountController>().Ignore();

        configuration.For<AfterController>().AddPolicy(new RequireElectionPolicy());

        configuration.For<BallotsController>().AddPolicy(new RequireElectionPolicy());

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
    TestStartTime = DateTime.UtcNow;
  }

  private void SetupEnvironment()
  {
    var siteInfo = new SiteInfo();
    if (siteInfo.CurrentDataSource == DataSource.SharedSql) FixUpConnectionString();
    // Database.SetInitializer(new MigrateDatabaseToLatestVersion<TallyJEntities, Configuration>());
    RegisterDefaultRoute(RouteTable.Routes,
      siteInfo.CurrentHostMode == HostMode.SelfHostCassini ? "Dashboard" : "Public");

    // ConfigureNLog();
    // ConfigureRedis();
  }

  //public ConfigurationOptions RedisConfigOpts { get; set; }

  //private void ConfigureRedis()
  //{
  //  if (new SiteInfo().CurrentEnvironment != "Azure")
  //  {
  //    return;
  //  }

  //  // https://github.com/welegan/RedisSessionProvider 


  //  RedisConfigOpts = ConfigurationOptions.Parse(ConfigurationManager.AppSettings["REDIS_Config"]);

  //  RedisConnectionConfig.GetSERedisServerConfig =
  //    arg => new KeyValuePair<string, ConfigurationOptions>("UsingRedis", RedisConfigOpts);
  //}

  // private void ConfigureNLog()
  // {
  //   // see https://github.com/nlog/nlog/wiki/Configuration-API
  //   var config = LogManager.Configuration;
  //   var target = config.FindTargetByName("logentries") as LogentriesTarget;
  //   if (target != null)
  //   {
  //     var siteInfo = new SiteInfo();
  //     var newKey = "";
  //     if (siteInfo.CurrentEnvironment == "Dev")
  //     {
  //       try
  //       {
  //         newKey = File.ReadAllText(@"c:\AppHarborConfig.LogEntries.txt");
  //       }
  //       catch
  //       {
  //         // swallow this
  //       }
  //     }
  //     else
  //     {
  //       newKey = ConfigurationManager.AppSettings["LOGENTRIES_ACCOUNT_KEY"];
  //     }
  //
  //     if (newKey.HasContent())
  //     {
  //       target.Key = newKey;
  //     }
  //   }
  // }

  DateTime _lastException = DateTime.MinValue;

  private void Application_Error(object sender, EventArgs e)
  {
    var mainException = Server.GetLastError().GetBaseException();

    var siteInfo = new SiteInfo();
    var url = siteInfo.RootUrl;

    if (mainException.Message == "Anonymous access denied")
    {
      Response.StatusCode = (int)HttpStatusCode.Unauthorized;
      Response.Redirect(url);
      Response.End();
      return;
    }

    var msgs = new List<string>();

    try
    {
      // var logger = LogManager.GetCurrentClassLogger();
      var mainMsg = mainException.GetAllMsgs("; ");

      if (mainMsg.Contains("dbo.Sessions")
          || mainMsg.Contains("The request was aborted")
          || mainMsg.Contains("The client disconnected")
          || mainMsg.Contains("does not implement IController.")
          || mainMsg.Contains("controller for path '/favicon.ico'")
          || mainMsg.Contains("controller for path '/apple-touch-icon")
         )
      {
        // set response to not found
        Response.StatusCode = (int)HttpStatusCode.NotFound;
        return;
      }

      msgs.Add(mainMsg);

      // April 2016 - trying to determine source of ths error
      if (mainMsg.StartsWith("A public action method"))
      {
        msgs.Add(Request.Url.AbsolutePath);
        if (Request.UrlReferrer != null) msgs.Add("From: " + Request.UrlReferrer.AbsolutePath);
      }

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
          // logger.Debug(msg);
          msgs.Add(msg);
        }

        var compileError = ex as HttpCompileException;
        if (compileError != null)
        {
          var errors = compileError.Results.Errors;
          var list = new CompilerError[errors.Count];
          errors.CopyTo(list, 0);
          var msg = list.Select(err => "{0}".FilledWith(err.ErrorText)).JoinedAsString("; ");
          // logger.Debug(msg);
          msgs.Add(msg);
        }

        ex = ex.InnerException;
      }

      // logger.Fatal(mainException, "Env: {0}  Err: {1}".FilledWith(siteInfo.CurrentEnvironment, msgs.JoinedAsString("; ")));

      var sendToRemoteLog = true;
      var publicMessage = "Exception: {0}".FilledWith(msgs.JoinedAsString("\n"));

      // if (mainException.HResult == -2147467259)
      // {
      //   Response.StatusCode = 404;
      //   publicMessage = "Not found.";
      //   sendToRemoteLog = false;
      // }
      // else
      {
        Response.StatusCode = 500;
      }

      try
      {
        new LogHelper().Add(
          "Error: " + msgs.JoinedAsString("\n") + "\n" + mainException.StackTrace.FilteredStackTrace(),
          sendToRemoteLog);
      }
      catch (Exception)
      {
        // ignore?
      }

      // add  /* */  because this is sometimes written onto the end of a Javascript file!!
      //      Response.Write(String.Format("/* Server Error: {0} */", msgs.JoinedAsString("\r\n")));

      Response.Write(publicMessage);
      //      Response.Write(String.Format("{0}", FilteredStack(mainException.StackTrace).Replace("\n", "<br>")));
      // if (HttpContext.Current.Request.Url.AbsolutePath.EndsWith(url))
      // {
      //   //Response.Write("Error on site");
      // }
      // else
      // {
      //   //Response.Write(String.Format("<script>location.href='{0}'</script>", url));
      //   //Response.Write("Error on site");
      // }

      try
      {
        Response.End();
      }
      catch (Exception)
      {
        // could fail if client disconnected, etc.
      }
    }
    catch (Exception exception)
    {
      try
      {
        msgs.Add(exception.Message);
        new LogHelper().Add(
          "Error: " + msgs.JoinedAsString("\n") + "\n" + mainException.StackTrace.FilteredStackTrace(), true);
      }
      catch (Exception)
      {
        // ignore
      }
    }
  }


  public void Application_End()
  {
    var shutdownReason = HostingEnvironment.ShutdownReason;
    // using EventLog eventLog = new EventLog("Application") { Source = "WebSites" };
    // initialize in powershell on webserver:  New-EventLog -LogName Application -Source WebSites
    var message = new[]
    {
      $"Website shut down: {shutdownReason}.",
      // "",
      // $"Site: {HostingEnvironment.SiteName}",
      // $"Application: {HostingEnvironment.ApplicationVirtualPath}",
      $"Ran for {DateTime.UtcNow - TestStartTime:hh\\:mm\\:ss}"
    }.JoinedAsString(" ");

    new LogHelper().SendToRemoteLog(message, true);

    // eventLog.WriteEntry(message, EventLogEntryType.Warning);
  }


  /// <summary>
  /// Modifies the connection string for the main database connection.
  /// </summary>
  /// <remarks>
  /// This method retrieves the connection string named "MainConnection3" from the web configuration file.
  /// If the connection string is found, it sets its read-only state to false, allowing modifications.
  /// The method then appends the option "MultipleActiveResultSets=True" to the existing connection string.
  /// This enables support for multiple active result sets (MARS) in SQL Server, allowing multiple 
  /// commands to be executed on a single connection without waiting for the previous command to complete.
  /// If the connection string is not found, the method simply returns without making any changes.
  /// </remarks>
  private void FixUpConnectionString()
  {
    var cnString = WebConfigurationManager.ConnectionStrings["MainConnection3"];
    if (cnString == null) return;

    var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
    fi.SetValue(cnString, false);

    cnString.ConnectionString += ";MultipleActiveResultSets=True";
  }

  public static void RegisterGlobalFilters(GlobalFilterCollection filters)
  {
    filters.Add(new HandleSecurityAttribute(), 0);
    filters.Add(new HandleErrorAttribute());
  }

  public static void RegisterGeneralRoutes(RouteCollection routes)
  {
    routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

    // not working?
    //      routes.MapRoute("fav", "favicon.ico", new { controller = "Public", action = "FavIcon" });
    routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });
  }

  public static void RegisterDefaultRoute(RouteCollection routes, string controllerName)
  {
    routes.MapMvcAttributeRoutes();

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
    UnityInstance.Resolve<IDbContextFactory>().CloseAll();
  }

  private void OnBeginRequest(object sender, EventArgs e)
  {
    var urlAdjuster = new UrlAdjuster(Request.Url.AbsolutePath);

    var newUrl = urlAdjuster.AdjustedUrl;
    if (newUrl.HasContent()) Context.RewritePath(newUrl);
  }
}