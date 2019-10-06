﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Profile;
using System.Web.Routing;
using FluentSecurity;
using NLog;
using NLog.Targets;
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
    public static DateTime LastRunOfScheduled;

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
            configuration.For<Account2Controller>().Ignore();

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

      var logger = LogManager.GetCurrentClassLogger();
      var mainMsg = mainException.GetAllMsgs("; ");

      if (mainMsg.Contains("dbo.Sessions") || mainMsg.Contains("The request was aborted"))
      {
        // don't track StateServer errors...
        return;
      }

      msgs.Add(mainMsg);

      // April 2016 - trying to determine source of ths error
      if (mainMsg.StartsWith("A public action method"))
      {
        msgs.Add(Request.Url.AbsolutePath);
        if (Request.UrlReferrer != null)
        {
          msgs.Add("From: " + Request.UrlReferrer.AbsolutePath);
        }
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
          logger.Debug(msg);
          msgs.Add(msg);
        }

        var compileError = ex as HttpCompileException;
        if (compileError != null)
        {
          var list = new CompilerError[0];
          compileError.Results.Errors.CopyTo(list, 0);
          var msg = list.Select(err => "{0}".FilledWith(err.ErrorText)).JoinedAsString("; ");
          logger.Debug(msg);
          msgs.Add(msg);
        }

        ex = ex.InnerException;
      }

      logger.Fatal(mainException, "Env: {0}  Err: {1}".FilledWith(siteInfo.CurrentEnvironment, msgs.JoinedAsString("; ")));

      var sendToRemoteLog = true;
      string publicMessage = "Exception: {0}<br>".FilledWith(msgs.JoinedAsString("<br>"));

      if (mainException.HResult == -2147467259)
      {
        Response.StatusCode = 404;
        publicMessage = "Not found.";
        sendToRemoteLog = false;
      }
      else
      {
        Response.StatusCode = 500;
      }

      new LogHelper().Add(msgs.JoinedAsString("\n") + "\n" + FilteredStack(mainException.StackTrace), sendToRemoteLog);


      // add  /* */  because this is sometimes written onto the end of a Javascript file!!
      //      Response.Write(String.Format("/* Server Error: {0} */", msgs.JoinedAsString("\r\n")));

      Response.Write(publicMessage);
      //      Response.Write(String.Format("{0}", FilteredStack(mainException.StackTrace).Replace("\n", "<br>")));
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

    private string FilteredStack(string stackTrace)
    {
      var parts = stackTrace.Split(new[] { '\n', '\r' }).Where(s => !string.IsNullOrEmpty(s)).Reverse().ToList();
      var newParts = new List<string>();
      var foundOurCode = false;
      foreach (var part in parts)
      {
        if (part.Contains("at TallyJ."))
        {
          foundOurCode = true;
        }
        if (foundOurCode)
        {
          newParts.Add(part);
        }
      }
      return newParts.Select(s => s).Reverse().JoinedAsString("\r\n");
    }


    private void FixUpConnectionString()
    {
      var cnString = ConfigurationManager.ConnectionStrings["MainConnection3"];

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

      // not working?
      routes.MapRoute("fav", "favicon.ico", new { controller = "Public", action = "FavIcon" });
      //routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });
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
      UnityInstance.Resolve<IDbContextFactory>().CloseAll();
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