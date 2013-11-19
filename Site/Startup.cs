using Microsoft.Owin;
using Owin;
using TallyJ;

[assembly: OwinStartup(typeof (Startup))]

namespace TallyJ
{
  public class Startup
  {
    public void Configuration(IAppBuilder app)
    {
      // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888

      app.MapSignalR();
    }
  }
}