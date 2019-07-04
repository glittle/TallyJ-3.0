using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Owin;
using Owin.Security.Providers.WordPress;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Controllers;
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

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            //app.UseCookieAuthentication(new CookieAuthenticationOptions
            //            {
            //                AuthenticationType = DefaultAuthenticationTypes.ExternalCookie
            //            });
            //app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            //            var cookieOptions = new CookieAuthenticationOptions
            //            {
            //                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
            //                LoginPath = new PathString("/Account/Connect")
            //            };
            //            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ApplicationCookie);

            //            app.Use<WordPressAuthenticationProvider>(null);

            app.UseGoogleAuthentication(CreateGoogleOptions());

            app.UseFacebookAuthentication(CreateFacebookOptions());

        }

        private FacebookAuthenticationOptions CreateFacebookOptions()
        {
            var options = new FacebookAuthenticationOptions
            {
                AppId = AppSettings["facebook-AppId"],
                AppSecret = AppSettings["facebook-AppSecret"],
            };

            options.Scope.Add("email");
            //            options.Scope.Add("public_profile");
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
                // After OAuth authentication completes successfully,
                // read user's profile image URL from the profile
                // response data and add it to the current user identity
                OnAuthenticated = context =>
                {
                    var email = (string)context.User["email"];
                    var emailIsVerified = (string)context.User["verified_email"];
                    var pictureUrl = (string)context.User["picture"];

                    //                    context.Identity.AddClaim(new Claim(ClaimTypes.Email, email));
                    //                    context.Identity.AddClaim(new Claim(ClaimTypes.Authentication, emailIsVerified));
                    //                    context.Identity.AddClaim(new Claim(ClaimTypes.Uri, pictureUrl));

                    UserSession.VoterEmail = email;
                    UserSession.VoterEmailIsVerified = emailIsVerified.AsBoolean();
                    UserSession.VoterPictureUrl = pictureUrl;

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, email),
                        new Claim("IsVerified", emailIsVerified),
                        new Claim("PictureUrl", pictureUrl)
                    };

                    var identity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);

                    var authenticationProperties = new AuthenticationProperties()
                    {
                        AllowRefresh = true,
                        IsPersistent = false,
                        ExpiresUtc = DateTime.UtcNow.AddDays(7)
                    };

                    HttpContext.Current.GetOwinContext().Authentication.SignIn(authenticationProperties, identity);

                    return Task.FromResult(0);
                }
                // [END read_google_profile_image_url]
            };
            return options;
        }

    }
}