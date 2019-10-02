using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Owin;
using TallyJ.Code;
using TallyJ.Code.OwinRelated;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Account2Models;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;
using static System.Configuration.ConfigurationManager;

[assembly: OwinStartup(typeof(TallyJ.OwinStartup))]

namespace TallyJ
{
  public class OwinStartup
  {
    public void Configuration(IAppBuilder app)
    {
      // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888

      app.MapSignalR();

      // for login v2
      app.CreatePerOwinContext(ApplicationDbContext.Create);
      app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
      app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      AntiForgeryConfig.UniqueClaimTypeIdentifier = "UniqueID";

      app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
        AuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
        CookieSecure = CookieSecureOption.Always,
        ExpireTimeSpan = new TimeSpan(1, 0, 0),
        LoginPath = new PathString("/"),
        Provider = new CookieAuthenticationProvider
        {
          // Enables the application to validate the security stamp when the user 
          // logs in. This is a security feature which is used when you 
          // change a password or add an external login to your account.  
          OnValidateIdentity = SecurityStampValidator
            .OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
              validateInterval: TimeSpan.FromMinutes(30),
              regenerateIdentity: (manager, user) 
                => user.GenerateUserIdentityAsync(manager))
        }
      });

      // Enables the application to remember the second login verification factor such 
      // as phone or email. Once you check this option, your second step of 
      // verification during the login process will be remembered on the device where 
      // you logged in from. This is similar to the RememberMe option when you log in.
      app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);


      if (!SettingsHelper.HostSupportsOnlineElections)
      {
        return;
      }

      // ensure that we have authentication account details

      if (AppSettings["facebook-AppId"].HasContent())
      {
        app.Use(typeof(CustomFacebookAuthenticationMiddleware), app, CreateFacebookOptions());
      }

      if (AppSettings["google-ClientId"].HasContent())
      {
        app.UseGoogleAuthentication(CreateGoogleOptions());
      }
    }

    private CustomFacebookAuthenticationOptions CreateFacebookOptions()
    {
      var options = new CustomFacebookAuthenticationOptions
      {
        AppId = AppSettings["facebook-AppId"],
        AppSecret = AppSettings["facebook-AppSecret"],
      };

      options.Scope.Add("email");

      options.Provider = new FacebookAuthenticationProvider
      {
        OnAuthenticated = authContext =>
        {
          var email = (string)authContext.User["email"];
          var gotEmail = email.HasContent();

          if (gotEmail)
          {
            var identity = new ClaimsIdentity(new List<Claim>
            {
              new Claim("Source", "Facebook"),
              new Claim("Email", email),
              new Claim("UniqueID", email),
              new Claim("IsVoter", "True"), // Facebook doesn't show if verified
            }, DefaultAuthenticationTypes.ExternalCookie);

            authContext.OwinContext.Authentication.SignIn(new AuthenticationProperties()
            {
              AllowRefresh = true,
              IsPersistent = false,
              ExpiresUtc = DateTime.UtcNow.AddHours(1)
            }, identity);

            RecordLogin("Facebook", email);
          }
          else
          {
            //TODO - didn't get email... redirect to home page
          }

          return Task.FromResult(0);
        }
      };

      return options;
    }

    private GoogleOAuth2AuthenticationOptions CreateGoogleOptions()
    {
      var options = new GoogleOAuth2AuthenticationOptions
      {
        ClientId = AppSettings["google-ClientId"],
        ClientSecret = AppSettings["google-ClientSecret"]
      };
      options.Scope.Add("email");
      options.Provider = new GoogleOAuth2AuthenticationProvider()
      {
        OnAuthenticated = authContext =>
        {
          var email = (string)authContext.User["email"];
          var emailIsVerified = (string)authContext.User["verified_email"];

          var identity = new ClaimsIdentity(new List<Claim>
          {
            new Claim("Source", "Google"),
            new Claim("Email", email),
            new Claim("UniqueID", email),
            new Claim("IsVoter",  emailIsVerified),
          }, DefaultAuthenticationTypes.ExternalCookie);

          authContext.OwinContext.Authentication.SignIn(new AuthenticationProperties()
          {
            AllowRefresh = true,
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddHours(1)
          }, identity);

          RecordLogin("Google", email);

          return Task.FromResult(0);
        }
      };
      return options;
    }

    private void RecordLogin(string source, string email)
    {
      var db = UserSession.DbContext;
      var onlineVoter = db.OnlineVoter.FirstOrDefault(ov => ov.Email == email);
      var now = DateTime.Now;

      if (onlineVoter == null)
      {
        onlineVoter = new OnlineVoter
        {
          Email = email,
          WhenRegistered = now
        };
        db.OnlineVoter.Add(onlineVoter);
      }
      else
      {
        UserSession.VoterLastLogin = onlineVoter.WhenLastLogin.GetValueOrDefault(DateTime.MinValue);
      }

      onlineVoter.WhenLastLogin = now;
      db.SaveChanges();

      new LogHelper().Add($"Login from {source}", true, email);

      new VoterPersonalHub().Login(email); // in case same email is logged into a different computer
    }
  }
}