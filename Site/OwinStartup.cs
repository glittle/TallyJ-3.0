using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Owin;
using TallyJ.Code;
using TallyJ.Code.OwinRelated;
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

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ExternalCookie
            });

            //            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            //            var cookieOptions = new CookieAuthenticationOptions
            //            {
            //                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
            //                LoginPath = new PathString("/Account/Connect")
            //            };
            //            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ApplicationCookie);

            //            app.Use<WordPressAuthenticationProvider>(null);
            app.UseGoogleAuthentication(CreateGoogleOptions());
            //            app.UseFacebookAuthentication(CreateFacebookOptions());
            app.Use((object)typeof(CustomFacebookAuthenticationMiddleware), (object)app, CreateFacebookOptions());



            //            app.UseSpotifyAuthentication()
            //            app.UseCookieAuthentication(CreateCookiesOptions());
        }

        //        private CookieAuthenticationOptions CreateCookiesOptions()
        //        {
        //            var options = new CookieAuthenticationOptions
        //            {
        //                LoginPath = new PathString("/Dashboard"),
        //                Provider = new CookieAuthenticationProvider
        //                {
        //                    OnResponseSignIn = signInContext =>
        //                    {
        //                        var x = 0;
        //                    },
        //                    OnValidateIdentity = identityContext =>
        //                    {
        //                        var x = 0;
        //                        return Task.FromResult(0);
        //                    },
        //                }
        //            };
        //
        //            return options;
        //        }

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
                //                OnReturnEndpoint = context =>
                //                {
                //                    var x = 1;
                //                    return Task.FromResult(0);
                //                },
                //                OnApplyRedirect = context =>
                //                {
                ////                    var x = 1;
                ////                    context.Properties.Dictionary["auth_type"] = "rerequest";
                ////
                ////                    context.Options.Fields.Clear();
                ////                    context.Options.Scope.Clear();
                ////                    context.Options.Scope.Add("email");
                //
                //                },
                OnAuthenticated = authContext =>
                {
                    var email = (string)authContext.User["email"];
                    var gotEmail = email.HasContent();

                    if (gotEmail)
                    {
                        var identity = new ClaimsIdentity(new List<Claim>
                        {
                            new Claim(ClaimTypes.Email, email),
                            new Claim("IsVoter", gotEmail ? "True" : null),
                        }, DefaultAuthenticationTypes.ExternalCookie);

                        authContext.OwinContext.Authentication.SignIn(new AuthenticationProperties()
                        {
                            AllowRefresh = true,
                            IsPersistent = false,
                            ExpiresUtc = DateTime.UtcNow.AddDays(7)
                        }, identity);
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
                        new Claim(ClaimTypes.Email, email),
                        new Claim("IsVoter", emailIsVerified),
                    }, DefaultAuthenticationTypes.ExternalCookie);

                    authContext.OwinContext.Authentication.SignIn(new AuthenticationProperties()
                    {
                        AllowRefresh = true,
                        IsPersistent = false,
                        ExpiresUtc = DateTime.UtcNow.AddDays(7)
                    }, identity);

                    return Task.FromResult(0);
                }
            };
            return options;
        }
    }
}
