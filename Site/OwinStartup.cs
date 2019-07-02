using Microsoft.Owin;
using Owin;
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

            app.UseGoogleAuthentication(AppSettings["google-ClientId"], AppSettings["google-ClientSecret"]);
            app.UseFacebookAuthentication(AppSettings["facebook-AppId"], AppSettings["facebook-AppSecret"]);
        }
    }
}