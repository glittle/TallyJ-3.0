using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using static System.Configuration.ConfigurationManager;

namespace TallyJ.Controllers
{
    public class ChallengeResult : HttpUnauthorizedResult
    {
        public ChallengeResult(string provider, string redirectUri)
            : this(provider, redirectUri, null)
        {
        }

        public ChallengeResult(string provider, string redirectUri, string xsrfValue)
        {
            LoginProvider = provider;
            RedirectUri = redirectUri;
            XsrfValue = xsrfValue;
        }

        public string LoginProvider { get; set; }
        public string RedirectUri { get; set; }
        public string XsrfValue { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = RedirectUri,
            };

            if (XsrfValue != null)
            {
                properties.Dictionary["XsrfKey"] = XsrfValue;
            }

            if (LoginProvider == "Facebook")
            {
                properties.Dictionary["auth_type"] = "rerequest";
            }

            var owin = context.HttpContext.GetOwinContext();
            owin.Authentication.Challenge(properties, LoginProvider);
        }
    }
}