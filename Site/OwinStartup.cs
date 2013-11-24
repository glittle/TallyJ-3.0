using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof (TallyJ.OwinStartup))]

namespace TallyJ
{
  public class OwinStartup
  {
    public void Configuration(IAppBuilder app)
    {
      // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888

      app.MapSignalR();
    }
  }
}