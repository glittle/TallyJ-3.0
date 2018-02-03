using System.Web.Hosting;
using RazorEngine;
using RazorEngine.Templating;
using System.IO;
using RazorEngine.Configuration;
using System.Configuration;

namespace TallyJ.Code.Helpers
{
  public class MvcViewRenderer
  {
    static TemplateServiceConfiguration config;

    public static string RenderRazorViewToString(string pathToView, object model = null)
    {
      var path = HostingEnvironment.MapPath(pathToView);
      if (path == null)
      {
        return "";
      }

      pathToView = pathToView.Replace("~", "");

      if (config == null || ConfigurationManager.AppSettings["Environment"] == "Dev")
      {
        config = new TemplateServiceConfiguration();
        config.TemplateManager = new ResolvePathTemplateManager(new[] { HostingEnvironment.MapPath("~") });
      }
      var razor = RazorEngineService.Create(config);

      //var template = File.ReadAllText(path);

      //var body = Engine.Razor.RunCompile(template, "Test", null, model);
      //new FullPathTemplateKey("x", path)
      var body = razor.RunCompile(path, null, model);

      return body;
    }
  }
}