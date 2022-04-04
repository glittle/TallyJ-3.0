using System.Net;
using System.Web.Helpers;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;
using TallyJ;
using TallyJ.Code;
using static System.Configuration.ConfigurationManager;

[assembly: OwinStartup(typeof(OwinStartup))]

namespace TallyJ
{
  public class OwinStartup
  {
    public void Configuration(IAppBuilder app)
    {
      // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888

      app.MapSignalR();

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      AntiForgeryConfig.UniqueClaimTypeIdentifier = "UniqueID";

      app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

      var useSecure = AppSettings["secure"].AsBoolean(true);

      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
        AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
        LoginPath = new PathString("/"),
        CookieSecure = useSecure ? CookieSecureOption.Always : CookieSecureOption.Never,
        CookieSameSite = useSecure ? SameSiteMode.Strict : SameSiteMode.Lax,
        CookieHttpOnly = true
      });
    }
  }
}