using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Web.Infrastructure;
using Owin;
using TallyJ.Code;
using TallyJ.Code.Data;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.Controllers.LoginHelpers;
using TallyJ.CoreModels.VoterAccountModels;
using TallyJ.EF;
using static System.Configuration.ConfigurationManager;
using SameSiteMode = Microsoft.Owin.SameSiteMode;

[assembly: OwinStartup(typeof(TallyJ.OwinStartup))]

namespace TallyJ
{
  public class OwinStartup
  {
    // private ITallyJDbContext _db;

    public void Configuration(IAppBuilder app)
    {
      // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888

      app.MapSignalR();

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      AntiForgeryConfig.UniqueClaimTypeIdentifier = "UniqueID";

      // auth0 - adapted from https://manage.auth0.com/dashboard/us/tallyj/applications/lDGuoI4pWDzNzzkjwmKq6w53alnXyKcw/quickstart/aspnet-owin
      var auth0Domain = AppSettings["auth0-domain"];
      var auth0ClientId = AppSettings["auth0-ClientId"];
      var auth0ClientSecret = AppSettings["auth0-ClientSecret"];
      var auth0RedirectUri = AppSettings["auth0-RedirectUri"];
      var auth0PostLogoutRedirectUri = AppSettings["auth0-PostLogoutRedirectUri"];

      app.UseKentorOwinCookieSaver();
      app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

      var useSecure = AppSettings["secure"].AsBoolean(true);

      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
        // AuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
        AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
        LoginPath = new PathString("/Login"),
        CookieSecure = useSecure ? CookieSecureOption.Always : CookieSecureOption.Never,
        CookieSameSite = useSecure ? SameSiteMode.Strict : SameSiteMode.Lax,
        CookieHttpOnly = true,
        // ExpireTimeSpan = new TimeSpan(1, 0, 0),
        // Provider = new CookieAuthenticationProvider
        // {
        //   // Enables the application to validate the security stamp when the user 
        //   // logs in. This is a security feature which is used when you 
        //   // change a password or add an external login to your account.  
        //   OnValidateIdentity = SecurityStampValidator
        //     .OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
        //       validateInterval: TimeSpan.FromMinutes(30),
        //       regenerateIdentity: (manager, user)
        //         => user.GenerateUserIdentityAsync(manager))
        // }
      });

      // Enables the application to remember the second login verification factor such 
      // as phone or email. Once you check this option, your second step of 
      // verification during the login process will be remembered on the device where 
      // you logged in from. This is similar to the RememberMe option when you log in.
      // app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

      // for login v2
      //app.CreatePerOwinContext(ApplicationDbContext.Create);
      // app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
      // app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

      if (!SettingsHelper.HostSupportsOnlineElections)
      {
        return;
      }

      // Configure Auth0 authentication
      app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
      {
        AuthenticationType = "Auth0",
        Authority = $"https://{auth0Domain}",

        ClientId = auth0ClientId,
        ClientSecret = auth0ClientSecret,

        RedirectUri = auth0RedirectUri,
        PostLogoutRedirectUri = auth0PostLogoutRedirectUri,

        ResponseType = OpenIdConnectResponseType.CodeIdToken,
        Scope = "email phone",

        TokenValidationParameters = new TokenValidationParameters
        {
          NameClaimType = "name",
        },

        Notifications = new OpenIdConnectAuthenticationNotifications
        {
          AuthorizationCodeReceived = delegate (AuthorizationCodeReceivedNotification notification)
          {
            var identity = notification.AuthenticationTicket.Identity;


            var sourceClaim = identity.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            var source = sourceClaim?.Value.Split('|')[0] ?? "Auth0";

            var country = identity.Claims.FirstOrDefault(c => c.Type == "https://ns.tallyj.com/country")?.Value;

            var validLogin = false;

            var emailClaim = identity.Claims.FirstOrDefault(c => c.Type == "https://ns.tallyj.com/email");

            if (emailClaim != null)
            {
              var validated = identity.Claims.FirstOrDefault(c => c.Type == "https://ns.tallyj.com/email_verified")?.Value.AsBoolean() ?? false;
              if (validated)
              {
                identity.AddClaim(new Claim("UniqueID", emailClaim.Value));
                identity.AddClaim(new Claim("VoterId", emailClaim.Value));
                identity.AddClaim(new Claim("VoterIdType", VoterIdTypeEnum.Email));
                identity.AddClaim(new Claim("IsVoter", "True"));
               
                validLogin = true;
              }
              else
              {
                // attempt to use unverified email?
              }
            }
            else
            {
              var phoneClaim = identity.Claims.FirstOrDefault(c => c.Type == "https://ns.tallyj.com/phone");
              if (phoneClaim != null)
              {
                var validated = identity.Claims.FirstOrDefault(c => c.Type == "https://ns.tallyj.com/phone_verified")?.Value.AsBoolean() ?? false;
                if (validated)
                {
                  identity.AddClaim(new Claim("UniqueID", phoneClaim.Value));
                  identity.AddClaim(new Claim("VoterId", phoneClaim.Value));
                  identity.AddClaim(new Claim("VoterIdType", VoterIdTypeEnum.Phone));
                  identity.AddClaim(new Claim("IsVoter", "True"));

                  validLogin = true;
                }
                else
                {
                  // attempt to use unverified phone?
                }
              }
            }

            if (validLogin)
            { 
              notification.OwinContext.Authentication.SignIn(new AuthenticationProperties()
              {
                AllowRefresh = true,
                IsPersistent = false,
                ExpiresUtc = DateTime.UtcNow.AddHours(1)
              }, identity);

              // rely on UserSession values now available after the SignIn just above

              UserSession.RecordVoterLogin(UserSession.VoterId, UserSession.VoterIdType, source, country);
            }
            else
            {
              notification.OwinContext.Authentication.SignOut("Auth0",
                DefaultAuthenticationTypes.ExternalCookie,
                DefaultAuthenticationTypes.ApplicationCookie);
            }

            return Task.FromResult(0);
          },
          MessageReceived = delegate (MessageReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
          {
            return Task.FromResult(0);
          },
          SecurityTokenReceived = delegate (SecurityTokenReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
          {
            return Task.FromResult(0);
          },
          SecurityTokenValidated = delegate (SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
          {
            return Task.FromResult(0);
          },
          TokenResponseReceived = delegate (TokenResponseReceivedNotification notification)
          {
            return Task.FromResult(0);
          },
          AuthenticationFailed = delegate (AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
          {
            return Task.FromResult(0);
          },
          RedirectToIdentityProvider = notification =>
          {
            var useSms = System.Web.HttpContext.Current.Items["CELL"].AsBoolean();

            if (useSms)
            {
              notification.ProtocolMessage.LoginHint = "useSMS";
            }

            if (notification.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
            {
              var logoutUri = $"https://{auth0Domain}/v2/logout?client_id={auth0ClientId}";

              var postLogoutUri = notification.ProtocolMessage.PostLogoutRedirectUri;
              if (!string.IsNullOrEmpty(postLogoutUri))
              {
                if (postLogoutUri.StartsWith("/"))
                {
                  // transform to absolute
                  var request = notification.Request;
                  postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                }
                logoutUri += $"&returnTo={ Uri.EscapeDataString(postLogoutUri)}";
              }

              notification.Response.Redirect(logoutUri);
              notification.HandleResponse();
            }
            return Task.FromResult(0);
          }
        }
      });




      // ensure that we have authentication account details
      // if (AppSettings["facebook-AppId"].HasContent())
      // {
      //   app.Use(typeof(CustomFacebookAuthenticationMiddleware), app, CreateFacebookOptions());
      // }
      //
      // if (AppSettings["google-ClientId"].HasContent())
      // {
      //   app.UseGoogleAuthentication(CreateGoogleOptions());
      // }
    }

    // private CustomFacebookAuthenticationOptions CreateFacebookOptions()
    // {
    //   var options = new CustomFacebookAuthenticationOptions
    //   {
    //     AppId = AppSettings["facebook-AppId"],
    //     AppSecret = AppSettings["facebook-AppSecret"],
    //   };
    //
    //   options.Scope.Add("email");
    //
    //   options.Provider = new FacebookAuthenticationProvider
    //   {
    //     OnAuthenticated = authContext =>
    //     {
    //       var email = (string)authContext.User["email"];
    //       var gotEmail = email.HasContent();
    //
    //       if (gotEmail)
    //       {
    //         _db = null; // was likely disposed of
    //         var hasLocalId = Db.AspNetUsers.Any(u => u.Email == email);
    //         if (hasLocalId)
    //         {
    //           // var pwProvided = SessionKey.ExtPassword.FromSession("");
    //           var model = new LoginViewModel
    //           {
    //             Email = email,
    //             Password = " " // no longer allow login this way...
    //           };
    //           var modelState = new ModelStateDictionary();
    //           var rootUrl = new SiteInfo().RootUrl;
    //           var helpers = new LoginHelper(modelState, rootUrl, "You must provide your TallyJ password with the email address: " + email,
    //             "Facebook",
    //             (s, s1) => "", // not used from here
    //             () => new RedirectResult(rootUrl + "/Account/Logoff"),
    //             () => null // not used from here
    //             );
    //           return helpers.LocalPwLogin(model, rootUrl);
    //         }
    //
    //
    //         var identity = new ClaimsIdentity(new List<Claim>
    //         {
    //           new Claim("Source", "Facebook"),
    //           new Claim("Email", email),
    //           new Claim("UniqueID", email),
    //           new Claim("IsVoter", "True"), // Facebook doesn't show if verified
    //         }, DefaultAuthenticationTypes.ExternalCookie);
    //
    //         authContext.OwinContext.Authentication.SignIn(new AuthenticationProperties()
    //         {
    //           AllowRefresh = true,
    //           IsPersistent = false,
    //           ExpiresUtc = DateTime.UtcNow.AddHours(1)
    //         }, identity);
    //
    //         UserSession.RecordVoterLogin("Facebook", email);
    //       }
    //       else
    //       {
    //         //TODO - didn't get email... redirect to home page
    //       }
    //
    //       return Task.FromResult(0);
    //     }
    //   };
    //
    //   return options;
    // }

    // private ITallyJDbContext Db => _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().GetNewDbContext);

    // private GoogleOAuth2AuthenticationOptions CreateGoogleOptions()
    // {
    //   var options = new GoogleOAuth2AuthenticationOptions
    //   {
    //     ClientId = AppSettings["google-ClientId"],
    //     ClientSecret = AppSettings["google-ClientSecret"]
    //   };
    //   options.Scope.Add("email");
    //   options.Provider = new GoogleOAuth2AuthenticationProvider()
    //   {
    //     OnAuthenticated = authContext =>
    //     {
    //       var email = (string)authContext.User["email"];
    //
    //       _db = null; // was likely disposed of
    //       var hasLocalId = Db.AspNetUsers.Any(u => u.Email == email);
    //       if (hasLocalId)
    //       {
    //         // var pwProvided = SessionKey.ExtPassword.FromSession("");
    //         var model = new LoginViewModel
    //         {
    //           Email = email,
    //           Password = " " // no longer allow login this way...
    //         };
    //         var modelState = new ModelStateDictionary();
    //         var rootUrl = new SiteInfo().RootUrl;
    //         var helpers = new LoginHelper(modelState, rootUrl, "You must provide your TallyJ password with the email address: " + email,
    //           "Google",
    //           (s, s1) => "", // not used from here
    //           () => new RedirectResult(rootUrl + "/Account/Logoff"),
    //           () => new RedirectResult(rootUrl + "/Account/Logoff"));
    //         return helpers.LocalPwLogin(model, rootUrl);
    //       }
    //
    //
    //
    //       var emailIsVerified = (string)authContext.User["verified_email"];
    //
    //       var identity = new ClaimsIdentity(new List<Claim>
    //       {
    //         new Claim("Source", "Google"),
    //         new Claim("Email", email),
    //         new Claim("UniqueID", email),
    //         new Claim("IsVoter",  emailIsVerified),
    //       }, DefaultAuthenticationTypes.ExternalCookie);
    //
    //       authContext.OwinContext.Authentication.SignIn(new AuthenticationProperties()
    //       {
    //         AllowRefresh = true,
    //         IsPersistent = false,
    //         ExpiresUtc = DateTime.UtcNow.AddHours(1)
    //       }, identity);
    //
    //       UserSession.RecordVoterLogin("Google", email);
    //
    //       return Task.FromResult(0);
    //     }
    //   };
    //   return options;
    // }


  }
}